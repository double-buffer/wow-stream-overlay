using System.Globalization;
using System.Text;
namespace WowStreamOverlay;

using WowStreamOverlay.CombatLog;

public sealed class WoWStreamOverlayApp
{
    private readonly CombatLogParser _parser;
    private readonly CharacterCache _characterCache;
    private readonly BattleNetClient? _battleNetClient;
    private string? _currentPlayerGuid;

    public CharacterProfile? CurrentCharacter { get; private set; }

    public WoWStreamOverlayApp(CombatLogParser parser, CharacterCache characterCache, BattleNetClient? battleNetClient)
    {
        _parser = parser;
        _characterCache = characterCache;
        _battleNetClient = battleNetClient;
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
                Console.WriteLine($"MythicPlus Started: {challengeStarted.DungeonName}, +{challengeStarted.Level}");
                break;

            case ChallengeModeEndedEvent challengeEnded:
                Console.WriteLine($"MythicPlus Ended Completed: {challengeEnded.Completed}");
                break;
        }
    }

    private async Task ProcessPlayerObservedAsync(PlayerObservedEvent playerObserved, CancellationToken cancellationToken)
    {
        if (playerObserved.Guid == _currentPlayerGuid)
        {
            return;
        }

        _currentPlayerGuid = playerObserved.Guid;

        var cachedCharacter = _characterCache.Get(playerObserved.Guid);

        if (cachedCharacter is not null)
        {
            CurrentCharacter = cachedCharacter.Profile;
            Console.WriteLine($"Found cached character: {CurrentCharacter.Name}, Spec: {CurrentCharacter.Specialization}, iLvl: {CurrentCharacter.ItemLevel}");
        }

        if (_battleNetClient is null)
        {
            return;
        }

        if (!TryParsePlayerName(playerObserved.Name, out var characterName, out var realmName))
        {
            Console.Error.WriteLine($"Unable to parse player name: {playerObserved.Name}");
            return;
        }

        var realmSlug = cachedCharacter?.Profile.RealmSlug ?? CreateRealmSlug(realmName);

        try
        {
            var character = await _battleNetClient.GetCharacterProfileAsync(realmSlug, characterName, cancellationToken);

            if (character is null)
            {
                Console.Error.WriteLine($"Character not found on Battle.net: {characterName}-{realmName}");
                return;
            }

            CurrentCharacter = character;

            _characterCache.Set(playerObserved.Guid, character, CharacterRefreshSource.BattleNet);
            await _characterCache.SaveAsync(cancellationToken);

            Console.WriteLine(
                $"Found character: {character.Name}, Spec: {character.Specialization}, iLvl: {character.ItemLevel}");
        }
        catch (HttpRequestException exception)
        {
            Console.Error.WriteLine($"Battle.net request failed: {exception.Message}");
        }
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
