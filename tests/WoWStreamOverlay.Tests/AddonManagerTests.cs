using Xunit;

namespace WowStreamOverlay.Tests;

public class AddonManagerTests
{
    [Fact]
    public void InstallStampsAddonWithApplicationVersion()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-{Guid.NewGuid():N}");
        var bundledPath = Path.Combine(rootPath, "bundled");
        var installedPath = Path.Combine(rootPath, "installed");

        try
        {
            Directory.CreateDirectory(bundledPath);
            File.WriteAllText(
                Path.Combine(bundledPath, AddonManager.TocFileName),
                "## Interface: 120100\n## Version: 1.0.0-dev.local\n\nWoWStreamOverlay.lua\n");
            File.WriteAllText(Path.Combine(bundledPath, "WoWStreamOverlay.lua"), "-- test");

            var manager = new AddonManager(bundledPath, installedPath);
            manager.Install();

            Assert.True(ReleaseVersion.TryParse(ApplicationInfo.Version, out var expectedVersion));
            Assert.Equal(expectedVersion, manager.InstalledVersion);
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
}
