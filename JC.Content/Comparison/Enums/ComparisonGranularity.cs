namespace JC.Content.Comparison.Enums;

/// <summary>
/// The unit a comparison works in. Decides how small a reported change can be, and how noisy the
/// result reads.
/// </summary>
public enum ComparisonGranularity
{
    /// <summary>
    /// Whole lines, each carrying its own terminator. The coarsest, and the right choice for
    /// anything structured — configuration, code, logs.
    /// </summary>
    Line,

    /// <summary>
    /// Words, each carrying the whitespace that follows it. A spacing change is reported against
    /// the word it follows rather than on its own.
    /// </summary>
    Word,

    /// <summary>
    /// Individual characters, counted as a reader sees them — a surrogate pair or a base letter
    /// with its combining marks stays whole. Precise, but noisy on prose.
    /// </summary>
    Character
}
