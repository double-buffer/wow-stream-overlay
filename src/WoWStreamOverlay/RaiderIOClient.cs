using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WowStreamOverlay;

public sealed partial class RaiderIOClient : ICharacterProfileProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _region;
    private readonly string _locale;

    public CharacterRefreshSource RefreshSource => CharacterRefreshSource.RaiderIO;

    public RaiderIOClient(HttpClient httpClient, string region = "eu", string locale = "fr_FR")
    {
        _httpClient = httpClient;
        _region = region.ToLowerInvariant();
        _locale = locale;
    }

    public async Task<CharacterProfile?> GetCharacterProfileAsync(
        string realmSlug,
        string characterName,
        CancellationToken cancellationToken = default)
    {
        var region = Uri.EscapeDataString(_region);
        var realm = Uri.EscapeDataString(realmSlug.ToLowerInvariant());
        var character = Uri.EscapeDataString(characterName);
        var fields = Uri.EscapeDataString("gear,mythic_plus_scores_by_season:current");
        var requestUri =
            $"https://raider.io/api/v1/characters/profile?region={region}&realm={realm}&name={character}&fields={fields}";

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var profile = await response.Content.ReadFromJsonAsync(RaiderIOJsonContext.Default.RaiderIOCharacterProfile, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException("Raider.IO returned an empty character profile.");
        }

        var characterClass = ParseClass(profile.Class);
        var specialization = ParseSpecialization(characterClass, profile.ActiveSpecializationName);
        var race = ParseRace(profile.Race, profile.Faction);
        var itemLevel = profile.Gear is null ? 0 : (int)Math.Floor(profile.Gear.ItemLevelEquipped);
        var score = profile.MythicPlusScoresBySeason.FirstOrDefault()?.Scores.All;
        int? mythicPlusScore = score is > 0
            ? (int)Math.Round(score.Value, MidpointRounding.AwayFromZero)
            : null;

        return new CharacterProfile(
            profile.Name,
            profile.Realm,
            realmSlug,
            string.IsNullOrWhiteSpace(profile.Region) ? _region : profile.Region,
            characterClass,
            specialization,
            race,
            0,
            itemLevel,
            CharacterLocalization.ClassName(characterClass, _locale, profile.Class),
            CharacterLocalization.SpecializationName(specialization, _locale, profile.ActiveSpecializationName),
            CharacterLocalization.RaceName(race, _locale, profile.Race),
            mythicPlusScore);
    }

    private static CharacterClass ParseClass(string value)
    {
        return value switch
        {
            "Warrior" => CharacterClass.Warrior,
            "Paladin" => CharacterClass.Paladin,
            "Hunter" => CharacterClass.Hunter,
            "Rogue" => CharacterClass.Rogue,
            "Priest" => CharacterClass.Priest,
            "Death Knight" => CharacterClass.DeathKnight,
            "Shaman" => CharacterClass.Shaman,
            "Mage" => CharacterClass.Mage,
            "Warlock" => CharacterClass.Warlock,
            "Monk" => CharacterClass.Monk,
            "Druid" => CharacterClass.Druid,
            "Demon Hunter" => CharacterClass.DemonHunter,
            "Evoker" => CharacterClass.Evoker,
            _ => CharacterClass.Unknown
        };
    }

    private static CharacterSpecialization ParseSpecialization(CharacterClass characterClass, string value)
    {
        return (characterClass, value) switch
        {
            (CharacterClass.Mage, "Arcane") => CharacterSpecialization.ArcaneMage,
            (CharacterClass.Mage, "Fire") => CharacterSpecialization.FireMage,
            (CharacterClass.Mage, "Frost") => CharacterSpecialization.FrostMage,

            (CharacterClass.Paladin, "Holy") => CharacterSpecialization.HolyPaladin,
            (CharacterClass.Paladin, "Protection") => CharacterSpecialization.ProtectionPaladin,
            (CharacterClass.Paladin, "Retribution") => CharacterSpecialization.RetributionPaladin,

            (CharacterClass.Warrior, "Arms") => CharacterSpecialization.ArmsWarrior,
            (CharacterClass.Warrior, "Fury") => CharacterSpecialization.FuryWarrior,
            (CharacterClass.Warrior, "Protection") => CharacterSpecialization.ProtectionWarrior,

            (CharacterClass.Druid, "Balance") => CharacterSpecialization.BalanceDruid,
            (CharacterClass.Druid, "Feral") => CharacterSpecialization.FeralDruid,
            (CharacterClass.Druid, "Guardian") => CharacterSpecialization.GuardianDruid,
            (CharacterClass.Druid, "Restoration") => CharacterSpecialization.RestorationDruid,

            (CharacterClass.DeathKnight, "Blood") => CharacterSpecialization.BloodDeathKnight,
            (CharacterClass.DeathKnight, "Frost") => CharacterSpecialization.FrostDeathKnight,
            (CharacterClass.DeathKnight, "Unholy") => CharacterSpecialization.UnholyDeathKnight,

            (CharacterClass.Hunter, "Beast Mastery") => CharacterSpecialization.BeastMasteryHunter,
            (CharacterClass.Hunter, "Marksmanship") => CharacterSpecialization.MarksmanshipHunter,
            (CharacterClass.Hunter, "Survival") => CharacterSpecialization.SurvivalHunter,

            (CharacterClass.Priest, "Discipline") => CharacterSpecialization.DisciplinePriest,
            (CharacterClass.Priest, "Holy") => CharacterSpecialization.HolyPriest,
            (CharacterClass.Priest, "Shadow") => CharacterSpecialization.ShadowPriest,

            (CharacterClass.Rogue, "Assassination") => CharacterSpecialization.AssassinationRogue,
            (CharacterClass.Rogue, "Outlaw") => CharacterSpecialization.OutlawRogue,
            (CharacterClass.Rogue, "Subtlety") => CharacterSpecialization.SubtletyRogue,

            (CharacterClass.Shaman, "Elemental") => CharacterSpecialization.ElementalShaman,
            (CharacterClass.Shaman, "Enhancement") => CharacterSpecialization.EnhancementShaman,
            (CharacterClass.Shaman, "Restoration") => CharacterSpecialization.RestorationShaman,

            (CharacterClass.Warlock, "Affliction") => CharacterSpecialization.AfflictionWarlock,
            (CharacterClass.Warlock, "Demonology") => CharacterSpecialization.DemonologyWarlock,
            (CharacterClass.Warlock, "Destruction") => CharacterSpecialization.DestructionWarlock,

            (CharacterClass.Monk, "Brewmaster") => CharacterSpecialization.BrewmasterMonk,
            (CharacterClass.Monk, "Windwalker") => CharacterSpecialization.WindwalkerMonk,
            (CharacterClass.Monk, "Mistweaver") => CharacterSpecialization.MistweaverMonk,

            (CharacterClass.DemonHunter, "Havoc") => CharacterSpecialization.HavocDemonHunter,
            (CharacterClass.DemonHunter, "Vengeance") => CharacterSpecialization.VengeanceDemonHunter,
            (CharacterClass.DemonHunter, "Devourer") => CharacterSpecialization.DevourerDemonHunter,

            (CharacterClass.Evoker, "Devastation") => CharacterSpecialization.DevastationEvoker,
            (CharacterClass.Evoker, "Preservation") => CharacterSpecialization.PreservationEvoker,
            (CharacterClass.Evoker, "Augmentation") => CharacterSpecialization.AugmentationEvoker,
            _ => CharacterSpecialization.Unknown
        };
    }

    private static CharacterRace ParseRace(string value, string faction)
    {
        return value switch
        {
            "Human" => CharacterRace.Human,
            "Orc" => CharacterRace.Orc,
            "Dwarf" => CharacterRace.Dwarf,
            "Night Elf" => CharacterRace.NightElf,
            "Undead" => CharacterRace.Undead,
            "Tauren" => CharacterRace.Tauren,
            "Gnome" => CharacterRace.Gnome,
            "Troll" => CharacterRace.Troll,
            "Goblin" => CharacterRace.Goblin,
            "Blood Elf" => CharacterRace.BloodElf,
            "Draenei" => CharacterRace.Draenei,
            "Worgen" => CharacterRace.Worgen,
            "Pandaren" when IsAlliance(faction) => CharacterRace.PandarenAlliance,
            "Pandaren" when IsHorde(faction) => CharacterRace.PandarenHorde,
            "Pandaren" => CharacterRace.PandarenNeutral,
            "Nightborne" => CharacterRace.Nightborne,
            "Highmountain Tauren" => CharacterRace.HighmountainTauren,
            "Void Elf" => CharacterRace.VoidElf,
            "Lightforged Draenei" => CharacterRace.LightforgedDraenei,
            "Zandalari Troll" => CharacterRace.ZandalariTroll,
            "Kul Tiran" => CharacterRace.KulTiran,
            "Dark Iron Dwarf" => CharacterRace.DarkIronDwarf,
            "Vulpera" => CharacterRace.Vulpera,
            "Mag'har Orc" => CharacterRace.MagharOrc,
            "Mechagnome" => CharacterRace.Mechagnome,
            "Dracthyr" when IsAlliance(faction) => CharacterRace.DracthyrAlliance,
            "Dracthyr" => CharacterRace.DracthyrHorde,
            "Earthen" when IsAlliance(faction) => CharacterRace.EarthenAlliance,
            "Earthen" => CharacterRace.EarthenHorde,
            "Haranir" => CharacterRace.Haranir,
            _ => CharacterRace.Unknown
        };
    }

    private static bool IsAlliance(string faction)
    {
        return faction.Equals("alliance", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHorde(string faction)
    {
        return faction.Equals("horde", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RaiderIOCharacterProfile
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("race")]
        public string Race { get; init; } = string.Empty;

        [JsonPropertyName("class")]
        public string Class { get; init; } = string.Empty;

        [JsonPropertyName("active_spec_name")]
        public string ActiveSpecializationName { get; init; } = string.Empty;

        [JsonPropertyName("faction")]
        public string Faction { get; init; } = string.Empty;

        [JsonPropertyName("region")]
        public string Region { get; init; } = string.Empty;

        [JsonPropertyName("realm")]
        public string Realm { get; init; } = string.Empty;

        [JsonPropertyName("mythic_plus_scores_by_season")]
        public RaiderIOSeasonScore[] MythicPlusScoresBySeason { get; init; } = [];

        [JsonPropertyName("gear")]
        public RaiderIOGear? Gear { get; init; }
    }

    private sealed class RaiderIOSeasonScore
    {
        [JsonPropertyName("scores")]
        public RaiderIOScores Scores { get; init; } = new();
    }

    private sealed class RaiderIOScores
    {
        [JsonPropertyName("all")]
        public double All { get; init; }
    }

    private sealed class RaiderIOGear
    {
        [JsonPropertyName("item_level_equipped")]
        public double ItemLevelEquipped { get; init; }
    }

    [JsonSerializable(typeof(RaiderIOCharacterProfile))]
    private partial class RaiderIOJsonContext : JsonSerializerContext
    {
    }
}
