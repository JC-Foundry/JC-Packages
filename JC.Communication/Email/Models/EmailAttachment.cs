using MimeKit;

namespace JC.Communication.Email.Models;

/// <summary>
/// A single file attached to an outbound email. Content is held in memory for the lifetime of the
/// <see cref="EmailMessage"/>, so the attachment stays valid across validation, sending and logging.
/// </summary>
/// <param name="FileName">The file name shown to the recipient, including its extension.</param>
/// <param name="Content">The raw file content.</param>
/// <param name="ContentType">
/// Optional MIME type. When omitted it is inferred from <paramref name="FileName"/>, falling back to
/// <c>application/octet-stream</c> for unrecognised extensions.
/// </param>
public sealed record EmailAttachment(string FileName, byte[] Content, string? ContentType = null)
{
    /// <summary>
    /// The size of the attachment in bytes, before transfer encoding is applied.
    /// </summary>
    public long SizeBytes => Content.LongLength;

    /// <summary>
    /// The MIME type to send the attachment as: <see cref="ContentType"/> when supplied, otherwise
    /// the type inferred from the file name.
    /// </summary>
    public string ResolvedContentType => string.IsNullOrWhiteSpace(ContentType)
        ? MimeTypes.GetMimeType(FileName)
        : ContentType;

    /// <summary>
    /// Validates the attachment, returning the reason it is unusable or null when it is valid.
    /// </summary>
    /// <returns>A description of the problem, or null if the attachment is valid.</returns>
    internal string? Validate()
    {
        if (string.IsNullOrWhiteSpace(FileName))
            return "An attachment is missing a file name.";

        // A file name reaching the MIME headers with path separators in it would let a recipient's
        // client write outside the folder it expects to save into.
        if (FileName.Contains('/') || FileName.Contains('\\') || FileName.Contains(".."))
            return $"Attachment file name '{FileName}' must not contain path separators.";

        return Content.Length == 0
            ? $"Attachment '{FileName}' has no content."
            : null;
    }
}
