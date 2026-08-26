using Microsoft.Extensions.Configuration;

namespace WowStreamOverlay;

public static class CommandLine
{
    public static bool IsHelpRequest(string[] args)
    {
        return args.Length == 1 && args[0] is "help" or "--help" or "-h";
    }

    public static void PrintHelp()
    {
        Console.WriteLine($"{ApplicationInfo.Name} {ApplicationInfo.Version}");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  wow-stream-overlay                  Run the application");
        Console.WriteLine("  wow-stream-overlay status           Show configuration and runtime status");
        Console.WriteLine("  wow-stream-overlay addon install    Install the bundled WoW addon");
        Console.WriteLine("  wow-stream-overlay addon update     Update the installed WoW addon");
        Console.WriteLine("  wow-stream-overlay addon uninstall  Uninstall the WoW addon");
        Console.WriteLine("  wow-stream-overlay help             Show this help");
    }

    public static async Task<bool> TryExecuteAsync(string[] args, IConfiguration configuration)
    {
        if (args.Length == 0)
        {
            return false;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "status" when args.Length == 1:
                await PrintStatusAsync(configuration);
                return true;

            case "addon":
                ExecuteAddonCommand(args, configuration);
                return true;

            default:
                Console.Error.WriteLine($"Unknown command: {string.Join(' ', args)}");
                Console.Error.WriteLine();
                PrintHelp();
                Environment.ExitCode = 1;
                return true;
        }
    }

    private static async Task PrintStatusAsync(IConfiguration configuration)
    {
        Console.WriteLine($"{ApplicationInfo.Name} {ApplicationInfo.Version}");

        var logsPath = configuration["Wow:LogsPath"];

        Console.WriteLine();
        Console.WriteLine("WoW");
        WriteStatus("Logs", string.IsNullOrWhiteSpace(logsPath) ? "Not configured" : logsPath);
        WriteAddonStatus(logsPath);

        var clientId = configuration["BattleNet:ClientId"];
        var clientSecret = configuration["BattleNet:ClientSecret"];
        var region = configuration["BattleNet:Region"] ?? "eu";
        var locale = configuration["BattleNet:Locale"] ?? "fr_FR";

        Console.WriteLine();
        Console.WriteLine("Battle.net");
        WriteStatus("Integration", !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret) ? "Configured" : "Not configured");
        WriteStatus("Region", region);
        WriteStatus("Locale", locale);

        var host = configuration["Web:Host"];

        if (string.IsNullOrWhiteSpace(host))
        {
            host = WebServer.DefaultHost;
        }

        var port = WebServer.DefaultPort;
        var configuredPort = configuration["Web:Port"];
        var validPort = string.IsNullOrWhiteSpace(configuredPort) || int.TryParse(configuredPort, out port) && port is >= 1 and <= 65535;

        Console.WriteLine();
        Console.WriteLine("Web");

        if (!validPort)
        {
            WriteStatus("Address", $"Invalid port: {configuredPort}");
            WriteStatus("Server", "Unknown");
        }
        else
        {
            var address = $"http://{host}:{port}";
            WriteStatus("Address", address);
            WriteStatus("Server", await IsServerOnlineAsync(host, port) ? "Online" : "Offline");

            Console.WriteLine();
            Console.WriteLine("Overlays");

            var overlays = configuration.GetSection("Overlays").GetChildren().ToArray();

            if (overlays.Length == 0)
            {
                WriteStatus("Status", "None configured");
            }
            else
            {
                foreach (var overlay in overlays)
                {
                    WriteStatus(overlay.Key, $"{address}/overlay/{overlay.Key}");
                }
            }
        }

        var charactersPath = configuration["Storage:CharactersPath"];
        var statePath = configuration["Storage:StatePath"];

