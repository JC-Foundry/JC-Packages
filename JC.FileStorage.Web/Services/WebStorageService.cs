using JC.FileStorage.Models;
using JC.FileStorage.Services;
using JC.FileStorage.Web.Helpers;
using JC.FileStorage.Web.Models;
using Microsoft.AspNetCore.Http;

namespace JC.FileStorage.Web.Services;

/// <summary>
/// Wraps <see cref="StorageService"/> for web applications: takes <see cref="IFormFile"/> uploads,
/// rejects them against the folder's limits before reading the stream, and returns stored files with
/// the MIME type and download name an action result needs.
/// </summary>
/// <remarks>
/// Validation here is a fail-fast convenience, not the gate. <see cref="StorageService"/> enforces
/// the same rules itself, so a file rejected here could not have been stored anyway.
/// </remarks>
public class WebStorageService
{
    private readonly StorageService _storageService;
    private readonly FolderRegistry _folderRegistry;

    public WebStorageService(StorageService storageService,
        FolderRegistry folderRegistry)
    {
        _storageService = storageService;
        _folderRegistry = folderRegistry;
    }

    /// <summary>
    /// Binds the underlying <see cref="StorageService"/> to a different DbContext. Affects every
    /// later call on this instance — see the JC.FileStorage guide.
    /// </summary>
    public void ChangeContext(Type contextType)
        => _storageService.ChangeContext(contextType);


    #region Validate

    /// <summary>
    /// Checks an upload against the folder's limits without storing it. Useful for populating
    /// <c>ModelState</c> before committing to the upload.
    /// </summary>
    public FileValidationResponse ValidateFile(FolderModel folder, IFormFile? file)
    {
        if(file == null || file.Length == 0)
            return FileValidationResponse.Invalid(FileValidationError.None, "No file was uploaded.");

        var ext = FormFileHelper.GetExtension(file);
        if(string.IsNullOrEmpty(ext))
            return FileValidationResponse.Invalid(FileValidationError.ExtensionNotAllowed,
                "The file has no extension.");

        return _folderRegistry.ValidateFile(folder, ext, file.Length);
    }

    #endregion


    #region Upload File

    /// <summary>
    /// Stores an upload in the current user's tenant.
    /// </summary>
    public async Task<FileUploadResponse> TryUploadFile(FolderModel folder, IFormFile? file,
        bool blockOverwrite = true, CancellationToken cancellationToken = default)
        => await Upload(folder, file, blockOverwrite, cancellationToken,
            (name, bytes, ext) => _storageService.TrySaveFile(folder, name, bytes, ext, blockOverwrite));

    /// <summary>
    /// Stores an upload in the given tenant. Bypasses the tenant query filter when the tenant differs
    /// from the caller's own — the consuming application must authorise this itself.
    /// </summary>
    public async Task<FileUploadResponse> TryUploadFileForTenant(string? tenantId, FolderModel folder,
        IFormFile? file, bool blockOverwrite = true, CancellationToken cancellationToken = default)
        => await Upload(folder, file, blockOverwrite, cancellationToken,
            (name, bytes, ext) => _storageService.TrySaveFileForTenant(tenantId, folder, name, bytes, ext, blockOverwrite));

    private async Task<FileUploadResponse> Upload(FolderModel folder, IFormFile? file, bool blockOverwrite,
        CancellationToken cancellationToken, Func<string, byte[], string, Task<bool>> save)
    {
        //Checked against the declared length before the stream is touched, so an oversized upload is
        //rejected without being buffered into memory
        var validation = ValidateFile(folder, file);
        if(!validation.Result)
            return FileUploadResponse.Rejected(validation);

        var bytes = await FormFileHelper.GetBytesAsync(file!, cancellationToken);
        var saved = await save(FormFileHelper.GetFileName(file!), bytes, FormFileHelper.GetExtension(file!));

        return saved
            ? FileUploadResponse.Success()
            : FileUploadResponse.Failed(blockOverwrite
                ? "A file of that name already exists here, or it could not be saved."
                : "The file could not be saved.");
    }

    #endregion


    #region Download File

    /// <summary>
    /// Reads a stored file from the current user's tenant, with its MIME type and download name.
    /// </summary>
    public async Task<FileDownloadResponse> GetFileForDownload(FolderModel folder, string fileName)
        => Download(await _storageService.GetSavedFileBytes(folder, fileName));

    /// <summary>
    /// Reads a stored file from the given tenant. Bypasses the tenant query filter when the tenant
    /// differs from the caller's own — the consuming application must authorise this itself.
    /// </summary>
    public async Task<FileDownloadResponse> GetFileForDownloadForTenant(string? tenantId, FolderModel folder, string fileName)
        => Download(await _storageService.GetSavedFileBytesForTenant(tenantId, folder, fileName));

    private static FileDownloadResponse Download(GetFileByteResponse response)
    {
        if(!response.Result || response.File == null || response.FileContent == null)
            return FileDownloadResponse.Failed(response.ErrorMessage ?? "File not found.");

        return FileDownloadResponse.Success(response.File, response.FileContent,
            FormFileHelper.GetContentType(response.File),
            FormFileHelper.GetDownloadName(response.File));
    }

    #endregion


    #region Delete File

    /// <summary>
    /// Deletes a file from the current user's tenant. The record is soft-deleted; the file is removed
    /// from disk permanently.
    /// </summary>
    public async Task<bool> TryDeleteFile(FolderModel folder, string fileName)
        => await _storageService.TryDeleteFile(folder, fileName);

    /// <summary>
    /// Deletes a file from the given tenant. Bypasses the tenant query filter when the tenant differs
    /// from the caller's own — the consuming application must authorise this itself.
    /// </summary>
    public async Task<bool> TryDeleteFileForTenant(string? tenantId, FolderModel folder, string fileName)
        => await _storageService.TryDeleteFileForTenant(tenantId, folder, fileName);

    #endregion
}