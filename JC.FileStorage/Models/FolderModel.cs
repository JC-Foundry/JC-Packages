using JC.FileStorage.Helpers;

namespace JC.FileStorage.Models;

public class FolderModel
{
    public const string NullTenantName = "NO__TENANT";

    public string Name { get; }
    public string Tenant { get; }
    public string? TenantId { get; }

    /// <summary>
    /// Maximum size of a file in this folder, or <c>null</c> to fall back to
    /// <c>FolderRegistry.DefaultMaxBytes</c>.
    /// </summary>
    public long? MaxBytes { get; }

    /// <summary>
    /// Extensions this folder accepts, or <c>null</c> to fall back to
    /// <c>FolderRegistry.DefaultAllowedExtensions</c>. Never overrides <see cref="ValidationHelper.BlockedExtensions"/>.
    /// </summary>
    public IReadOnlyList<string>? AllowedExtensions { get; }

    public FolderModel(string name)
    {
        if(name.Length > 256)
            throw new ArgumentException("Folder name cannot exceed 256 characters.", nameof(name));

        if(name.Contains('/') || name.Contains('.') || name.Contains('\\') || name.Contains('?'))
            throw new ArgumentException("Folder name cannot contain '/', '\\', '?' or '.' characters.", nameof(name));

        Name = name;
        Tenant = NullTenantName;
    }

    public FolderModel(string name, string? tenantId)
        : this(name)
    {
        var tenant = tenantId;
        if(string.IsNullOrWhiteSpace(tenantId))
            tenant = NullTenantName;

        if(tenant!.Length > 36)
            throw new ArgumentException("Tenant ID cannot exceed 36 characters.", nameof(tenantId));

        Tenant = tenant;
        TenantId = tenantId;
    }

    public FolderModel(string name, string? tenantId, long? maxBytes, IEnumerable<string>? allowedExtensions)
        : this(name, tenantId)
    {
        MaxBytes = ValidationHelper.ValidateMaxBytes(maxBytes, nameof(maxBytes));
        AllowedExtensions = ValidationHelper.ValidateAllowedExtensions(allowedExtensions, nameof(allowedExtensions));
    }
}
