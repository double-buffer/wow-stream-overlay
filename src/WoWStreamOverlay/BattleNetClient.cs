using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace WowStreamOverlay;

public sealed partial class BattleNetClient : ICharacterProfileProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _region;
    private readonly string _locale;

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiration;

    public CharacterRefreshSource RefreshSource => CharacterRefreshSource.BattleNet;

    public BattleNetClient(
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        string region = "eu",
        string locale = "fr_FR")
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _region = region.ToLowerInvariant();
        _locale = locale;
    }

    public async Task<CharacterProfile?> GetCharacterProfileAsync(
        string realmSlug,
        string characterName,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var realm = Uri.EscapeDataString(realmSlug.ToLowerInvariant());
        var character = Uri.EscapeDataString(characterName.ToLowerInvariant());
        var locale = Uri.EscapeDataString(_locale);

        var requestUri =
            $"https://{_region}.api.blizzard.com/profile/wow/character/{realm}/{character}" +
            $"?namespace=profile-{_region}&locale={locale}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var profile = await response.Content.ReadFromJsonAsync(BattleNetJsonContext.Default.BattleNetCharacterProfile, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException("Battle.net returned an empty character profile.");
        }

        return new CharacterProfile(
            profile.Name,
            profile.Realm.Name,
            profile.Realm.Slug,
            _region,
            (CharacterClass)profile.CharacterClass.Id,
            (CharacterSpecialization)profile.ActiveSpecialization.Id,
            (CharacterRace)profile.Race.Id,
            profile.Level,
            profile.EquippedItemLevel,
            profile.CharacterClass.Name,
            profile.ActiveSpecialization.Name,
            profile.Race.Name);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (_accessToken is not null && now < _accessTokenExpiration - TimeSpan.FromMinutes(1))
        {
            return _accessToken;
        }

        var requestUri = $"https://{_region}.battle.net/oauth/token";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync(BattleNetJsonContext.Default.BattleNetAccessToken, cancellationToken);

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Battle.net returned an invalid access token.");
        }

        _accessToken = token.AccessToken;
        _accessTokenExpiration = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

        return _accessToken;
    }

    private sealed class BattleNetAccessToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }

    private sealed class BattleNetCharacterProfile
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("race")]
        public BattleNetReference Race { get; init; } = new();

        [JsonPropertyName("character_class")]
        public BattleNetReference CharacterClass { get; init; } = new();

        [JsonPropertyName("active_spec")]
        public BattleNetReference ActiveSpecialization { get; init; } = new();

        [JsonPropertyName("realm")]
        public BattleNetRealm Realm { get; init; } = new();

        [JsonPropertyName("level")]
        public int Level { get; init; }

        [JsonPropertyName("equipped_item_level")]
        public int EquippedItemLevel { get; init; }
    }

    private sealed class BattleNetReference
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
    }

    private sealed class BattleNetRealm
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; init; } = string.Empty;
    }

    [JsonSerializable(typeof(BattleNetAccessToken))]
    [JsonSerializable(typeof(BattleNetCharacterProfile))]
    private partial class BattleNetJsonContext : JsonSerializerContext
    {
    }
}
