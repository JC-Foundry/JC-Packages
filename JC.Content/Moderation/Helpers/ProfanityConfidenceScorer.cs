using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Helpers;

/// <summary>
/// Turns the work a match needed into a confidence percentage. The principle: the harder the matcher
/// had to work, the less certain the finding — except where the work itself proves intent.
/// </summary>
internal static class ProfanityConfidenceScorer
{
    private const int DiacriticEach = 8;
    private const int DiacriticCap = 24;
    private const int Mask = 25;
    private const int RunExpansion = 35;
    private const int Separators = 35;
    private const int LeetFirst = 35;
    private const int LeetEach = 10;
    private const int LeetCap = 60;

    /// <summary>Stacking two kinds of evasion is far weaker evidence than either alone.</summary>
    private const int StackedTypes = 20;

    /// <summary>
    /// A match inside a longer word never rises above <see cref="ProfanityConfidence.Low"/>, and no
    /// level blocks on Low. Capping rather than deducting makes that a guarantee rather than an
    /// arithmetic coincidence — this is the Scunthorpe case, and it must never block.
    /// </summary>
    private const int InsideWordCeiling = 39;

    public static int Score(ProfanityTransformation transformations,
        int leetCount,
        int diacriticCount,
        bool insideWord)
    {
        var score = 100;
        var evasions = 0;

        //Case and homoglyphs cost nothing: both are lossless, where a mask stands for any letter and
        //'1' could be 'i' or 'l'. The deductions below price that ambiguity
        if(transformations.HasFlag(ProfanityTransformation.DiacriticsRemoved))
        {
            score -= Math.Min(diacriticCount * DiacriticEach, DiacriticCap);
            evasions++;
        }

        //Scores above leetspeak on purpose: typing 'f*ck' proves the writer knew what it was
        if(transformations.HasFlag(ProfanityTransformation.MaskWildcard))
        {
            score -= Mask;
            evasions++;
        }

        if(transformations.HasFlag(ProfanityTransformation.RunExpanded))
        {
            score -= RunExpansion;
            evasions++;
        }

        if(transformations.HasFlag(ProfanityTransformation.SeparatorsRemoved))
        {
            score -= Separators;
            evasions++;
        }

        if(transformations.HasFlag(ProfanityTransformation.Leetspeak))
        {
            score -= Math.Min(LeetFirst + Math.Max(leetCount - 1, 0) * LeetEach, LeetCap);
            evasions++;
        }

        if(evasions >= 2)
            score -= StackedTypes;

        score = Math.Clamp(score, 1, 100);

        return insideWord ? Math.Min(score, InsideWordCeiling) : score;
    }
}
