namespace JC.FileStorage.Helpers;

public static class NormalisationHelper
{
    /// <summary>
    /// Trims <paramref name="extension"/> and gives it a leading dot, so extensions compare
    /// consistently wherever they came from. Lower-cases it too unless told not to.
    /// </summary>
    /// <param name="extension">The extension to normalise. The leading dot is optional.</param>
    /// <param name="lowerCase">
    /// Whether to lower-case the extension. Leave <c>true</c> for any value that will be compared —
    /// a blocked-extension check, an allowed-extension list, a lookup key. Pass <c>false</c> when the
    /// result becomes part of a physical path: the file on disk keeps whatever casing it was deployed
    /// with, and a case-sensitive filesystem will not match a lower-cased spelling of it.
    /// </param>
    /// <returns>
    /// The trimmed extension with a leading dot, lower-cased when <paramref name="lowerCase"/> is <c>true</c>.
    /// </returns>
    public static string NormaliseExtension(string extension, bool lowerCase = true)
    {
        var ext = extension.Trim();
        if(lowerCase)
            ext = ext.ToLowerInvariant();
        
        return !ext.StartsWith('.') ? $".{ext}" : ext;
    }

    /// <summary>
    /// Strips any directory and extension from <paramref name="fileName"/>, giving the value
    /// <see cref="JC.FileStorage.Models.SavedFile.SetFileName"/> stores in <c>SavedFile.FileName</c>.
    /// Anything querying on that column must key off this, or it will not match what was persisted.
    /// </summary>
    /// <param name="fileName">The full file name, including its extension.</param>
    /// <returns>A string containing the file name without its directory or extension.</returns>
    public static string NormaliseFileName(string fileName)
        => Path.GetFileNameWithoutExtension(fileName);
    
    /// <summary>
    /// Combines the given <paramref name="name"/> and <paramref name="extension"/> to construct a complete file name.
    /// </summary>
    /// <param name="name">The base name of the file without its extension.</param>
    /// <param name="extension">The extension of the file, which can optionally include a leading dot.</param>
    /// <returns>
    /// A string representing the complete file name, formed by combining <paramref name="name"/> and
    /// the normalised <paramref name="extension"/>. Casing is preserved on both parts, since the
    /// result names a file on disk rather than a value to be compared.
    /// </returns>
    public static string GetFileName(string name, string extension)
        => $"{name}{NormaliseExtension(extension, false)}";
}