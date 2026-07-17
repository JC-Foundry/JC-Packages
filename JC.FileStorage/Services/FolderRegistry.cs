using System.Collections.Concurrent;
using JC.FileStorage.Models;

namespace JC.FileStorage.Services;

public class FolderRegistry
{
    private readonly Lock _lock = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<FolderModel>> _folders = new();

    private long? _defaultMaxBytes;
    private IReadOnlyList<string>? _defaultAllowedExtensions;

    /// <summary>
    /// Size limit applied to folders that set no <see cref="FolderModel.MaxBytes"/> of their own.
    /// <c>null</c> (the default) means no size limit for those folders.
    /// Cannot be set above <see cref="FolderModel.MaxAllowedBytes"/> (10GB).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative, or above the 10GB ceiling.</exception>
    public long? DefaultMaxBytes
    {
        get => _defaultMaxBytes;
        set => _defaultMaxBytes = FolderModel.ValidateMaxBytes(value, nameof(DefaultMaxBytes));
    }

    /// <summary>
    /// Extensions accepted by folders that set no <see cref="FolderModel.AllowedExtensions"/> of their
    /// own. <c>null</c> (the default) means those folders accept any extension that is not blocked.
    /// Entries are normalised to lower case with a leading dot.
    /// </summary>
    /// <exception cref="ArgumentException">The list is empty, or names a blocked extension.</exception>
    public IReadOnlyList<string>? DefaultAllowedExtensions
    {
        get => _defaultAllowedExtensions;
        set => _defaultAllowedExtensions =
            FolderModel.ValidateAllowedExtensions(value, nameof(DefaultAllowedExtensions));
    }

    /// <summary>
    /// The size limit in force for <paramref name="folder"/> — its own, else the registry default,
    /// else <c>null</c> for no limit.
    /// </summary>
    public long? ResolveMaxBytes(FolderModel folder)
        => folder.MaxBytes ?? _defaultMaxBytes;

    /// <summary>
    /// The allowed extensions in force for <paramref name="folder"/> — its own, else the registry
    /// default, else <c>null</c> for anything that is not blocked.
    /// </summary>
    public IReadOnlyList<string>? ResolveAllowedExtensions(FolderModel folder)
        => folder.AllowedExtensions ?? _defaultAllowedExtensions;

    /// <summary>
    /// Checks an extension and size against the blocked list, then the folder's limits (falling back
    /// to the registry defaults). The blocked list is checked first and always applies.
    /// </summary>
    /// <param name="folder">The folder the file would be stored in.</param>
    /// <param name="extension">The file's extension. The leading dot is optional.</param>
    /// <param name="sizeBytes">The file's size in bytes.</param>
    public FileValidationResponse ValidateFile(FolderModel folder, string extension, long sizeBytes)
    {
        var ext = FolderModel.NormaliseExtension(extension);

        //Always first - a blocked extension cannot be allowed back in by a folder or a default
        if(FolderModel.IsBlockedExtension(ext))
            return FileValidationResponse.Invalid(FileValidationError.BlockedExtension,
                $"Files of type '{ext}' cannot be stored.");

        var allowed = ResolveAllowedExtensions(folder);
        if(allowed != null && !allowed.Contains(ext))
            return FileValidationResponse.Invalid(FileValidationError.ExtensionNotAllowed,
                $"Files of type '{ext}' are not accepted here. Accepted types: {string.Join(", ", allowed)}.");

        var maxBytes = ResolveMaxBytes(folder);
        if(maxBytes != null && sizeBytes > maxBytes)
            return FileValidationResponse.Invalid(FileValidationError.TooLarge,
                $"The file is {sizeBytes} bytes, which is larger than the {maxBytes} byte limit.");

        return FileValidationResponse.Valid();
    }

    public bool TryAddFolder(FolderModel folder)
    {
        lock (_lock)
        {
            var result = _folders.TryGetValue(folder.Tenant, out var tenantFolders);
            if (!result || tenantFolders == null)
                return _folders.TryAdd(folder.Tenant, new List<FolderModel> { folder });
        
            if (tenantFolders.Any(f => string.Equals(f.Name, folder.Name, StringComparison.OrdinalIgnoreCase)))
                return false;
        
            tenantFolders = tenantFolders.Append(folder).ToList();
            _folders[folder.Tenant] = tenantFolders;
            return true;
        }
    }

    public bool TryGetFolder(string name, string? tenantId, out FolderModel? folder)
    {
        folder = null;
        var isNull = string.IsNullOrEmpty(tenantId);
        if(isNull)
            tenantId = FolderModel.NullTenantName;
        
        var result = _folders.TryGetValue(tenantId!, out var f);
        if(!result || f == null)
            return false;
        
        folder = f.FirstOrDefault(fd => string.Equals(fd.Name, name, StringComparison.OrdinalIgnoreCase));
        return folder != null;
    }

    public bool TryGetFolders(string? tenantId, out IReadOnlyList<FolderModel>? folders)
    {
        folders = null;
        if(string.IsNullOrEmpty(tenantId))
            tenantId = FolderModel.NullTenantName;
        
        return _folders.TryGetValue(tenantId, out folders);
    }

    public IReadOnlyList<string> GetFolderNames(string? tenantId = null)
    {
        var result = TryGetFolders(tenantId, out var folders);
        if (!result || folders == null)
            return [];
        
        return folders.Select(f => f.Name).ToList();
    }
}