using JC.Core.Enums;

namespace JC.Core.Models.Auditing;

/// <summary>
/// Represents a single audit trail record capturing who performed what action and when.
/// </summary>
public class AuditEntry
{
    /// <summary>Gets the unique identifier for this audit entry.</summary>
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the type of action that was performed.</summary>
    public AuditAction Action { get; set; }

    /// <summary>Gets or sets the UTC date and time the action occurred.</summary>
    public DateTime AuditDate { get; set; }

    /// <summary>Gets or sets the identifier of the user who performed the action.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the display name of the user who performed the action.</summary>
    public string? UserName { get; set; }

    /// <summary>Gets or sets the name of the database table affected by the action.</summary>
    public string? TableName { get; set; }

    /// <summary>Gets or sets the JSON-serialised entity data associated with the action.</summary>
    public string? ActionData { get; set; }
}