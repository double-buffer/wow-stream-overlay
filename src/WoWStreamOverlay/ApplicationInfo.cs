namespace WowStreamOverlay;

public static class ApplicationInfo
{
    public const string Name = "WoW Stream Overlay";
    public const string RepositoryUrl = "https://github.com/double-buffer/wow-stream-overlay";

    public static string Version => typeof(ApplicationInfo).Assembly.GetName().Version?.ToString(3) ?? "unknown";
}
