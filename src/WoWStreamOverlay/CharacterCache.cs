using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WowStreamOverlay;

/// <summary>
/// Source used for the latest character profile refresh.
/// </summary>
public enum CharacterRefreshSource
{
    CombatLog,
    BattleNet
}

/// <summary>
/// Character profile stored in the local cache.
/// </summary>
/// <param name="Profile">Character profile.</param>
/// <param name="LastRefresh">Date of the latest refresh.</param>
/// <param name="LastRefreshSource">Source used for the latest refresh.</param>
public sealed record CachedCharacter(
    CharacterProfile Profile,
    DateTimeOffset LastRefresh,
    CharacterRefreshSource LastRefreshSource);

/// <summary>
/// Persistent cache of World of Warcraft character profiles.
/// </summary>
public sealed partial class CharacterCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly CharacterCacheJsonContext JsonContext = new(JsonOptions);

    private readonly string _path;
    private readonly Dictionary<string, CachedCharacter> _characters = new(StringComparer.OrdinalIgnoreCase);

    public CharacterCache(string path)
    {
        _path = path;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _characters.Clear();

        if (!File.Exists(_path))
        {
            return;
        }

        await using var stream = File.OpenRead(_path);
        var cacheFile = await JsonSerializer.DeserializeAsync(stream, JsonContext.CharacterCacheFile, cancellationToken);

        if (cacheFile is null)
        {
            return;
        }

        foreach (var character in cacheFile.Characters)
        {
            _characters[character.Key] = character.Value.ToCachedCharacter();
        }
    }

    public CachedCharacter? Get(string guid)
    {
        if (_characters.TryGetValue(guid, out var character))
        {
            return character;
        }

        return null;
    }

    public KeyValuePair<string, CachedCharacter>[] GetAll()
    {
        return [.. _characters];
    }

    public void Set(string guid, CharacterProfile profile, CharacterRefreshSource source)
    {
        _characters[guid] = new CachedCharacter(profile, DateTimeOffset.UtcNow, source);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var cacheFile = new CharacterCacheFile();

        foreach (var character in _characters)
        {
            cacheFile.Characters[character.Key] = CharacterCacheValue.FromCachedCharacter(character.Value);
        }

        var temporaryPath = $"{_path}.tmp";

        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, cacheFile, JsonContext.CharacterCacheFile, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed class CharacterCacheFile
    {
        public Dictionary<string, CharacterCacheValue> Characters { get; init; } = [];
    }

    private sealed class CharacterCacheValue
    {
        public string Name { get; init; } = string.Empty;
        public string Realm { get; init; } = string.Empty;
        public string RealmSlug { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public CharacterClass Class { get; init; }
        public CharacterSpecialization Specialization { get; init; }
        public CharacterRace Race { get; init; }
        public int Level { get; init; }
        public int ItemLevel { get; init; }
        public DateTimeOffset LastRefresh { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter<CharacterRefreshSource>))]
        public CharacterRefreshSource LastRefreshSource { get; init; }

        public CachedCharacter ToCachedCharacter()
        {
            return new CachedCharacter(
                new CharacterProfile(Name, Realm, RealmSlug, Region, Class, Specialization, Race, Level, ItemLevel),
                LastRefresh,
                LastRefreshSource);
        }

        public static CharacterCacheValue FromCachedCharacter(CachedCharacter character)
        {
            return new CharacterCacheValue
            {
                Name = character.Profile.Name,
                Realm = character.Profile.Realm,
                RealmSlug = character.Profile.RealmSlug,
                Region = character.Profile.Region,
                Class = character.Profile.Class,
                Specialization = character.Profile.Specialization,
                Race = character.Profile.Race,
                Level = character.Profile.Level,
                ItemLevel = character.Profile.ItemLevel,
                LastRefresh = character.LastRefresh,
                LastRefreshSource = character.LastRefreshSource
            };
        }
    }

    [JsonSerializable(typeof(CharacterCacheFile))]
    private partial class CharacterCacheJsonContext : JsonSerializerContext
    {
    }
}
