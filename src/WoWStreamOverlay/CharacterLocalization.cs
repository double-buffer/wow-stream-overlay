namespace WowStreamOverlay;

internal static class CharacterLocalization
{
    public static string ClassName(CharacterClass characterClass, string locale, string fallback)
    {
        if (!IsFrench(locale))
        {
            return fallback;
        }

        return characterClass switch
        {
            CharacterClass.Warrior => "Guerrier",
            CharacterClass.Paladin => "Paladin",
            CharacterClass.Hunter => "Chasseur",
            CharacterClass.Rogue => "Voleur",
            CharacterClass.Priest => "Prêtre",
            CharacterClass.DeathKnight => "Chevalier de la mort",
            CharacterClass.Shaman => "Chaman",
            CharacterClass.Mage => "Mage",
            CharacterClass.Warlock => "Démoniste",
            CharacterClass.Monk => "Moine",
            CharacterClass.Druid => "Druide",
            CharacterClass.DemonHunter => "Chasseur de démons",
            CharacterClass.Evoker => "Évocateur",
            _ => fallback
        };
    }

    public static string SpecializationName(CharacterSpecialization specialization, string locale, string fallback)
    {
        if (!IsFrench(locale))
        {
            return fallback;
        }

        return specialization switch
        {
            CharacterSpecialization.ArcaneMage => "Arcane",
            CharacterSpecialization.FireMage => "Feu",
            CharacterSpecialization.FrostMage => "Givre",

            CharacterSpecialization.HolyPaladin => "Sacré",
            CharacterSpecialization.ProtectionPaladin => "Protection",
            CharacterSpecialization.RetributionPaladin => "Vindicte",

            CharacterSpecialization.ArmsWarrior => "Armes",
            CharacterSpecialization.FuryWarrior => "Fureur",
            CharacterSpecialization.ProtectionWarrior => "Protection",

            CharacterSpecialization.BalanceDruid => "Équilibre",
            CharacterSpecialization.FeralDruid => "Farouche",
            CharacterSpecialization.GuardianDruid => "Gardien",
            CharacterSpecialization.RestorationDruid => "Restauration",

            CharacterSpecialization.BloodDeathKnight => "Sang",
            CharacterSpecialization.FrostDeathKnight => "Givre",
            CharacterSpecialization.UnholyDeathKnight => "Impie",

            CharacterSpecialization.BeastMasteryHunter => "Maîtrise des bêtes",
            CharacterSpecialization.MarksmanshipHunter => "Précision",
            CharacterSpecialization.SurvivalHunter => "Survie",

            CharacterSpecialization.DisciplinePriest => "Discipline",
            CharacterSpecialization.HolyPriest => "Sacré",
            CharacterSpecialization.ShadowPriest => "Ombre",

            CharacterSpecialization.AssassinationRogue => "Assassinat",
            CharacterSpecialization.OutlawRogue => "Hors-la-loi",
            CharacterSpecialization.SubtletyRogue => "Finesse",

            CharacterSpecialization.ElementalShaman => "Élémentaire",
            CharacterSpecialization.EnhancementShaman => "Amélioration",
            CharacterSpecialization.RestorationShaman => "Restauration",

            CharacterSpecialization.AfflictionWarlock => "Affliction",
            CharacterSpecialization.DemonologyWarlock => "Démonologie",
            CharacterSpecialization.DestructionWarlock => "Destruction",

            CharacterSpecialization.BrewmasterMonk => "Maître brasseur",
            CharacterSpecialization.WindwalkerMonk => "Marche-vent",
            CharacterSpecialization.MistweaverMonk => "Tisse-brume",

            CharacterSpecialization.HavocDemonHunter => "Dévastation",
            CharacterSpecialization.VengeanceDemonHunter => "Vengeance",
            CharacterSpecialization.DevourerDemonHunter => "Dévoreur",

            CharacterSpecialization.DevastationEvoker => "Dévastation",
            CharacterSpecialization.PreservationEvoker => "Préservation",
            CharacterSpecialization.AugmentationEvoker => "Augmentation",
            _ => fallback
        };
    }

    public static string RaceName(CharacterRace race, string locale, string fallback)
    {
        if (!IsFrench(locale))
        {
            return fallback;
        }

        return race switch
        {
            CharacterRace.Human => "Humain",
            CharacterRace.Orc => "Orc",
            CharacterRace.Dwarf => "Nain",
            CharacterRace.NightElf => "Elfe de la nuit",
            CharacterRace.Undead => "Mort-vivant",
            CharacterRace.Tauren => "Tauren",
            CharacterRace.Gnome => "Gnome",
            CharacterRace.Troll => "Troll",
            CharacterRace.Goblin => "Gobelin",
            CharacterRace.BloodElf => "Elfe de sang",
            CharacterRace.Draenei => "Draeneï",
            CharacterRace.Worgen => "Worgen",
            CharacterRace.PandarenNeutral or CharacterRace.PandarenAlliance or CharacterRace.PandarenHorde => "Pandaren",
            CharacterRace.Nightborne => "Sacrenuit",
            CharacterRace.HighmountainTauren => "Tauren de Haut-Roc",
            CharacterRace.VoidElf => "Elfe du Vide",
            CharacterRace.LightforgedDraenei => "Draeneï sancteforge",
            CharacterRace.ZandalariTroll => "Troll zandalari",
            CharacterRace.KulTiran => "Kultirassien",
            CharacterRace.DarkIronDwarf => "Nain sombrefer",
            CharacterRace.Vulpera => "Vulpérin",
            CharacterRace.MagharOrc => "Orc mag'har",
            CharacterRace.Mechagnome => "Mécagnome",
            CharacterRace.DracthyrAlliance or CharacterRace.DracthyrHorde => "Dracthyr",
            CharacterRace.EarthenAlliance or CharacterRace.EarthenHorde => "Terrestre",
            CharacterRace.Haranir => "Haranir",
            _ => fallback
        };
    }

    private static bool IsFrench(string locale)
    {
        return locale.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
    }
}
