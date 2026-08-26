using Xunit;

namespace WowStreamOverlay.Tests;

public class CommandLineTests
{
    [Theory]
    [InlineData("version")]
    [InlineData("--version")]
    public void RecognizesVersionRequests(string argument)
    {
        Assert.True(CommandLine.IsVersionRequest([argument]));
    }

    [Fact]
    public void RejectsInvalidVersionRequests()
    {
        Assert.False(CommandLine.IsVersionRequest([]));
        Assert.False(CommandLine.IsVersionRequest(["--version", "extra"]));
        Assert.False(CommandLine.IsVersionRequest(["-v"]));
    }
}
