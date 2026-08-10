using System.Globalization;
using System.Text.RegularExpressions;
using JC.Core.Helpers;

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
    /// <param name="normaliseToDisplayName">
    /// Whether the value is normalised to a display name before converting to slug value.
    /// For example, 'MyText' will normalise to 'My Text', to create slug 'my-text' when true.
    /// Otherwise, 'MyText' will create slug 'mytext'.
    /// </param>
    /// <returns>A URL-friendly slug representation of the string.</returns>
    public static string ToSlug(this string value, bool normaliseToDisplayName = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Digits are deliberately left attached here even though the display name normaliser
        // splits them by default. Slugs are persisted in URLs, so splitting would silently
        // turn an existing 'version2' into 'version-2' and break every link to it.
        var name = normaliseToDisplayName
            ? InternalHelpers.ToDisplayName(value.Trim(), splitDigits: false)
            : value.Trim();
        
        var slug = name.ToLowerInvariant();
        slug = NonAlphanumericRegex().Replace(slug, "-");
        slug = ConsecutiveHyphensRegex().Replace(slug, "-");

        return slug.Trim('-');
    }

    /// <summary>
    /// Converts a string to a normalised URL-friendly slug by first normalising the string
    /// to a display name and then converting it to a slug format.
    /// </summary>
    /// <param name="value">The string to convert into a normalised slug.</param>
    /// <returns>A URL-friendly slug representation of the normalised string.</returns>
    public static string ToNormalisedSlug(this string value)
        => value.ToSlug(true);

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
    /// Converts an identifier-style string to a display-friendly name. Underscores, hyphens, full stops
    /// and whitespace separate words, as do casing transitions, and each word is capitalised.
    /// Acronyms keep their casing unless the input is entirely uppercase.
    /// </summary>
    /// <remarks>
    /// Intended for identifiers rather than prose, since every word is capitalised. Use
    /// <see cref="ToTitleCase"/> when the input is already readable text.
    /// </remarks>
    /// <param name="value">The input string to convert to a display name.</param>
    /// <param name="splitDigits">
    /// Whether a digit adjoining a letter starts a new word, so that 'Address1' becomes 'Address 1'.
    /// Adjacent digits are kept together either way. Set to <c>false</c> to leave digits attached
    /// to the word they follow, giving 'Address1'.
    /// </param>
    /// <returns>A display-friendly version of the input string, or an empty string if the input is null or whitespace.</returns>
    public static string ToDisplayName(this string value, bool splitDigits = true)
        => InternalHelpers.ToDisplayName(value, splitDigits);

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
