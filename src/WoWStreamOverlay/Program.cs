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

var charactersPath = configuration["Cache:CharactersPath"];

if (string.IsNullOrWhiteSpace(charactersPath))
{
    charactersPath = "characters.json";
}

var characterCache = new CharacterCache(charactersPath);
await characterCache.LoadAsync();

var clientId = configuration["BattleNet:ClientId"];
var clientSecret = configuration["BattleNet:ClientSecret"];
var region = configuration["BattleNet:Region"] ?? "eu";
var locale = configuration["BattleNet:Locale"] ?? "fr_FR";

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

var app = new WoWStreamOverlayApp(new CombatLogParser(), characterCache, battleNetClient);

using var streamReader = new StreamReader("TestLog.txt");

while (await streamReader.ReadLineAsync() is { } line)
{
    await app.ProcessCombatLogLineAsync(line);
}
