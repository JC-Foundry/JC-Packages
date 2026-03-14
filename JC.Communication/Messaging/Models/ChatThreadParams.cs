using JC.Core.Enums;

namespace JC.Communication.Messaging.Models;

/// <summary>
/// Parameters for creating and querying chat threads. Extends <see cref="QueryParams"/>
/// with thread-specific properties such as name, description, date formatting, and colour preference.
/// </summary>
public class ChatThreadParams : QueryParams
{
    /// <summary>Gets or sets the display name for the thread. May be cleared to <c>null</c> to use a default name.</summary>
    public string? Name { get; internal set; }

    /// <summary>Gets the optional description for the thread.</summary>
    public string? Description { get; }

    /// <summary>Gets the format string used to display dates. Defaults to general short format ("g").</summary>
    public string DateFormat { get; } = "g";

    /// <summary>Gets whether colour values should prefer hex over RGB in the returned model.</summary>
    public bool PreferHexCode { get; } = true;

    /// <summary>
    /// Creates thread parameters with a name and query options.
    /// </summary>
    /// <param name="name">The display name for the thread.</param>
    /// <param name="asNoTracking">If <c>true</c>, entities are queried without change tracking.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all records are included.</param>
    public ChatThreadParams(string name, bool asNoTracking = false,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        : base(asNoTracking, deletedQueryType)
    {
        Name = name;
    }

    /// <summary>
    /// Creates thread parameters with full control over name, description, formatting, and query options.
    /// </summary>
    /// <param name="name">The display name for the thread.</param>
    /// <param name="description">An optional description for the thread.</param>
    /// <param name="dateFormat">The format string used to display dates. Defaults to "g" if <c>null</c>.</param>
    /// <param name="preferHexCode">If <c>true</c>, colour values prefer hex over RGB.</param>
    /// <param name="asNoTracking">If <c>true</c>, entities are queried without change tracking.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all records are included.</param>
    public ChatThreadParams(string name, string? description = null, string? dateFormat = null,
        bool preferHexCode = true, bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        : base(asNoTracking, deletedQueryType)
    {
        Name = name;
        Description = description;
        DateFormat = dateFormat ?? "g";
        PreferHexCode = preferHexCode;
    }
}

/// <summary>
/// Base query parameters controlling change tracking and soft-delete filtering behaviour.
/// </summary>
public class QueryParams
{
    /// <summary>Gets whether queries should use no-tracking mode for read-only access.</summary>
    public bool AsNoTracking { get; } = true;

    /// <summary>Gets the soft-delete filter mode for queries.</summary>
    public DeletedQueryType DeletedQueryType { get; } = DeletedQueryType.OnlyActive;

    /// <summary>
    /// Creates query parameters with default values (no-tracking enabled, active records only).
    /// </summary>
    public QueryParams()
    {
    }

    /// <summary>
    /// Creates query parameters with the specified tracking and deletion filter options.
    /// </summary>
    /// <param name="asNoTracking">If <c>true</c>, entities are queried without change tracking.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all records are included.</param>
    public QueryParams(bool asNoTracking,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        AsNoTracking = asNoTracking;
        DeletedQueryType = deletedQueryType;
    }
}