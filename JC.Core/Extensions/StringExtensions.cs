using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JC.Core.Extensions;

/// <summary>
/// Extension methods for common string operations including truncation, slugification, title casing, and masking.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Truncates a string to the specified maximum length, appending a suffix if truncation occurs.
    /// Returns the original string unchanged if it is shorter than or equal to the maximum length.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the string content before the suffix is appended.</param>
    /// <param name="suffix">The suffix to append when truncation occurs. Defaults to "...".</param>
    /// <returns>The original string if within the limit, or a truncated string ending with the suffix.</returns>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return string.Concat(value.AsSpan(0, maxLength), suffix);
    }

    /// <summary>
    /// Converts a string to a URL-friendly slug by lowercasing, replacing spaces and non-alphanumeric
    /// characters with hyphens, and collapsing consecutive hyphens.
    /// </summary>
    /// <param name="value">The string to convert into a slug.</param>
    /// <returns>A URL-friendly slug representation of the string.</returns>
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var slug = value.ToLowerInvariant();
        slug = NonAlphanumericRegex().Replace(slug, "-");
        slug = ConsecutiveHyphensRegex().Replace(slug, "-");

        return slug.Trim('-');
    }

    /// <summary>
    /// Converts a string to title case using the current culture's text rules.
    /// Each word's first letter is capitalised and the remaining letters are lowercased.
    /// </summary>
    /// <param name="value">The string to convert to title case.</param>
    /// <param name="culture">The culture whose casing rules are used. Defaults to <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <returns>The string converted to title case.</returns>
    public static string ToTitleCase(this string value, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var textInfo = (culture ?? CultureInfo.CurrentCulture).TextInfo;
        return textInfo.ToTitleCase(value.ToLower(culture ?? CultureInfo.CurrentCulture));
    }

    /// <summary>
    /// Masks a string by keeping only the first few characters visible and replacing the rest with asterisks.
    /// </summary>
    /// <param name="value">The string to mask.</param>
    /// <param name="visibleChars">The number of leading characters to keep visible.</param>
    /// <returns>The masked string with trailing characters replaced by asterisks.</returns>
    public static string Mask(this string value, int visibleChars)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (visibleChars < 0)
            visibleChars = 0;

        if (visibleChars >= value.Length)
            return value;

        return string.Concat(value.AsSpan(0, visibleChars), new string('*', value.Length - visibleChars));
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex ConsecutiveHyphensRegex();
}
