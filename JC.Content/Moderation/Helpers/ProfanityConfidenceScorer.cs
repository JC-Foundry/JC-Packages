using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Helpers;

/// <summary>
/// Turns the work a match needed into a confidence percentage.
/// </summary>
/// <remarks>
/// Only transformations that can fabricate a match from innocent text are priced, in proportion to
/// how much of the term they account for. Ones that can only decode a deliberate match are free, and
/// structurally unreliable matches are capped rather than deducted.
/// </remarks>
internal static class ProfanityConfidenceScorer
{
    //Cost of substituting every letter of a term. Masking scores below leetspeak: a '*' cannot occur
    //by accident, where '1' and '5' turn up in part numbers and identifiers
    private const int LeetWeight = 90;
    private const int MaskWeight = 92;

    //Its own weight, not leetspeak's: the source is an ordinary letter, so this fires on prose in a
    //way '1' and '@' never do
    private const int ConfusableWeight = 90;

    /// <param name="transformations">What the matcher did to the text to find this.</param>
    /// <param name="leetPositions">Letters of the term hidden behind a leetspeak character.</param>
    /// <param name="maskPositions">Letters of the term hidden behind a mask.</param>
    /// <param name="confusablePositions">Letters of the term written as a look-alike letter.</param>
    /// <param name="termLength">Letters the term had to match, excluding phrase spaces.</param>
    /// <param name="mediumConfidenceMinimum">The floor of the medium band, which the ceiling sits below.</param>
    public static int Score(ProfanityTransformation transformations,
        int leetPositions,
        int maskPositions,
        int confusablePositions,
        int termLength,
        ushort mediumConfidenceMinimum)
    {
        //Case, homoglyphs, accents, repeated letters and in-word punctuation are all free: each needs
        //the term's letters already present in order, so none can invent a match
        var score = 100
                    - Deduct(leetPositions, termLength, LeetWeight)
                    - Deduct(maskPositions, termLength, MaskWeight)
                    - Deduct(confusablePositions, termLength, ConfusableWeight);

        score = Math.Clamp(score, 1, 100);

        //Derived from the configured band, not a constant: an unreliable match has to stay in Low
        //wherever the caller has put its floor, or no level could still guarantee it never blocks
        return IsUnreliable(transformations)
            ? Math.Min(score, mediumConfidenceMinimum - 1)
            : score;
    }

    /// <summary>Whether the match spans text that may never have been one word.</summary>
    private static bool IsUnreliable(ProfanityTransformation transformations)
        => transformations.HasFlag(ProfanityTransformation.InsideWord)
           || transformations.HasFlag(ProfanityTransformation.WordBreakRemoved);

    /// <summary>
    /// Prices a substitution by the share of the term it hides, so a long word survives what a short
    /// one does not — three swapped letters is most of 'shit' but a quarter of 'motherfucker'.
    /// </summary>
    private static int Deduct(int positions, int termLength, int weight)
        => positions <= 0 || termLength <= 0
            ? 0
            : (int)Math.Round(weight * Math.Min(positions, termLength) / (double)termLength);
}
