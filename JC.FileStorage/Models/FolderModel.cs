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
    
    
    
    #region Obsolete

    /// <summary>
    /// Hard ceiling on any configured size limit (10GB). No folder or registry default may exceed this.
    /// </summary>
    [Obsolete("Use ValidationHelper.MaxAllowedBytes instead.", false)]
    public const long MaxAllowedBytes = ValidationHelper.MaxAllowedBytes;

    /// <summary>
    /// Extensions that can never be stored, whatever a folder or the registry defaults allow.
    /// Compared case-insensitively.
    /// </summary>
    [Obsolete("Use ValidationHelper.BlockedExtensions instead.", false)]
    public static IReadOnlyCollection<string> BlockedExtensions => ValidationHelper.Blocked;

    /// <summary>
    /// Whether <paramref name="extension"/> is blocked outright. The leading dot is optional.
    /// </summary>
    [Obsolete("Use ValidationHelper.IsBlockedExtension instead.", false)]
    public static bool IsBlockedExtension(string extension)
        => ValidationHelper.IsBlockedExtension(extension);   
    
    
    /// <summary>
    /// Normalises an allowed-extension list and rejects any blocked entry, so a blocked extension
    /// can never be allowed back in by configuration. <c>null</c> means "not set here".
    /// </summary>
    /// <exception cref="ArgumentException">The list is empty, or names a blocked extension.</exception>
    [Obsolete("Use ValidationHelper.ValidateAllowedExtensions instead.", false)]
    internal static IReadOnlyList<string>? ValidateAllowedExtensions(IEnumerable<string>? allowedExtensions, string paramName)
        => ValidationHelper.ValidateAllowedExtensions(allowedExtensions, paramName);
    
    
    /// <summary>
    /// Checks a size limit against the <see cref="ValidationHelper.MaxAllowedBytes"/> ceiling,
    /// returning it unchanged when valid. <c>null</c> means "no limit set here" and is always valid.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The limit is zero or negative, or above the ceiling.</exception>
    [Obsolete("Use ValidationHelper.ValidateMaxBytes instead.", false)]
    internal static long? ValidateMaxBytes(long? maxBytes, string paramName)
        => ValidationHelper.ValidateMaxBytes(maxBytes, paramName);

    
    /// <summary>
    /// Trims <paramref name="extension"/> and gives it a leading dot, so extensions compare
    /// consistently wherever they came from. Lower-cases it too unless <paramref name="lowerCase"/>
    /// is <c>false</c>, which callers building a physical path want — see
    /// <see cref="NormalisationHelper.NormaliseExtension"/>.
    /// </summary>
    [Obsolete("Use NormalisationHelper.NormaliseExtension instead.", false)]
    public static string NormaliseExtension(string extension, bool lowerCase = true)
        => NormalisationHelper.NormaliseExtension(extension, lowerCase);
    
    #endregion
}