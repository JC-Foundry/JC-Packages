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
    /// <see cref="ProfanityConfidence.Medium"/> at any level, and a match found inside a longer word
    /// or across a word break is capped in <see cref="ProfanityConfidence.Low"/> — which is why
    /// neither can ever block.
    /// </summary>
    public static (ProfanitySeverity Severity, ProfanityConfidence Confidence) Floors(ProfanityLevel level)
        => level switch
        {
            ProfanityLevel.Minimal => (ProfanitySeverity.High, ProfanityConfidence.High),
            ProfanityLevel.Lax => (ProfanitySeverity.High, ProfanityConfidence.Medium),
            ProfanityLevel.Safe => (ProfanitySeverity.Medium, ProfanityConfidence.Medium),
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
    /// <param name="score">The confidence percentage.</param>
    /// <param name="mediumMinimum">The floor of the medium band, from <c>ProfanityModerationOptions</c>.</param>
    /// <param name="highMinimum">The floor of the high band, from <c>ProfanityModerationOptions</c>.</param>
    public static ProfanityConfidence ToConfidence(int score, ushort mediumMinimum, ushort highMinimum)
    {
        if(score <= 0) return ProfanityConfidence.None;
        if(score < mediumMinimum) return ProfanityConfidence.Low;
        if(score < highMinimum) return ProfanityConfidence.Medium;

        return score < 100 ? ProfanityConfidence.High : ProfanityConfidence.Certain;
    }
}
