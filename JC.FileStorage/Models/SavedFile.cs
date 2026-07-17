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
    
    [Required]
    [MaxLength(256)]
    public string FileName { get; private set; } = string.Empty;
    
    [Required]
    [MaxLength(64)]
    public string Extension { get; private set; } = string.Empty;
    
    [Required]
    [MaxLength(256)]
    public string FolderName { get; private set; } = string.Empty;


    /// <summary>
    /// Strips any directory and extension from <paramref name="fileName"/>, giving the value that
    /// <see cref="SetFileName"/> stores in <see cref="FileName"/>. Anything querying on
    /// <see cref="FileName"/> must key off this, or it will not match what was persisted.
    /// </summary>
    public static string NormaliseFileName(string fileName)
        => Path.GetFileNameWithoutExtension(fileName);

    public void SetFileName(string fileName, string? ext = null)
    {
        if(string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));

        var fn = NormaliseFileName(fileName);

        //An extension on the file name wins - ext is only a fallback for names that carry none
        var fileExt = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(fileExt))
        {
            if(string.IsNullOrWhiteSpace(ext))
                throw new ArgumentException("You must provide an extension when the file name does not contain one.", nameof(ext));

            fileExt = ext;
        }

        //A name that is all extension (".gitignore") leaves nothing to store
        if(string.IsNullOrWhiteSpace(fn))
            throw new ArgumentException("File name cannot be empty once the extension is removed.", nameof(fileName));

        fileExt = !fileExt.StartsWith('.') ? $".{fileExt}" : fileExt;

        if(fn.Length > 256)
            throw new ArgumentException("File name cannot exceed 256 characters.", nameof(fileName));

        if(fileExt.Length > 64)
            throw new ArgumentException("Extension cannot exceed 64 characters.", nameof(ext));

        FileName = fn;
        Extension = fileExt;
    }
    
    public void SetFolderName(FolderModel folder)
    {
        var isNull = string.IsNullOrEmpty(TenantId);
        if (isNull)
        {
            if(!string.Equals(folder.Tenant, FolderModel.NullTenantName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The folder must belong to the default tenant.");
        }
        else if(!string.Equals(folder.Tenant, TenantId))
            throw new ArgumentException("The folder must belong to the same tenant as the saved file.");
        
        FolderName = folder.Name;
    }
}