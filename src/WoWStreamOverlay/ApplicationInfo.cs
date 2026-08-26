using System.Reflection;

namespace WowStreamOverlay;

public static class ApplicationInfo
{
    public const string Name = "WoW Stream Overlay";
    public const string RepositoryUrl = "https://github.com/double-buffer/wow-stream-overlay";

    public static string Version
    {
        get
        {
            var informationalVersion = typeof(ApplicationInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var metadataSeparator = informationalVersion.IndexOf('+');
                return metadataSeparator < 0 ? informationalVersion : informationalVersion[..metadataSeparator];
            }

            return typeof(ApplicationInfo).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        }
    }
}
