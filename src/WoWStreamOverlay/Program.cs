using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WowStreamOverlay;
using WowStreamOverlay.CombatLog;

Console.WriteLine("WoW Stream Overlay v0.1.0 · https://github.com/double-buffer/wow-stream-overlay");

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets(typeof(BattleNetClient).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var logsPath = configuration["Wow:LogsPath"];

if (string.IsNullOrWhiteSpace(logsPath))
{
    Console.Error.WriteLine("Error: Wow:LogsPath is not configured.");
    return;
}

if (!Directory.Exists(logsPath))
{
    Console.Error.WriteLine($"Error: World of Warcraft logs folder does not exist: {logsPath}");
    return;
}

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

var webBuilder = WebApplication.CreateSlimBuilder(args);
webBuilder.WebHost.UseUrls("http://127.0.0.1:37231");

var webServer = webBuilder.Build();
webServer.MapGet("/api/state", () => Results.Text(GameStateSerializer.Serialize(app.State), "application/json"));

var combatLogReader = new CombatLogReader(logsPath);
using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

await webServer.StartAsync(shutdown.Token);

var logTask = combatLogReader.ProcessLogFilesAsync(app.ProcessCombatLogLineAsync, shutdown.Token);
var serverTask = webServer.WaitForShutdownAsync(shutdown.Token);

try
{
    await Task.WhenAny(logTask, serverTask);
}
finally
{
    await shutdown.CancelAsync();
}

try
{
    await Task.WhenAll(logTask, serverTask);
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
}
