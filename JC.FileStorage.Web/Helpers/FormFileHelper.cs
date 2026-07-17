using JC.FileStorage.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace JC.FileStorage.Web.Helpers;

/// <summary>
/// Translates <see cref="IFormFile"/> uploads into the name, extension and bytes JC.FileStorage
/// works in, and maps extensions to web MIME types for serving files back.
/// </summary>
public static class FormFileHelper
{
    //Ships with the ASP.NET Core shared framework, ~380 mappings. Immutable after construction here,
    //so one shared instance is safe across requests
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    /// <summary>Returned by <see cref="GetContentType(string)"/> for extensions it does not know.</summary>
    public const string DefaultContentType = "application/octet-stream";

    /// <summary>
    /// The upload's file name with any directory component stripped. Browsers have historically sent
    /// full client paths, so the raw <see cref="IFormFile.FileName"/> is not safe to use as-is.
    /// </summary>
    public static string GetFileName(IFormFile file)
        => Path.GetFileName(file.FileName);

    /// <summary>
    /// The upload's extension, lower-cased with a leading dot. Empty when the name carries none.
    /// </summary>
    public static string GetExtension(IFormFile file)
    {
        var ext = Path.GetExtension(GetFileName(file));
        return string.IsNullOrWhiteSpace(ext) ? string.Empty : FolderModel.NormaliseExtension(ext);
    }

    /// <summary>
    /// Reads the whole upload into memory.
    /// </summary>
    /// <remarks>
    /// Buffers the entire file, so validate the size before calling — see
    /// <c>FolderRegistry.ValidateFile</c>, or use <c>WebStorageService</c>, which checks first.
    /// </remarks>
    public static async Task<byte[]> GetBytesAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    /// <summary>
    /// The MIME type for an extension (e.g. <c>.pdf</c> gives <c>application/pdf</c>), or
    /// <see cref="DefaultContentType"/> when it is not recognised. The leading dot is optional.
    /// </summary>
    public static string GetContentType(string extension)
    {
        if(string.IsNullOrWhiteSpace(extension))
            return DefaultContentType;

        var ext = FolderModel.NormaliseExtension(extension);
        return ContentTypeProvider.TryGetContentType($"file{ext}", out var contentType)
            ? contentType
            : DefaultContentType;
    }

    /// <summary>
    /// The MIME type for a stored file, from its <see cref="SavedFile.Extension"/>.
    /// </summary>
    public static string GetContentType(SavedFile file)
        => GetContentType(file.Extension);

    /// <summary>
    /// The file name to serve a stored file under — its <see cref="SavedFile.FileName"/> and
    /// <see cref="SavedFile.Extension"/> rejoined. The name on disk is the record's ID, so it is
    /// never suitable to hand to a user.
    /// </summary>
    public static string GetDownloadName(SavedFile file)
        => $"{file.FileName}{file.Extension}";

    /// <summary>
    /// A byte count as readable text — <c>1024</c> gives <c>1 KB</c>, <c>1572864</c> gives
    /// <c>1.5 MB</c>. For display only; JC.FileStorage's own messages report raw bytes.
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        if(bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), bytes, "A byte count cannot be negative.");

        string[] units = ["bytes", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        //Whole bytes never want a decimal; larger units keep up to two, trimmed
        return unit == 0 ? $"{bytes} {units[unit]}" : $"{size:0.##} {units[unit]}";
    }
}