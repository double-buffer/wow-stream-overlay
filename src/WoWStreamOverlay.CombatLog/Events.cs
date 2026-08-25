namespace WowStreamOverlay.CombatLog;

/// <summary>
/// Base type for events produced by the combat log parser.
/// </summary>
public abstract record CombatLogEvent;

/// <summary>
/// Describes the combat log format and client build currently in use.
/// </summary>
/// <param name="Version">Combat log format version.</param>
/// <param name="AdvancedLogEnabled">Whether advanced combat logging is enabled.</param>
/// <param name="BuildVersion">World of Warcraft client build version.</param>
/// <param name="ProjectId">Blizzard project identifier.</param>
public sealed record CombatLogVersionEvent(int Version, bool AdvancedLogEnabled, string BuildVersion, int ProjectId) : CombatLogEvent;

/// <summary>
/// Indicates that a player character was observed in the combat log.
/// </summary>
/// <param name="Guid">Player GUID.</param>
/// <param name="Name">Player character name.</param>
public sealed record PlayerObservedEvent(string Guid, string Name) : CombatLogEvent;

/// <summary>
/// Indicates that a Mythic+ challenge has started.
/// </summary>
/// <param name="DungeonName">Dungeon name.</param>
/// <param name="Level">Keystone level.</param>
public sealed record ChallengeModeStartedEvent(string DungeonName, int Level) : CombatLogEvent;

/// <summary>
/// Indicates that a Mythic+ challenge has ended.
/// </summary>
/// <param name="Completed">Whether the challenge was completed successfully.</param>
public sealed record ChallengeModeEndedEvent(bool Completed) : CombatLogEvent;
