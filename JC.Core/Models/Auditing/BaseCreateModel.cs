namespace JC.Core.Models.Auditing;

public class BaseCreateModel
{
    /// <summary>Gets the identifier of the user who created this entity.</summary>
    public string? CreatedById { get; private set; }

    /// <summary>Gets the UTC date and time this entity was created.</summary>
    public DateTime CreatedUtc { get; private set; }
    
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
}