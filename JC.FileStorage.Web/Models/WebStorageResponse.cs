using JC.FileStorage.Models;

namespace JC.FileStorage.Web.Models;

/// <summary>
/// Outcome of uploading an <c>IFormFile</c> through <c>WebStorageService</c>.
/// </summary>
public sealed record FileUploadResponse
{
    /// <summary>Whether the file was stored.</summary>
    public bool Result { get; init; }

    /// <summary>Why the upload failed, when <see cref="Result"/> is <c>false</c>. Null on success. Safe to surface to a user.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// What the file failed validation on, when it was rejected before being stored.
    /// <see cref="FileValidationError.None"/> when the upload succeeded, or when it failed for a
    /// reason other than validation.
    /// </summary>
    public FileValidationError ValidationError { get; init; }

    private FileUploadResponse(bool result, string? errorMessage, FileValidationError validationError)
    {
        Result = result;
        ErrorMessage = errorMessage;
        ValidationError = validationError;
    }

    /// <summary>The file was stored.</summary>
    public static FileUploadResponse Success()
        => new(true, null, FileValidationError.None);

    /// <summary>The upload failed for a reason other than validation.</summary>
    public static FileUploadResponse Failed(string errorMessage)
        => new(false, errorMessage, FileValidationError.None);

    /// <summary>The file was rejected by validation, before anything was read or written.</summary>
    public static FileUploadResponse Rejected(FileValidationResponse validation)
        => new(false, validation.ErrorMessage, validation.Error);
}

/// <summary>
/// Outcome of reading a stored file for a web response, carrying everything an
/// <c>IActionResult</c> needs.
/// </summary>
public sealed record FileDownloadResponse
{
    /// <summary>Whether the file was read.</summary>
    public bool Result { get; init; }

    /// <summary>Why the read failed, when <see cref="Result"/> is <c>false</c>. Null on success.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>The record, when <see cref="Result"/> is <c>true</c>. Null on failure.</summary>
    public SavedFile? File { get; init; }

    /// <summary>The file's bytes, when <see cref="Result"/> is <c>true</c>. Null on failure.</summary>
    public byte[]? Content { get; init; }

    /// <summary>The MIME type for the file's extension, when <see cref="Result"/> is <c>true</c>. Null on failure.</summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// The name to serve the file under, when <see cref="Result"/> is <c>true</c>. Null on failure.
    /// The name on disk is the record's ID, so it is never suitable to hand to a user.
    /// </summary>
    public string? DownloadName { get; init; }

    private FileDownloadResponse(bool result, string? errorMessage, SavedFile? file,
        byte[]? content, string? contentType, string? downloadName)
    {
        Result = result;
        ErrorMessage = errorMessage;
        File = file;
        Content = content;
        ContentType = contentType;
        DownloadName = downloadName;
    }

    /// <summary>The file was read.</summary>
    public static FileDownloadResponse Success(SavedFile file, byte[] content, string contentType, string downloadName)
        => new(true, null, file, content, contentType, downloadName);

    /// <summary>The file could not be read.</summary>
    public static FileDownloadResponse Failed(string errorMessage)
        => new(false, errorMessage, null, null, null, null);
}