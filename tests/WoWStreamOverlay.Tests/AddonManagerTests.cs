using Xunit;

namespace WowStreamOverlay.Tests;

public class AddonManagerTests
{
    [Fact]
    public void InstallPreservesBundledAddonVersion()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-{Guid.NewGuid():N}");
        var bundledPath = Path.Combine(rootPath, "bundled");
        var installedPath = Path.Combine(rootPath, "installed");

        try
        {
            Directory.CreateDirectory(bundledPath);
            File.WriteAllText(
                Path.Combine(bundledPath, AddonManager.TocFileName),
                "## Interface: 120100\n## Version: 1.0.0\n\nWoWStreamOverlay.lua\n");
            File.WriteAllText(Path.Combine(bundledPath, "WoWStreamOverlay.lua"), "-- test");

            var manager = new AddonManager(bundledPath, installedPath);
            manager.Install();

            Assert.Equal(Parse("1.0.0"), manager.BundledVersion);
            Assert.Equal(Parse("1.0.0"), manager.InstalledVersion);
            Assert.True(File.Exists(Path.Combine(installedPath, "WoWStreamOverlay.lua")));
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    private static ReleaseVersion Parse(string value)
    {
        Assert.True(ReleaseVersion.TryParse(value, out var version));
        return version;
    }
}
