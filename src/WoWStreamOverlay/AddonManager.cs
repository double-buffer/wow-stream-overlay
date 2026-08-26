namespace WowStreamOverlay;

public enum AddonUpdateResult
{
    Updated,
    UpToDate,
    InstalledVersionIsNewer
}

public sealed class AddonManager
{
    public const string AddonFolderName = "WoWStreamOverlay";
    public const string TocFileName = "WoWStreamOverlay.toc";

    public string BundledPath { get; }
    public string InstalledPath { get; }

    public bool IsInstalled => Directory.Exists(InstalledPath);
    public ReleaseVersion? BundledVersion => ReleaseVersion.TryParse(ApplicationInfo.Version, out var version) ? version : null;
    public ReleaseVersion? InstalledVersion => ReadVersion(Path.Combine(InstalledPath, TocFileName));

    public AddonManager(string bundledPath, string installedPath)
    {
        BundledPath = bundledPath;
        InstalledPath = installedPath;
    }

    public static AddonManager? CreateFromLogsPath(string? logsPath)
    {
        if (string.IsNullOrWhiteSpace(logsPath))
        {
            return null;
        }

        DirectoryInfo logsDirectory;

        try
        {
            logsDirectory = new DirectoryInfo(Path.GetFullPath(logsPath));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        if (!string.Equals(logsDirectory.Name, "Logs", StringComparison.OrdinalIgnoreCase) || logsDirectory.Parent is null)
        {
            return null;
        }

        var bundledPath = Path.Combine(AppContext.BaseDirectory, "Addon", AddonFolderName);
        var installedPath = Path.Combine(logsDirectory.Parent.FullName, "Interface", "AddOns", AddonFolderName);

        return new AddonManager(bundledPath, installedPath);
    }

    public void Install()
    {
        EnsureBundledAddonExists();

        if (Directory.Exists(InstalledPath))
        {
            throw new InvalidOperationException("The addon is already installed. Use 'addon update' to replace it.");
        }

        var bundledVersion = BundledVersion ?? throw new InvalidOperationException("The application has no valid release version.");

        CopyDirectory(BundledPath, InstalledPath);
        WriteVersion(Path.Combine(InstalledPath, TocFileName), bundledVersion);
    }

    public AddonUpdateResult Update()
    {
        EnsureBundledAddonExists();

        if (!Directory.Exists(InstalledPath))
        {
            throw new InvalidOperationException("The addon is not installed. Use 'addon install' first.");
        }

        var bundledVersion = BundledVersion ?? throw new InvalidOperationException("The application has no valid release version.");
        var installedVersion = InstalledVersion;

        if (installedVersion is not null)
        {
            var comparison = bundledVersion.CompareTo(installedVersion.Value);

            if (comparison == 0)
            {
                return AddonUpdateResult.UpToDate;
            }

            if (comparison < 0)
            {
                return AddonUpdateResult.InstalledVersionIsNewer;
            }
        }

        Directory.Delete(InstalledPath, recursive: true);
        CopyDirectory(BundledPath, InstalledPath);
        WriteVersion(Path.Combine(InstalledPath, TocFileName), bundledVersion);

        return AddonUpdateResult.Updated;
    }

    public bool Uninstall()
    {
        if (!Directory.Exists(InstalledPath))
        {
            return false;
        }

        Directory.Delete(InstalledPath, recursive: true);
        return true;
    }

    public static ReleaseVersion? ReadVersion(string tocPath)
    {
        if (!File.Exists(tocPath))
        {
            return null;
        }

        foreach (var line in File.ReadLines(tocPath))
        {
            const string prefix = "## Version:";

            if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = line[prefix.Length..].Trim();
            return ReleaseVersion.TryParse(value, out var version) ? version : null;
        }

        return null;
    }

    private void EnsureBundledAddonExists()
    {
        if (!Directory.Exists(BundledPath) || !File.Exists(Path.Combine(BundledPath, TocFileName)))
        {
            throw new InvalidOperationException($"Bundled addon not found: {BundledPath}");
        }
    }

    private static void WriteVersion(string tocPath, ReleaseVersion version)
    {
        var lines = File.ReadAllLines(tocPath);

        for (var index = 0; index < lines.Length; index++)
        {
            if (!lines[index].StartsWith("## Version:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lines[index] = $"## Version: {version}";
            File.WriteAllLines(tocPath, lines);
            return;
        }

        throw new InvalidOperationException($"Addon version header not found: {tocPath}");
    }

    private static void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);

        foreach (var directory in Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, directory);
            Directory.CreateDirectory(Path.Combine(destinationPath, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, file);
            var destinationFile = Path.Combine(destinationPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(file, destinationFile, overwrite: true);
        }
    }
}
