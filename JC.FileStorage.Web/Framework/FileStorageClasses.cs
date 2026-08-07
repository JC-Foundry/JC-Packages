namespace JC.FileStorage.Web.Framework;

/*
 * The same rules as JC.Web's FrameworkClasses apply here — see that file for the reasoning. Every
 * property holds a whole class attribute value, values are complete rather than compositional, and
 * every property defaults to an empty string so adding one does not break an existing dictionary.
 */

/// <summary>
/// Classes for the upload constraints help text.
/// </summary>
public sealed record UploadConstraintsClasses
{
    /// <summary>The element wrapping the constraint text.</summary>
    public string Container { get; init; } = "";
}
