using Xunit;

namespace WowStreamOverlay.Tests;

public class ReleaseVersionTests
{
    [Theory]
    [InlineData("1.0.0-dev.1", 1, 0, 0, ReleaseStage.Dev, 1)]
    [InlineData("1.0.0-beta.2", 1, 0, 0, ReleaseStage.Beta, 2)]
    [InlineData("1.0.0-rc.3", 1, 0, 0, ReleaseStage.Rc, 3)]
    [InlineData("1.0.0", 1, 0, 0, ReleaseStage.Release, 0)]
    public void ParseReleaseVersion(string value, int major, int minor, int patch, ReleaseStage stage, int stageNumber)
    {
        Assert.True(ReleaseVersion.TryParse(value, out var version));
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(stage, version.Stage);
        Assert.Equal(stageNumber, version.StageNumber);
        Assert.Equal(value, version.ToString());
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0-dev")]
    [InlineData("1.0.0-dev.0")]
    [InlineData("1.0.0-alpha.1")]
    [InlineData("nope")]
    public void RejectInvalidReleaseVersion(string value)
    {
        Assert.False(ReleaseVersion.TryParse(value, out _));
    }

    [Fact]
    public void PrereleaseStagesUseProductReleaseOrder()
    {
        Assert.True(Parse("1.0.0-dev.1") < Parse("1.0.0-dev.2"));
        Assert.True(Parse("1.0.0-dev.2") < Parse("1.0.0-beta.1"));
        Assert.True(Parse("1.0.0-beta.1") < Parse("1.0.0-rc.1"));
        Assert.True(Parse("1.0.0-rc.1") < Parse("1.0.0"));
        Assert.True(Parse("1.0.0") < Parse("1.1.0-dev.1"));
    }

    [Fact]
    public async Task ReadAddonVersionFromToc()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wow-stream-overlay-{Guid.NewGuid():N}.toc");

        try
        {
            await File.WriteAllTextAsync(path, "## Interface: 120100\n## Version: 1.0.0-dev.1\n");

            var version = AddonManager.ReadVersion(path);

            Assert.Equal(Parse("1.0.0-dev.1"), version);
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
