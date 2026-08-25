using Xunit;

namespace WowStreamOverlay.CombatLog.Tests;

public class PlayerObservedTests
{
    [Fact]
    public void ParseLocalPlayerFromSource()
    {
        var parser = new CombatLogParser();
        var line = TestDataReader.ReadLine("V22/player-observed-source.txt");

        var result = parser.ParseLine(line);

        Assert.Equal(ParseStatus.Parsed, result.Status);
        var eventValue = Assert.IsType<PlayerObservedEvent>(result.Event);
        Assert.Equal("Player-510-00626ADE", eventValue.Guid);
        Assert.Equal("Naaruël-Vol'jin-EU", eventValue.Name);
    }

    [Fact]
    public void ParseLocalPlayerFromDestination()
    {
        var parser = new CombatLogParser();
        const string line = "8/25/2026 08:10:32.0262  SPELL_AURA_APPLIED,Player-509-05585900,\"OtherPlayer\",0x548,0x80000000,Player-510-00626ADE,\"Naaruël-Vol'jin-EU\",0x511,0x80000000,21562,\"Power Word: Fortitude\",0x2,BUFF";

        var result = parser.ParseLine(line);

        Assert.Equal(ParseStatus.Parsed, result.Status);
        var eventValue = Assert.IsType<PlayerObservedEvent>(result.Event);
        Assert.Equal("Player-510-00626ADE", eventValue.Guid);
        Assert.Equal("Naaruël-Vol'jin-EU", eventValue.Name);
    }

    [Fact]
    public void IgnoreSpellWithoutLocalPlayer()
    {
        var parser = new CombatLogParser();
        const string line = "8/25/2026 08:10:32.4702  SPELL_AURA_APPLIED,Player-509-05585900,\"Myruh\",0x548,0x80000000,Player-509-05585900,\"Myruh\",0x548,0x80000000,43308,\"Fishing\",0x1,BUFF";

        var result = parser.ParseLine(line);

        Assert.Equal(ParseStatus.Ignored, result.Status);
        Assert.Null(result.Event);
    }

    [Fact]
    public void MalformedSpellReturnsMalformed()
    {
        var parser = new CombatLogParser();

        var result = parser.ParseLine("8/25/2026 08:10:32.0262  SPELL_AURA_APPLIED,Player-510-00626ADE");

        Assert.Equal(ParseStatus.Malformed, result.Status);
        Assert.Null(result.Event);
    }
}
