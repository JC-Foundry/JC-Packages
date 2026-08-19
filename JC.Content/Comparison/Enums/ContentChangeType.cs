namespace JC.Content.Comparison.Enums;

/// <summary>
/// What happened to a run of content between the two versions compared.
/// </summary>
public enum ContentChangeType
{
    /// <summary>Present in both versions, unaltered.</summary>
    Unchanged,

    /// <summary>Present only in the revised content.</summary>
    Added,

    /// <summary>Present only in the original content.</summary>
    Removed
}
