namespace JC.FileStorage.Models;

public class FolderModel
{
    public const string NullTenantName = "NO__TENANT";
    
    public string Name { get; }
    public string Tenant { get; }

    public FolderModel(string name)
    {
        if(name.Length > 256)
            throw new ArgumentException("Folder name cannot exceed 256 characters.", nameof(name));
        
        Name = name;
        Tenant = NullTenantName;
    }

    public FolderModel(string name, string tenantId) 
        : this(name)
    {
        if(tenantId.Length > 36)
            throw new ArgumentException("Tenant ID cannot exceed 36 characters.", nameof(tenantId));
        
        Tenant = tenantId;
    }
}