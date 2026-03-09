namespace JC.Core.Models.Auditing;

/// <summary>
/// Base class for auditable entities. Provides automatic population of creation, modification,
/// soft-delete, and restore audit fields. All setters are private — state is only changed through
/// the <c>Fill*</c> methods to ensure consistency.
/// </summary>
public class AuditModel
{
    /// <summary>Gets the identifier of the user who created this entity.</summary>
    public string? CreatedById { get; private set; }

    /// <summary>Gets the UTC date and time this entity was created.</summary>
    public DateTime CreatedUtc { get; private set; }

    /// <summary>Gets the identifier of the user who last modified this entity.</summary>
    public string? LastModifiedById { get; private set; }

    /// <summary>Gets the UTC date and time this entity was last modified.</summary>
    public DateTime? LastModifiedUtc { get; private set; }

    /// <summary>Gets the identifier of the user who soft-deleted this entity.</summary>
    public string? DeletedById { get; private set; }

    /// <summary>Gets the UTC date and time this entity was soft-deleted.</summary>
    public DateTime? DeletedUtc { get; private set; }

    /// <summary>Gets whether this entity is currently soft-deleted.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Gets the identifier of the user who restored this entity from soft-deletion.</summary>
    public string? RestoredById { get; private set; }

    /// <summary>Gets the UTC date and time this entity was restored from soft-deletion.</summary>
    public DateTime? RestoredUtc { get; private set; }

    /// <summary>
    /// Populates the creation audit fields with the current UTC time.
    /// </summary>
    /// <param name="userId">The identifier of the user creating the entity.</param>
    public void FillCreated(string userId)
    {
        if(string.IsNullOrWhiteSpace(CreatedById)) 
            CreatedById = userId;
        
        if(CreatedUtc == default)
            CreatedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Populates the modification audit fields with the current UTC time.
    /// </summary>
    /// <param name="userId">The identifier of the user modifying the entity.</param>
    public void FillModified(string userId)
    {
        LastModifiedById = userId;
        LastModifiedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the entity as soft-deleted and clears any previous restore fields.
    /// </summary>
    /// <param name="userId">The identifier of the user deleting the entity.</param>
    public void FillDeleted(string userId)
    {
        DeletedById = userId;
        DeletedUtc = DateTime.UtcNow;
        IsDeleted = true;

        RestoredById = null;
        RestoredUtc = null;
    }

    /// <summary>
    /// Restores the entity from soft-deletion and clears any previous delete fields.
    /// </summary>
    /// <param name="userId">The identifier of the user restoring the entity.</param>
    public void FillRestored(string userId)
    {
        RestoredById = userId;
        RestoredUtc = DateTime.UtcNow;
        IsDeleted = false;

        DeletedById = null;
        DeletedUtc = null;
    }
}