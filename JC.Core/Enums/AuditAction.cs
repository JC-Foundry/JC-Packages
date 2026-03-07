namespace JC.Core.Enums;

/// <summary>
/// Represents the type of auditable action performed on an entity.
/// </summary>
public enum AuditAction
{
    /// <summary>A new entity was created.</summary>
    Create,

    /// <summary>An existing entity was updated.</summary>
    Update,

    /// <summary>An entity was deleted (soft or hard).</summary>
    Delete,

    /// <summary>A soft-deleted entity was restored.</summary>
    Restore
}