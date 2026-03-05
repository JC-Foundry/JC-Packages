namespace JC.Core.Enums;

/// <summary>
/// Specifies how soft-deleted records should be filtered in queries.
/// </summary>
public enum DeletedQueryType
{
    /// <summary>
    /// Include all records regardless of deletion status.
    /// </summary>
    All,

    /// <summary>
    /// Exclude soft-deleted records, returning only active records.
    /// </summary>
    OnlyActive,

    /// <summary>
    /// Return only soft-deleted records.
    /// </summary>
    OnlyDeleted
}