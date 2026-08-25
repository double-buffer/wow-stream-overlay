using Xunit;

namespace WowStreamOverlay.CombatLog.Tests;

public class ChallengeModeTests
{
    [Fact]
    public void ParseChallengeModeStart()
    {
        var parser = new CombatLogParser();
        const string line = "8/25/2026 09:00:00.0000  CHALLENGE_MODE_START,\"Antre de Nalorakk\",2825,999,9,[1,2,3,4]";

        var result = parser.ParseLine(line);

        Assert.Equal(ParseStatus.Parsed, result.Status);
        var eventValue = Assert.IsType<ChallengeModeStartedEvent>(result.Event);
        Assert.Equal("Antre de Nalorakk", eventValue.DungeonName);
        Assert.Equal(9, eventValue.Level);
    }

    [Fact]
    public void IgnoreChallengeModeEndWithoutActiveChallenge()
    {
        var parser = new CombatLogParser();
        const string line = "8/25/2026 09:00:00.0000  CHALLENGE_MODE_END,2825,0,0,0,0.000000,0.000000";

        var result = parser.ParseLine(line);

        Assert.Equal(ParseStatus.Ignored, result.Status);
        Assert.Null(result.Event);
    }

    [Fact]
    public void ParseChallengeModeEndAfterStart()
    {
        var parser = new CombatLogParser();
        parser.ParseLine("8/25/2026 09:00:00.0000  CHALLENGE_MODE_START,\"Allée du meurtre\",2813,999,8,[1,2,3,4]");

        var result = parser.ParseLine("8/25/2026 09:26:12.7160  CHALLENGE_MODE_END,2813,1,8,1572716,283.589783,2032.212158");

        Assert.Equal(ParseStatus.Parsed, result.Status);
        var eventValue = Assert.IsType<ChallengeModeEndedEvent>(result.Event);
        Assert.True(eventValue.Completed);
    }

    [Fact]
    public void ChallengeModeEndClearsActiveChallenge()
    {
        var parser = new CombatLogParser();
        parser.ParseLine("8/25/2026 09:00:00.0000  CHALLENGE_MODE_START,\"Le val Aveuglant\",2859,999,10,[1,2,3,4]");
        parser.ParseLine("8/25/2026 09:26:42.4320  CHALLENGE_MODE_END,2859,1,9,1602432,294.115997,2032.212158");

        var result = parser.ParseLine("8/25/2026 09:26:43.0000  CHALLENGE_MODE_END,2859,0,0,0,0.000000,0.000000");

        Assert.Equal(ParseStatus.Ignored, result.Status);
        Assert.Null(result.Event);
    }

    [Fact]
    public void InvalidChallengeModeStartReturnsMalformed()
    {
        var parser = new CombatLogParser();

        var result = parser.ParseLine("8/25/2026 09:00:00.0000  CHALLENGE_MODE_START,\"Antre de Nalorakk\",2825,999,nope,[1,2,3,4]");

        Assert.Equal(ParseStatus.Malformed, result.Status);
        Assert.Null(result.Event);
    }

    [Fact]
    public void MalformedChallengeModeEndDoesNotClearActiveChallenge()
    {
        var parser = new CombatLogParser();
        parser.ParseLine("8/25/2026 09:00:00.0000  CHALLENGE_MODE_START,\"Antre de Nalorakk\",2825,999,9,[1,2,3,4]");

        var malformedResult = parser.ParseLine("8/25/2026 09:35:59.9930  CHALLENGE_MODE_END,2825,nope,9,2159993,270.312622,2032.212158");
        var validResult = parser.ParseLine("8/25/2026 09:35:59.9940  CHALLENGE_MODE_END,2825,1,9,2159993,270.312622,2032.212158");

        Assert.Equal(ParseStatus.Malformed, malformedResult.Status);
        Assert.Null(malformedResult.Event);
        Assert.Equal(ParseStatus.Parsed, validResult.Status);
        Assert.IsType<ChallengeModeEndedEvent>(validResult.Event);
    }
}
