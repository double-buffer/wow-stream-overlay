using Xunit;

namespace WowStreamOverlay.CombatLog.Tests;

public class CombatLogVersionTests
{
    [Fact]
    public void ParseV22VersionEvent()
    {
        var parser = new CombatLogParser();
        var line = TestDataReader.ReadLine("V22/combat-log-version.txt");

        var result = parser.ParseLine(line);

        Assert.Equal(ParseStatus.Parsed, result.Status);
        var eventValue = Assert.IsType<CombatLogVersionEvent>(result.Event);
        Assert.Equal(22, eventValue.Version);
        Assert.True(eventValue.AdvancedLogEnabled);
        Assert.Equal("12.1.0", eventValue.BuildVersion);
        Assert.Equal(1, eventValue.ProjectId);
    }

    [Theory]
    [InlineData("8/25/2026 08:10:31.9862  COMBAT_LOG_VERSION,22")]
    [InlineData("8/25/2026 08:10:31.9862  COMBAT_LOG_VERSION,nope,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,12.1.0,PROJECT_ID,1")]
    [InlineData("8/25/2026 08:10:31.9862  COMBAT_LOG_VERSION,22,ADVANCED_LOG_ENABLED,nope,BUILD_VERSION,12.1.0,PROJECT_ID,1")]
    [InlineData("8/25/2026 08:10:31.9862  COMBAT_LOG_VERSION,22,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,12.1.0,PROJECT_ID,nope")]
    public void InvalidVersionEventReturnsMalformed(string line)
    {
        var parser = new CombatLogParser();

        var result = parser.ParseLine(line);

        Assert.Equal(ParseStatus.Malformed, result.Status);
        Assert.Null(result.Event);
    }
}
