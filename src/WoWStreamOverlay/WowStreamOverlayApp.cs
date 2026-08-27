using System.Globalization;
using System.Text;
using WowStreamOverlay.CombatLog;

namespace WowStreamOverlay;

public sealed class WoWStreamOverlayApp
{
    private readonly CombatLogParser _parser;
    private readonly CharacterCache _characterCache;
    private readonly ICharacterProfileProvider? _characterProfileProvider;
    private readonly GameState _gameState;
    private readonly GameStateStore _gameStateStore;
    private readonly TimeSpan _characterRefreshInterval;

    private string? _currentPlayerGuid;
    private DateTimeOffset _nextCharacterRefresh;

    public GameState State => _gameState;

    public WoWStreamOverlayApp(
        CombatLogParser parser,
        CharacterCache characterCache,
        ICharacterProfileProvider? characterProfileProvider,
        GameState gameState,
        GameStateStore gameStateStore,
        TimeSpan characterRefreshInterval)
    {
        _parser = parser;
        _characterCache = characterCache;
        _characterProfileProvider = characterProfileProvider;
        _gameState = gameState;
        _gameStateStore = gameStateStore;
        _characterRefreshInterval = characterRefreshInterval;
    }

    public async Task RefreshCharacterCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_characterProfileProvider is null)
        {
            return;
        }

        var characters = _characterCache.GetAll();
        var cacheChanged = false;
        var stateChanged = false;

        foreach (var cachedCharacter in characters)
        {
            try
            {
                var profile = await _characterProfileProvider.GetCharacterProfileAsync(
                    cachedCharacter.Value.Profile.RealmSlug,
                    cachedCharacter.Value.Profile.Name,
                    cancellationToken);

                if (profile is null)
                {
                    continue;
                }

                profile = PreserveUnavailableFields(profile, cachedCharacter.Value.Profile);
                _characterCache.Set(cachedCharacter.Key, profile, _characterProfileProvider.RefreshSource);
                cacheChanged = true;

                if (string.Equals(_gameState.CurrentCharacterGuid, cachedCharacter.Key, StringComparison.OrdinalIgnoreCase))
                {
                    _gameState.Character = profile;
                    stateChanged = true;
                }

                Console.WriteLine($"Refreshed character: {profile.Name}, Spec: {profile.Specialization}, iLvl: {profile.ItemLevel}");
            }
            catch (HttpRequestException exception)
            {
                Console.Error.WriteLine(
                    $"{_characterProfileProvider.RefreshSource} refresh failed for {cachedCharacter.Value.Profile.Name}: {exception.Message}");
            }
        }

        if (cacheChanged)
        {
            await _characterCache.SaveAsync(cancellationToken);
        }

        if (stateChanged)
        {
            await _gameStateStore.SaveAsync(_gameState, cancellationToken);
            _gameState.NotifyChanged();
        }
    }

    public Task BootstrapPlayerAsync(PlayerObservedEvent playerObserved, CancellationToken cancellationToken = default)
    {
        return ProcessPlayerObservedAsync(playerObserved, cancellationToken);
    }

    public async Task ProcessCombatLogLineAsync(string line, CancellationToken cancellationToken = default)
    {
        var result = _parser.ParseLine(line);

        if (result.Status != ParseStatus.Parsed)
        {
            return;
        }

        switch (result.Event)
        {
            case PlayerObservedEvent playerObserved:
                await ProcessPlayerObservedAsync(playerObserved, cancellationToken);
                break;

            case ChallengeModeStartedEvent challengeStarted:
                _gameState.MythicPlus = new MythicPlusState(challengeStarted.DungeonName, challengeStarted.Level);
                await _gameStateStore.SaveAsync(_gameState, cancellationToken);
                _gameState.NotifyChanged();
                Console.WriteLine($"MythicPlus Started: {challengeStarted.DungeonName}, +{challengeStarted.Level}");
                break;

            case ChallengeModeEndedEvent challengeEnded:
                _gameState.MythicPlus = null;
                await _gameStateStore.SaveAsync(_gameState, cancellationToken);
                _gameState.NotifyChanged();
                Console.WriteLine($"MythicPlus Ended Completed: {challengeEnded.Completed}");
                break;
        }
    }

    private async Task ProcessPlayerObservedAsync(PlayerObservedEvent playerObserved, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (playerObserved.Guid == _currentPlayerGuid && now < _nextCharacterRefresh)
        {
            return;
        }

        var characterChanged = playerObserved.Guid != _currentPlayerGuid;

        if (characterChanged)
        {
            _currentPlayerGuid = playerObserved.Guid;
            _gameState.CurrentCharacterGuid = playerObserved.Guid;

            var cachedCharacter = _characterCache.Get(playerObserved.Guid);
            _gameState.Character = cachedCharacter?.Profile;
            _nextCharacterRefresh = cachedCharacter?.LastRefresh + _characterRefreshInterval ?? now;

            await _gameStateStore.SaveAsync(_gameState, cancellationToken);
            _gameState.NotifyChanged();

            if (cachedCharacter is not null)
            {
                Console.WriteLine(
                    $"Found cached character: {cachedCharacter.Profile.Name}, Spec: {cachedCharacter.Profile.Specialization}, " +
                    $"iLvl: {cachedCharacter.Profile.ItemLevel}");
            }
        }

        if (_characterProfileProvider is null)
        {
            _nextCharacterRefresh = DateTimeOffset.MaxValue;
            return;
        }

        if (now < _nextCharacterRefresh)
        {
            return;
        }

        await RefreshCurrentCharacterAsync(playerObserved, cancellationToken);
    }

    private async Task RefreshCurrentCharacterAsync(PlayerObservedEvent playerObserved, CancellationToken cancellationToken)
    {
        var cachedCharacter = _characterCache.Get(playerObserved.Guid);
        string characterName;
        string realmSlug;

        if (cachedCharacter is not null)
        {
            characterName = cachedCharacter.Profile.Name;
            realmSlug = cachedCharacter.Profile.RealmSlug;
        }
        else
        {
            if (!TryParsePlayerName(playerObserved.Name, out characterName, out var realmName))
            {
                _nextCharacterRefresh = DateTimeOffset.UtcNow + _characterRefreshInterval;
                Console.Error.WriteLine($"Unable to parse player name: {playerObserved.Name}");
                return;
            }

            realmSlug = CreateRealmSlug(realmName);
        }

        _nextCharacterRefresh = DateTimeOffset.UtcNow + _characterRefreshInterval;

        try
        {
            var character = await _characterProfileProvider!.GetCharacterProfileAsync(realmSlug, characterName, cancellationToken);

            if (character is null)
            {
                Console.Error.WriteLine($"Character not found by {_characterProfileProvider.RefreshSource}: {characterName}");
                return;
            }

            if (cachedCharacter is not null)
            {
                character = PreserveUnavailableFields(character, cachedCharacter.Profile);
            }

            _characterCache.Set(playerObserved.Guid, character, _characterProfileProvider.RefreshSource);
            _gameState.Character = character;

            await _characterCache.SaveAsync(cancellationToken);
            await _gameStateStore.SaveAsync(_gameState, cancellationToken);
            _gameState.NotifyChanged();

            Console.WriteLine($"Refreshed current character: {character.Name}, Spec: {character.Specialization}, iLvl: {character.ItemLevel}");
        }
        catch (HttpRequestException exception)
        {
            Console.Error.WriteLine($"{_characterProfileProvider.RefreshSource} request failed: {exception.Message}");
        }
    }

    private static CharacterProfile PreserveUnavailableFields(CharacterProfile profile, CharacterProfile cachedProfile)
    {
        return profile with
        {
            Level = profile.Level > 0 ? profile.Level : cachedProfile.Level,
            ItemLevel = profile.ItemLevel > 0 ? profile.ItemLevel : cachedProfile.ItemLevel,
            ClassName = profile.ClassName ?? cachedProfile.ClassName,
            SpecializationName = profile.SpecializationName ?? cachedProfile.SpecializationName,
            RaceName = profile.RaceName ?? cachedProfile.RaceName
        };
    }

    private static bool TryParsePlayerName(string value, out string characterName, out string realmName)
    {
        characterName = string.Empty;
        realmName = string.Empty;

        var firstSeparator = value.IndexOf('-');
        var lastSeparator = value.LastIndexOf('-');

        if (firstSeparator <= 0 || lastSeparator <= firstSeparator || lastSeparator == value.Length - 1)
        {
            return false;
        }

        characterName = value[..firstSeparator];
        realmName = value[(firstSeparator + 1)..lastSeparator];

        return true;
    }

    private static string CreateRealmSlug(string realmName)
    {
        var normalized = realmName.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                result.Append(char.ToLowerInvariant(character));
            }
            else if ((char.IsWhiteSpace(character) || character == '-') && result.Length > 0 && result[^1] != '-')
            {
                result.Append('-');
            }
        }

        return result.ToString().TrimEnd('-');
    }
}
