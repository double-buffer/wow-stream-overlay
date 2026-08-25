namespace WowStreamOverlay;

/// <summary>
/// Current state exposed by the application.
/// </summary>
public sealed class GameState
{
    public string? CurrentCharacterGuid { get; set; }
    public CharacterProfile? Character { get; set; }
    public MythicPlusState? MythicPlus { get; set; }
}

/// <summary>
/// Current Mythic+ state.
/// </summary>
/// <param name="DungeonName">Dungeon name.</param>
/// <param name="Level">Keystone level.</param>
public sealed record MythicPlusState(string DungeonName, int Level);
