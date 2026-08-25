using Xunit;

namespace WowStreamOverlay.CombatLog.Tests;

public class ParserTests
{
    [Fact]
    public void ParseLineWithoutDateSeparatorReturnsMalformed()
    {
        var parser = new CombatLogParser();

        var result = parser.ParseLine("COMBAT_LOG_VERSION,22,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,12.1.0,PROJECT_ID,1");

        Assert.Equal(ParseStatus.Malformed, result.Status);
        Assert.Null(result.Event);
    }

    [Fact]
    public void ParseUnhandledEventReturnsIgnored()
    {
        var parser = new CombatLogParser();

        var result = parser.ParseLine("8/25/2026 08:10:31.9862  ZONE_CHANGE,0,\"UNKNOWN AREA\",0");

        Assert.Equal(ParseStatus.Ignored, result.Status);
        Assert.Null(result.Event);
    }
}
