namespace WowStreamOverlay;

/// <summary>
/// Describes a World of Warcraft character profile.
/// </summary>
/// <param name="Name">Character name.</param>
/// <param name="Realm">Realm display name.</param>
/// <param name="RealmSlug">Realm slug used by the Battle.net API.</param>
/// <param name="Region">Battle.net region.</param>
/// <param name="Class">Character class.</param>
/// <param name="Specialization">Active character specialization.</param>
/// <param name="Race">Character race.</param>
/// <param name="Level">Character level.</param>
/// <param name="ItemLevel">Equipped item level.</param>
public sealed record CharacterProfile(
    string Name,
    string Realm,
    string RealmSlug,
    string Region,
    CharacterClass Class,
    CharacterSpecialization Specialization,
    CharacterRace Race,
    int Level,
    int ItemLevel);

/// <summary>
/// World of Warcraft playable character classes.
/// Values correspond to Blizzard class IDs.
/// </summary>
public enum CharacterClass
{
    Unknown = 0,

    Warrior = 1,
    Paladin = 2,
    Hunter = 3,
    Rogue = 4,
    Priest = 5,
    DeathKnight = 6,
    Shaman = 7,
    Mage = 8,
    Warlock = 9,
    Monk = 10,
    Druid = 11,
    DemonHunter = 12,
    Evoker = 13
}

/// <summary>
/// World of Warcraft character specializations.
/// Values correspond to Blizzard specialization IDs.
/// </summary>
public enum CharacterSpecialization
{
    Unknown = 0,

    ArcaneMage = 62,
    FireMage = 63,
    FrostMage = 64,

    HolyPaladin = 65,
    ProtectionPaladin = 66,
    RetributionPaladin = 70,

    ArmsWarrior = 71,
    FuryWarrior = 72,
    ProtectionWarrior = 73,

    BalanceDruid = 102,
    FeralDruid = 103,
    GuardianDruid = 104,
    RestorationDruid = 105,

    BloodDeathKnight = 250,
    FrostDeathKnight = 251,
    UnholyDeathKnight = 252,

    BeastMasteryHunter = 253,
    MarksmanshipHunter = 254,
    SurvivalHunter = 255,

    DisciplinePriest = 256,
    HolyPriest = 257,
    ShadowPriest = 258,

    AssassinationRogue = 259,
    OutlawRogue = 260,
    SubtletyRogue = 261,

    ElementalShaman = 262,
    EnhancementShaman = 263,
    RestorationShaman = 264,

    AfflictionWarlock = 265,
    DemonologyWarlock = 266,
    DestructionWarlock = 267,

    BrewmasterMonk = 268,
    WindwalkerMonk = 269,
    MistweaverMonk = 270,

    HavocDemonHunter = 577,
    VengeanceDemonHunter = 581,

    DevastationEvoker = 1467,
    PreservationEvoker = 1468,
    AugmentationEvoker = 1473,

    DevourerDemonHunter = 1480
}

/// <summary>
/// World of Warcraft playable character races.
/// Values correspond to Blizzard race IDs.
/// </summary>
public enum CharacterRace
{
    Unknown = 0,

    Human = 1,
    Orc = 2,
    Dwarf = 3,
    NightElf = 4,
    Undead = 5,
    Tauren = 6,
    Gnome = 7,
    Troll = 8,
    Goblin = 9,
    BloodElf = 10,
    Draenei = 11,

    Worgen = 22,

    PandarenNeutral = 24,
    PandarenAlliance = 25,
    PandarenHorde = 26,

    Nightborne = 27,
    HighmountainTauren = 28,
    VoidElf = 29,
    LightforgedDraenei = 30,
    ZandalariTroll = 31,
    KulTiran = 32,
    DarkIronDwarf = 34,
    Vulpera = 35,
    MagharOrc = 36,
    Mechagnome = 37,

    DracthyrAlliance = 52,
    DracthyrHorde = 70,

    EarthenHorde = 84,
    EarthenAlliance = 85,

    Haranir = 86
}
