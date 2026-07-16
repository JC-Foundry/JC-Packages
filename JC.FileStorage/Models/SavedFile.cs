using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;
using JC.Core.Models.MultiTenancy;

namespace JC.FileStorage.Models;

public class SavedFile : AuditModel, IMultiTenancy
{
    [Key]
    [MaxLength(36)]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    [MaxLength(36)]
    public string? TenantId { get; set; }
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
    
    [MaxLength(256)]
    public string FileName { get; set; } = string.Empty;
    
    [MaxLength(256)]
    public string FolderName { get; private set; } = string.Empty;

    public void SetFolderName(FolderModel folder)
    {
        if((string.Equals(folder.Tenant, FolderModel.NullTenantName, StringComparison.OrdinalIgnoreCase) 
           && !string.IsNullOrEmpty(TenantId)) || (!string.Equals(folder.Tenant, TenantId)))
           throw new ArgumentException("The folder must belong to the same tenant as the saved file.");
        
        FolderName = folder.Name;
    }
}