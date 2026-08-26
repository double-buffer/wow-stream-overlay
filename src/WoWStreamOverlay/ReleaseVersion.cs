namespace WowStreamOverlay;

public enum ReleaseStage
{
    Dev,
    Beta,
    Rc,
    Release
}

public readonly record struct ReleaseVersion(
    int Major,
    int Minor,
    int Patch,
    ReleaseStage Stage = ReleaseStage.Release,
    int StageNumber = 0) : IComparable<ReleaseVersion>
{
    public int CompareTo(ReleaseVersion other)
    {
        var comparison = Major.CompareTo(other.Major);

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = Minor.CompareTo(other.Minor);

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = Patch.CompareTo(other.Patch);

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = Stage.CompareTo(other.Stage);

        if (comparison != 0)
        {
            return comparison;
        }

        return StageNumber.CompareTo(other.StageNumber);
    }

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";

        if (Stage == ReleaseStage.Release)
        {
            return version;
        }

        var stage = Stage switch
        {
            ReleaseStage.Dev => "dev",
            ReleaseStage.Beta => "beta",
            ReleaseStage.Rc => "rc",
            _ => throw new InvalidOperationException($"Unsupported release stage: {Stage}")
        };

        return $"{version}-{stage}.{StageNumber}";
    }

    public static bool TryParse(string? value, out ReleaseVersion version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var separator = value.IndexOf('-');
        var baseValue = separator < 0 ? value : value[..separator];
        var baseParts = baseValue.Split('.');

        if (baseParts.Length != 3 ||
            !int.TryParse(baseParts[0], out var major) || major < 0 ||
            !int.TryParse(baseParts[1], out var minor) || minor < 0 ||
            !int.TryParse(baseParts[2], out var patch) || patch < 0)
        {
            return false;
        }

        if (separator < 0)
        {
            version = new ReleaseVersion(major, minor, patch);
            return true;
        }

        var suffix = value[(separator + 1)..];
        var suffixParts = suffix.Split('.');

        if (suffixParts.Length != 2 || !int.TryParse(suffixParts[1], out var stageNumber) || stageNumber < 1)
        {
            return false;
        }

        var stage = suffixParts[0].ToLowerInvariant() switch
        {
            "dev" => ReleaseStage.Dev,
            "beta" => ReleaseStage.Beta,
            "rc" => ReleaseStage.Rc,
            _ => (ReleaseStage?)null
        };

        if (stage is null)
        {
            return false;
        }

        version = new ReleaseVersion(major, minor, patch, stage.Value, stageNumber);
        return true;
    }

    public static bool operator <(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) >= 0;
}
