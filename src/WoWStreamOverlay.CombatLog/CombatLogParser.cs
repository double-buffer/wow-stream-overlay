using System.Globalization;

namespace WowStreamOverlay.CombatLog;

/// <summary>
/// Parses World of Warcraft combat log lines.
/// </summary>
public class CombatLogParser
{
    private bool _isChallengeStarted = false;

    /// <summary>
    /// Parses a single combat log line.
    /// </summary>
    /// <param name="line">Combat log line to parse.</param>
    /// <returns>The result of the parsing operation.</returns>
    public ParseResult ParseLine(ReadOnlySpan<char> line)
    {
        const string dateSeparator = "  ";
        var separator = line.IndexOf(dateSeparator);

        if (separator < 0)
        {
            return new ParseResult(ParseStatus.Malformed, null);
        }

        var logData = line[(separator + dateSeparator.Length)..];

        Span<Range> ranges = stackalloc Range[50];
        var itemCount = logData.Split(ranges, ',');

        var eventName = logData[ranges[0]];

        return eventName switch
        {
            "COMBAT_LOG_VERSION" => ParseCombatLogVersion(logData, ranges[..itemCount]),
            "CHALLENGE_MODE_START" => ParseChallengeModeStarted(logData, ranges[..itemCount]),
            "CHALLENGE_MODE_END" => ParseChallengeModeEnded(logData, ranges[..itemCount]),
            _ when eventName.StartsWith("SPELL_", StringComparison.Ordinal)  => ParsePlayerObserved(logData, ranges[..itemCount]),
            _ => new ParseResult(ParseStatus.Ignored, null)
        };
            
    }

    private ParseResult ParseCombatLogVersion(ReadOnlySpan<char> logData, ReadOnlySpan<Range> ranges)
    {
        if (ranges.Length < 8)
        {
            return new ParseResult(ParseStatus.Malformed, null);
        }

        if (!int.TryParse(logData[ranges[1]], out var version))
        {
            return new(ParseStatus.Malformed, null);
        }

        if (!int.TryParse(logData[ranges[3]], out var advancedLogEnabled))
        {
            return new(ParseStatus.Malformed, null);
        }

        if (!int.TryParse(logData[ranges[7]], out var projectId))
        {
            return new(ParseStatus.Malformed, null);
        }

        var buildVersion = logData[ranges[5]].ToString();

        return new(ParseStatus.Parsed, new CombatLogVersionEvent(version, advancedLogEnabled != 0, buildVersion, projectId));
    }

    private ParseResult ParseChallengeModeStarted(ReadOnlySpan<char> logData, ReadOnlySpan<Range> ranges)
    {
        if (ranges.Length < 5)
        {
            return new ParseResult(ParseStatus.Malformed, null);
        }

        if (!int.TryParse(logData[ranges[4]], out var level))
        {
            return new(ParseStatus.Malformed, null);
        }

        _isChallengeStarted = true;
        var dungeonName = logData[ranges[1]].Trim('"').ToString();

        return new(ParseStatus.Parsed, new ChallengeModeStartedEvent(dungeonName, level));
    }

    private ParseResult ParseChallengeModeEnded(ReadOnlySpan<char> logData, ReadOnlySpan<Range> ranges)
    {
        if (!_isChallengeStarted)
        {
            return new ParseResult(ParseStatus.Ignored, null);
        }

        if (ranges.Length < 7)
        {
            return new ParseResult(ParseStatus.Malformed, null);
        }

        if (!int.TryParse(logData[ranges[2]], out var completed))
        {
            return new(ParseStatus.Malformed, null);
        }

        _isChallengeStarted = false;

        return new(ParseStatus.Parsed, new ChallengeModeEndedEvent(completed != 0));
    }

    private ParseResult ParsePlayerObserved(ReadOnlySpan<char> logData, ReadOnlySpan<Range> ranges)
    {
        if (ranges.Length < 8)
        {
            return new ParseResult(ParseStatus.Malformed, null);
        }

        if (IsLocalPlayer(logData[ranges[3]]))
        {
            return ParsePlayer(logData[ranges[1]], logData[ranges[2]]);
        }

        if (IsLocalPlayer(logData[ranges[7]]))
        {
            return ParsePlayer(logData[ranges[5]], logData[ranges[6]]);
        }

        return new ParseResult(ParseStatus.Ignored, null);
    }

    private static ParseResult ParsePlayer(ReadOnlySpan<char> guidValue, ReadOnlySpan<char> nameValue)
    {
        if (guidValue.SequenceEqual("nil") || nameValue.SequenceEqual("nil"))
        {
            return new ParseResult(ParseStatus.Ignored, null);
        }

        var guid = guidValue.ToString();
        var name = nameValue.Trim('"').ToString();

        return new ParseResult(ParseStatus.Parsed, new PlayerObservedEvent(guid, name));
    }

    private static bool IsLocalPlayer(ReadOnlySpan<char> value)
    {
        const int affiliationMine = 0x001;
        const int controlPlayer = 0x100;
        const int typePlayer = 0x400;
        const int localPlayerMask = affiliationMine | controlPlayer | typePlayer;

        if (!value.StartsWith("0x", StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var flags))
        {
            return false;
        }

        return (flags & localPlayerMask) == localPlayerMask;
    }
}
