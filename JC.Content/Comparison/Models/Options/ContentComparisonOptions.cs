using JC.Content.Comparison.Enums;

namespace JC.Content.Comparison.Models.Options;

/// <summary>
/// Comparison settings, fixed at registration. <see cref="Granularity"/> is the only one a call can
/// override.
/// </summary>
public class ContentComparisonOptions
{
    /// <summary>
    /// The unit applied when a call does not name one. Defaults to
    /// <see cref="ComparisonGranularity.Word"/>.
    /// </summary>
    public ComparisonGranularity Granularity { get; set; } = ComparisonGranularity.Word;

    /// <summary>
    /// The most characters to compare from either version, or zero for no limit. Content past the
    /// limit is neither examined nor returned, and
    /// <see cref="ContentComparisonResult.Truncated"/> reports it.
    /// </summary>
    /// <remarks>
    /// Worth setting where the content is user-supplied. The cost of a comparison rises with how
    /// much the two versions differ, so two long and largely unrelated documents are the expensive
    /// case — particularly at <see cref="ComparisonGranularity.Character"/>.
    /// </remarks>
    public int MaxContentLength { get; set; }

    internal void Validate()
    {
        if(MaxContentLength < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxContentLength), MaxContentLength,
                "Maximum content length cannot be negative. Use zero for no limit.");
    }
}