        Console.WriteLine();
        Console.WriteLine("Storage");
        WriteStatus("Characters", string.IsNullOrWhiteSpace(charactersPath) ? "characters.json" : charactersPath);
        WriteStatus("State", string.IsNullOrWhiteSpace(statePath) ? "state.json" : statePath);
    }

    private static void WriteAddonStatus(string? logsPath)
    {
        var addonManager = AddonManager.CreateFromLogsPath(logsPath);

        if (addonManager is null)
        {
            WriteStatus("Addon", "Unavailable (invalid or missing Logs path)");
            return;
        }

        WriteStatus("Addon", addonManager.IsInstalled ? "Installed" : "Not installed");
        WriteStatus("Addon path", addonManager.InstalledPath);

        var bundledVersion = addonManager.BundledVersion;
        var installedVersion = addonManager.InstalledVersion;

        WriteStatus("Bundled version", FormatVersion(bundledVersion));

        if (!addonManager.IsInstalled)
        {
            return;
        }

        WriteStatus("Installed version", FormatVersion(installedVersion));

        if (bundledVersion is null || installedVersion is null)
        {
            WriteStatus("Update", "Unknown");
        }
        else if (bundledVersion.Value > installedVersion.Value)
        {
            WriteStatus("Update", "Available");
        }
        else if (bundledVersion.Value == installedVersion.Value)
        {
            WriteStatus("Update", "Up to date");
        }
        else
        {
            WriteStatus("Update", "Installed version is newer");
        }
    }

    private static void ExecuteAddonCommand(string[] args, IConfiguration configuration)
    {
        if (args.Length != 2)
        {
            PrintAddonUsage();
            Environment.ExitCode = 1;
            return;
        }

        var addonManager = AddonManager.CreateFromLogsPath(configuration["Wow:LogsPath"]);

        if (addonManager is null)
        {
            Console.Error.WriteLine("Unable to locate the WoW addon folder. Configure Wow:LogsPath to the _retail_/Logs directory first.");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            switch (args[1].ToLowerInvariant())
            {
                case "install":
                    addonManager.Install();
                    Console.WriteLine($"WoWStreamOverlay addon {FormatVersion(addonManager.BundledVersion)} installed:");
                    Console.WriteLine(addonManager.InstalledPath);
                    break;

                case "update":
                    var result = addonManager.Update();

                    switch (result)
                    {
                        case AddonUpdateResult.Updated:
                            Console.WriteLine($"WoWStreamOverlay addon updated to {FormatVersion(addonManager.BundledVersion)}.");
                            break;

                        case AddonUpdateResult.UpToDate:
                            Console.WriteLine($"WoWStreamOverlay addon is already up to date ({FormatVersion(addonManager.InstalledVersion)}).");
                            break;

                        case AddonUpdateResult.InstalledVersionIsNewer:
                            Console.WriteLine(
                                $"Installed addon {FormatVersion(addonManager.InstalledVersion)} is newer than bundled version " +
                                $"{FormatVersion(addonManager.BundledVersion)}. Nothing was changed.");
                            break;
                    }
                    break;

                case "uninstall":
                    if (addonManager.Uninstall())
                    {
                        Console.WriteLine("WoWStreamOverlay addon uninstalled.");
                    }
                    else
                    {
                        Console.WriteLine("WoWStreamOverlay addon is not installed.");
                    }
                    break;

                default:
                    PrintAddonUsage();
                    Environment.ExitCode = 1;
                    break;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Addon operation failed: {exception.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static async Task<bool> IsServerOnlineAsync(string host, int port)
    {
        var probeHost = host == "0.0.0.0" ? "127.0.0.1" : host;

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(1)
            };

            using var response = await httpClient.GetAsync($"http://{probeHost}:{port}/api/state");
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return false;
        }
    }

    private static string FormatVersion(ReleaseVersion? version)
    {
        return version?.ToString() ?? "Unknown";
    }

    private static void WriteStatus(string label, string value)
    {
        Console.WriteLine($"  {label,-18}{value}");
    }

    private static void PrintAddonUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  wow-stream-overlay addon install");
        Console.Error.WriteLine("  wow-stream-overlay addon update");
        Console.Error.WriteLine("  wow-stream-overlay addon uninstall");
    }
}
