namespace WowStreamOverlay.CombatLog;

/// <summary>
/// Result of parsing a combat log line.
/// </summary>
/// <param name="Status">Parsing status.</param>
/// <param name="Event">Parsed event, if any.</param>
public readonly record struct ParseResult(ParseStatus Status, CombatLogEvent? Event);

/// <summary>
/// Status returned after parsing a combat log line.
/// </summary>
public enum ParseStatus
{
    /// <summary>
    /// The line was successfully parsed.
    /// </summary>
    Parsed,

    /// <summary>
    /// The line contains an event that is not handled by the parser.
    /// </summary>
    Ignored,

    /// <summary>
    /// The line could not be parsed because it is invalid or incomplete.
    /// </summary>
    Malformed
}
