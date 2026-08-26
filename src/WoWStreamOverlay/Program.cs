using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WowStreamOverlay;
using WowStreamOverlay.CombatLog;

if (CommandLine.IsHelpRequest(args))
{
    CommandLine.PrintHelp();
    return;
}

if (CommandLine.IsVersionRequest(args))
{
    CommandLine.PrintVersion();
    return;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets(typeof(BattleNetClient).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

if (await CommandLine.TryExecuteAsync(args, configuration))
{
    return;
}

Console.WriteLine($"{ApplicationInfo.Name} v{ApplicationInfo.Version} · {ApplicationInfo.RepositoryUrl}");

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

var combatLogReader = new CombatLogReader(logsPath);

if (gameState.Character is null)
{
    var lastPlayerObserved = await combatLogReader.FindLastPlayerObservedAsync();

    if (lastPlayerObserved is not null)
    {
        Console.WriteLine($"Bootstrapping character from combat log: {lastPlayerObserved.Name}");
        await app.BootstrapPlayerAsync(lastPlayerObserved);
    }
}

var overlays = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

foreach (var overlay in configuration.GetSection("Overlays").GetChildren())
{
    var templatePath = overlay["Template"];

    if (string.IsNullOrWhiteSpace(templatePath))
    {
        continue;
    }

    overlays[overlay.Key] = Path.IsPathRooted(templatePath)
        ? templatePath
        : Path.Combine(AppContext.BaseDirectory, templatePath);
}

var webHost = configuration["Web:Host"];

if (string.IsNullOrWhiteSpace(webHost))
{
    webHost = WebServer.DefaultHost;
}

var webPort = WebServer.DefaultPort;
var configuredWebPort = configuration["Web:Port"];

if (!string.IsNullOrWhiteSpace(configuredWebPort))
{
    if (!int.TryParse(configuredWebPort, out webPort) || webPort < 1 || webPort > 65535)
    {
        Console.Error.WriteLine($"Error: Web:Port is invalid: {configuredWebPort}");
        return;
    }
}

using var shutdownTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdownTokenSource.Cancel();
};

var webServer = WebServer.Create(
    app.State,
    host: webHost,
    port: webPort,
    overlays: overlays,
    applicationStopping: shutdownTokenSource.Token);

await webServer.StartAsync(shutdownTokenSource.Token);

var logTask = combatLogReader.ProcessLogFilesAsync(app.ProcessCombatLogLineAsync, shutdownTokenSource.Token);
var serverTask = webServer.WaitForShutdownAsync(shutdownTokenSource.Token);

try
{
    await Task.WhenAny(logTask, serverTask);

    shutdownTokenSource.Cancel();

    await Task.WhenAll(logTask, serverTask);
}
catch (OperationCanceledException) when (shutdownTokenSource.IsCancellationRequested)
{
}
