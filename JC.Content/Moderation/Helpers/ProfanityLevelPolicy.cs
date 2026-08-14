using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Helpers;

/// <summary>
/// The thresholds behind each <see cref="ProfanityLevel"/>. Public because an application is free to
/// ignore our block decision and apply its own — this is the same arithmetic, so it can do that
/// without restating the rules and drifting from them.
/// </summary>
public static class ProfanityLevelPolicy
{
    /// <summary>
    /// The floors a match must reach to count at <paramref name="level"/>. Nothing blocks below
    /// <see cref="ProfanityConfidence.Medium"/> at any level, and a term found inside a longer word is
    /// capped in <see cref="ProfanityConfidence.Low"/> — which is the Scunthorpe case, and is why it
    /// can never block.
    /// </summary>
    public static (ProfanitySeverity Severity, ProfanityConfidence Confidence) Floors(ProfanityLevel level)
        => level switch
        {
            ProfanityLevel.Lax => (ProfanitySeverity.High, ProfanityConfidence.High),
            ProfanityLevel.Safe => (ProfanitySeverity.Medium, ProfanityConfidence.High),
            ProfanityLevel.Strict => (ProfanitySeverity.Low, ProfanityConfidence.Medium),
            _ => (ProfanitySeverity.Mild, ProfanityConfidence.Medium)
        };

    /// <summary>Whether a finding of this severity and confidence counts at <paramref name="level"/>.</summary>
    public static bool Counts(ProfanityLevel level, ProfanitySeverity severity, ProfanityConfidence confidence)
    {
        var (minSeverity, minConfidence) = Floors(level);
        return severity >= minSeverity && confidence >= minConfidence;
    }

    /// <summary>
    /// The band a percentage falls in. Half-open, so each score belongs to exactly one band —
    /// 0 alone is None, and only 100 is Certain.
    /// </summary>
    public static ProfanityConfidence ToConfidence(int score)
        => score switch
        {
            <= 0 => ProfanityConfidence.None,
            < 40 => ProfanityConfidence.Low,
            < 70 => ProfanityConfidence.Medium,
            < 100 => ProfanityConfidence.High,
            _ => ProfanityConfidence.Certain
        };
}
