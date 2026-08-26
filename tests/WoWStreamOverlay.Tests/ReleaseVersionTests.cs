using Xunit;

namespace WowStreamOverlay.Tests;

public class ReleaseVersionTests
{
    [Theory]
    [InlineData("1.0.0-dev.local", 1, 0, 0, ReleaseStage.Dev, 0)]
    [InlineData("1.0.0-dev.1", 1, 0, 0, ReleaseStage.Dev, 1)]
    [InlineData("1.0.0-alpha.3", 1, 0, 0, ReleaseStage.Alpha, 3)]
    [InlineData("1.0.0-ptr.4", 1, 0, 0, ReleaseStage.Ptr, 4)]
    [InlineData("1.0.0-rc.5", 1, 0, 0, ReleaseStage.Rc, 5)]
    [InlineData("1.0.0", 1, 0, 0, ReleaseStage.Release, 0)]
    public void ParseReleaseVersion(string value, int major, int minor, int patch, ReleaseStage stage, int buildNumber)
    {
        Assert.True(ReleaseVersion.TryParse(value, out var version));
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(stage, version.Stage);
        Assert.Equal(buildNumber, version.BuildNumber);
        Assert.Equal(value, version.ToString());
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0-dev")]
    [InlineData("1.0.0-dev.0")]
    [InlineData("1.0.0-beta.1")]
    [InlineData("nope")]
    public void RejectInvalidReleaseVersion(string value)
    {
        Assert.False(ReleaseVersion.TryParse(value, out _));
    }

    [Fact]
    public void PrereleaseStagesUseProductReleaseOrder()
    {
        Assert.True(Parse("1.0.0-dev.local") < Parse("1.0.0-dev.1"));
        Assert.True(Parse("1.0.0-dev.2") < Parse("1.0.0-alpha.3"));
        Assert.True(Parse("1.0.0-alpha.3") < Parse("1.0.0-ptr.4"));
        Assert.True(Parse("1.0.0-ptr.4") < Parse("1.0.0-rc.5"));
        Assert.True(Parse("1.0.0-rc.5") < Parse("1.0.0"));
        Assert.True(Parse("1.0.0") < Parse("1.1.0-dev.6"));
    }

    [Fact]
    public async Task ReadAddonVersionFromToc()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-{Guid.NewGuid():N}.toc");

        try
        {
            await File.WriteAllTextAsync(path, "## Interface: 120100\n## Version: 1.0.0-ptr.4\n");

            var version = AddonManager.ReadVersion(path);

            Assert.Equal(Parse("1.0.0-ptr.4"), version);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ReleaseVersion Parse(string value)
    {
        Assert.True(ReleaseVersion.TryParse(value, out var version));
        return version;
    }
}
