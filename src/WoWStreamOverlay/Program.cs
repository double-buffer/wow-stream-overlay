using Microsoft.Extensions.Configuration;
using WowStreamOverlay;
using WowStreamOverlay.CombatLog;

Console.WriteLine("WoW Stream Overlay v0.1.0 · https://github.com/double-buffer/wow-stream-overlay");

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets(typeof(BattleNetClient).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var charactersPath = configuration["Storage:CharactersPath"];

if (string.IsNullOrWhiteSpace(charactersPath))
{
    charactersPath = "characters.json";
}

var statePath = configuration["Storage:StatePath"];

if (string.IsNullOrWhiteSpace(statePath))
{
    statePath = "state.json";
}

var characterCache = new CharacterCache(charactersPath);
await characterCache.LoadAsync();

var gameStateStore = new GameStateStore(statePath);
var gameState = await gameStateStore.LoadAsync();

var clientId = configuration["BattleNet:ClientId"];
var clientSecret = configuration["BattleNet:ClientSecret"];
var region = configuration["BattleNet:Region"];
var locale = configuration["BattleNet:Locale"];

if (string.IsNullOrWhiteSpace(region))
{
    region = "eu";
}

if (string.IsNullOrWhiteSpace(locale))
{
    locale = "fr_FR";
}

var refreshIntervalSeconds = 60;

if (int.TryParse(configuration["BattleNet:CharacterRefreshIntervalSeconds"], out var configuredRefreshInterval) && configuredRefreshInterval > 0)
{
    refreshIntervalSeconds = configuredRefreshInterval;
}

using var httpClient = new HttpClient();
BattleNetClient? battleNetClient = null;

if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
{
    battleNetClient = new BattleNetClient(httpClient, clientId, clientSecret, region, locale);
}
else
{
    Console.WriteLine("Battle.net integration is not configured.");
}

var app = new WoWStreamOverlayApp(
    new CombatLogParser(),
    characterCache,
    battleNetClient,
    gameState,
    gameStateStore,
    TimeSpan.FromSeconds(refreshIntervalSeconds));

await app.RefreshCharacterCacheAsync();

using var streamReader = new StreamReader("TestLog.txt");

while (await streamReader.ReadLineAsync() is { } line)
{
    await app.ProcessCombatLogLineAsync(line);
}
