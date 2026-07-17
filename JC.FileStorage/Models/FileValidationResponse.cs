namespace JC.FileStorage.Models;

/// <summary>
/// Outcome of checking a file's extension and size against a folder's limits, the registry
/// defaults, and the blocked-extension list. Produced by <c>FolderRegistry.ValidateFile</c>.
/// </summary>
public sealed record FileValidationResponse
{
    /// <summary>Whether the file may be stored.</summary>
    public bool Result { get; init; }

    /// <summary>Why the file was rejected, when <see cref="Result"/> is <c>false</c>. Null on success.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>What the file failed on, when <see cref="Result"/> is <c>false</c>.</summary>
    public FileValidationError Error { get; init; }

    private FileValidationResponse(bool result, FileValidationError error, string? errorMessage)
    {
        Result = result;
        Error = error;
        ErrorMessage = errorMessage;
    }

    /// <summary>The file may be stored.</summary>
    public static FileValidationResponse Valid()
        => new(true, FileValidationError.None, null);

    /// <summary>The file was rejected for the given reason.</summary>
    public static FileValidationResponse Invalid(FileValidationError error, string errorMessage)
        => new(false, error, errorMessage);
}

/// <summary>
/// Why a file failed validation. Lets a caller tell the reasons apart without parsing the message.
/// </summary>
public enum FileValidationError
{
    /// <summary>The file passed validation.</summary>
    None = 0,

    /// <summary>The extension is on the blocked list and can never be stored.</summary>
    BlockedExtension = 1,

    /// <summary>The extension is not in the folder's allowed list, or the registry default list.</summary>
    ExtensionNotAllowed = 2,

    /// <summary>The file is larger than the folder's limit, or the registry default limit.</summary>
    TooLarge = 3
}