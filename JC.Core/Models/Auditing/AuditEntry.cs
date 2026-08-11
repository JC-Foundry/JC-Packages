using System.ComponentModel.DataAnnotations;
using JC.Core.Enums;

namespace JC.Core.Models.Auditing;

/// <summary>
/// Represents a single audit trail record capturing who performed what action and when.
/// </summary>
public class AuditEntry
{
    /// <summary>Gets the unique identifier for this audit entry.</summary>
    [Key]
    [MaxLength(36)]
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the type of action that was performed.</summary>
    public AuditAction Action { get; set; }

    /// <summary>Gets or sets the UTC date and time the action occurred.</summary>
    public DateTime AuditDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who performed the action, as seen by the saving
    /// <see cref="Data.IDataDbContext"/> (the ambient <see cref="IUserInfo"/>, or
    /// <see cref="IUserInfo.MissingUserInfoId"/> when the context has no identity).
    /// </summary>
    [MaxLength(256)]
    public string? UserId { get; set; }

    /// <summary>Gets or sets the display name of the user who performed the action.</summary>
    [MaxLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the identifier stamped onto the entity's own audit fields for this action
    /// (<c>CreatedById</c>, <c>LastModifiedById</c>, <c>DeletedById</c> or <c>RestoredById</c> as
    /// appropriate). This is the actor recorded by the repository/manager layer and can be more
    /// accurate than <see cref="UserId"/> when the context lacks identity or an explicit user id was
    /// supplied. <c>null</c> when the entity is not auditable or nothing was stamped (e.g. hard delete).
    /// </summary>
    [MaxLength(256)]
    public string? ActionUserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the application that wrote this audit entry, as configured via
    /// <c>CoreAuditOptions.ApplicationName</c>. <c>null</c> when the writing application did not configure one.
    /// </summary>
    [MaxLength(256)]
    public string? SourceApplication { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="ActionUserId"/> should be preferred over <see cref="UserId"/>
    /// as the true actor. <c>true</c> when <see cref="ActionUserId"/> holds a usable identifier that
    /// differs from <see cref="UserId"/>; otherwise <c>false</c>.
    /// </summary>
    public bool IsActionIdPreferred { get; set; }

    /// <summary>Gets or sets the name of the database table affected by the action.</summary>
    [MaxLength(256)]
    public string? TableName { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialised primary key of the audited entity, keyed by property name
    /// (e.g. <c>{"Id":"abc"}</c> or, for composite keys, <c>{"ThreadId":"abc","UserId":"xyz"}</c>).
    /// </summary>
    [MaxLength(512)]
    public string? EntityKey { get; set; }

    /// <summary>Gets or sets the JSON-serialised entity data associated with the action.</summary>
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? ActionData { get; set; }
}