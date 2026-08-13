using System.Text;

namespace JC.Core.Helpers;

public static class CoreHelpers
{
    /// <summary>The default package name for JC Packages</summary>
    public const string PackageName = "JCP";
    
    /// <summary>The default version prefix for JC Packages</summary>
    public const string PackageVersionPrefix = "v";
    
    /// <summary>The current version for JC Packages</summary>
    public const string PackageVersion = "6.1.0";

    /// <summary>
    /// Generates a display string for a package, combining an optional introductory text,
    /// a package display name, a version prefix, and the package version.
    /// </summary>
    /// <param name="introText">The text to display before the package information. Defaults to "Using".</param>
    /// <param name="displayNameOverride">An optional override for the package display name. If null or whitespace, the default package name is used.</param>
    /// <param name="versionPrefixOverride">An optional override for the version prefix. If null or whitespace, the default version prefix is used.</param>
    /// <returns>A formatted string representing the package display information.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="introText"/> is null or whitespace.</exception>
    public static string PackageDisplay(string introText = "Using",
        string? displayNameOverride = null,
        string? versionPrefixOverride = null)
    {
        if (string.IsNullOrWhiteSpace(introText))
            throw new ArgumentException("Introductory text cannot be null or whitespace.", nameof(introText));
        
        var name = string.IsNullOrWhiteSpace(displayNameOverride) ? PackageName : displayNameOverride;
        var prefix = string.IsNullOrWhiteSpace(versionPrefixOverride) ? PackageVersionPrefix : versionPrefixOverride;
        return $"{introText.Trim()} {name} {prefix}{PackageVersion}";
    }
    
    /// <summary>
    /// Normalises an identifier-style string into human-readable text. Runs of separators
    /// collapse into a single space, casing transitions become word boundaries, and every
    /// word is capitalised. Acronyms keep their casing unless the whole input is uppercase,
    /// and reference codes such as "BT.23.9" or "2024-01-15" are carried across verbatim.
    /// </summary>
    /// <param name="name">The identifier-style string to normalise.</param>
    /// <param name="splitDigits">Whether a digit adjoining a letter starts a new word.</param>
    internal static string ToDisplayName(string name, bool splitDigits = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // SCREAMING_CASE and snake_case carry no inner casing worth keeping, so everything
        // after a word's first letter is lowercased. Mixed-case input is left alone, which
        // is what allows an acronym such as "XMLParser" to survive as "XML Parser".
        var lowercaseRest = DetermineCase(name) is CaseStyle.Screaming or CaseStyle.Snake;

        var result = new StringBuilder(name.Length + 8);
        var position = 0;

        while (position < name.Length)
        {
            // Consuming the whole run at once collapses repeated separators into a single
            // boundary and discards any that lead or trail the input.
            while (position < name.Length && IsSeparator(name, position))
                position++;

            if (position == name.Length)
                break;

            var (end, isLiteral) = ScanToken(name, position, splitDigits);

            if (result.Length > 0)
                result.Append(' ');

            if (isLiteral)
            {
                // A token holding a retained separator is a reference code rather than a
                // word, so it is copied across untouched by the casing rules.
                result.Append(name, position, end - position);
            }
            else
            {
                result.Append(char.ToUpperInvariant(name[position]));

                for (var i = position + 1; i < end; i++)
                    result.Append(lowercaseRest ? char.ToLowerInvariant(name[i]) : name[i]);
            }

            position = end;
        }

        return result.ToString();
    }

    /// <summary>
    /// Finds where the token starting at <paramref name="start"/> ends, and reports whether it
    /// holds a retained separator and so has to be treated as a literal code.
    /// </summary>
    private static (int End, bool IsLiteral) ScanToken(string name, int start, bool splitDigits)
    {
        var isLiteral = false;
        var afterRetained = false;

        for (var i = start + 1; i < name.Length; i++)
        {
            if (IsRetainedSeparator(name, i))
            {
                isLiteral = true;
                afterRetained = true;
                continue;
            }

            if (IsSeparator(name, i))
                return (i, isLiteral);

            // The character straight after a retained separator can never open a word, or
            // "E-Commerce" would break at the capital C and "2024-01-15" at every group.
            if (afterRetained)
            {
                afterRetained = false;
                continue;
            }

            if (StartsNewWord(name, i, splitDigits))
                return (i, isLiteral);
        }

        return (name.Length, isLiteral);
    }

    /// <summary>
    /// Determines whether the character at <paramref name="index"/> opens a new word, either
    /// through a casing transition or, when enabled, a boundary between letters and digits.
    /// </summary>
    private static bool StartsNewWord(string name, int index, bool splitDigits)
    {
        var current = name[index];
        var previous = name[index - 1];

        // An uppercase letter opens a word when it follows a lowercase one ("myText"), or when
        // it precedes one — the second case splits a leading acronym from the word it qualifies,
        // turning "XMLParser" into "XML Parser".
        if (char.IsUpper(current)
            && (char.IsLower(previous) || (index + 1 < name.Length && char.IsLower(name[index + 1]))))
            return true;

        if (!splitDigits)
            return false;

        // A digit meeting a letter in either direction opens a word, so that "Address1" reads
        // as "Address 1". Adjacent digits stay together, keeping "Base64Encode" as "Base 64 Encode".
        return char.IsDigit(current)
            ? char.IsLetter(previous)
            : char.IsLetter(current) && char.IsDigit(previous);
    }

    /// <summary>
    /// Underscores and whitespace always separate words. Hyphens and full stops usually do too,
    /// unless they are retained, in which case they belong to the token around them.
    /// </summary>
    private static bool IsSeparator(string name, int index)
    {
        var current = name[index];

        return current is '_'
               || char.IsWhiteSpace(current)
               || (IsRetainable(current) && !IsRetainedSeparator(name, index));
    }

    /// <summary>
    /// A hyphen or full stop is retained when flanked on both sides by a digit or a capital,
    /// which is what holds references such as "26.4", "BT.23.9" and "2024-01-15" together.
    /// Other punctuation is never treated as a separator at all, so that the comma in a flags
    /// enum's "Read, Write" survives normalisation.
    /// </summary>
    private static bool IsRetainedSeparator(string name, int index)
        => IsRetainable(name[index])
           && index > 0
           && index + 1 < name.Length
           && IsCodeCharacter(name[index - 1])
           && IsCodeCharacter(name[index + 1]);

    private static bool IsRetainable(char value)
        => value is '-' or '.';

    private static bool IsCodeCharacter(char value)
        => char.IsDigit(value) || char.IsUpper(value);

    private enum CaseStyle
    {
        Pascal,
        Camel,
        Screaming,
        Snake
    }

    /// <summary>
    /// Classifies the input's casing style. Only ever called with a string holding at least one
    /// non-whitespace character, as <see cref="ToDisplayName"/> returns early otherwise.
    /// </summary>
    private static CaseStyle DetermineCase(string text)
    {
        var hasUpper = false;
        var hasLower = false;

        foreach (var current in text)
        {
            hasUpper |= char.IsUpper(current);
            hasLower |= char.IsLower(current);
        }

        if (!hasLower)
            return CaseStyle.Screaming;

        if (!hasUpper)
            return CaseStyle.Snake;

        return char.IsUpper(text[0])
            ? CaseStyle.Pascal
            : CaseStyle.Camel;
    }
}
