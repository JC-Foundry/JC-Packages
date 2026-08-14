using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JC.Content.Helpers;

/// <summary>
/// Cleans up content without changing what it says. Every method returns null only for null input.
/// </summary>
/// <remarks>
/// Not to be confused with the canonicaliser behind profanity matching, which is lossy and only ever
/// matched against. These results are meant to be kept.
/// </remarks>
public static partial class NormalisationHelper
{
    /// <summary>
    /// Invisible characters with no textual role. Excludes U+200C and U+200D, which are required in
    /// Arabic, Persian and Indic scripts and join emoji sequences, and the U+200E/U+200F direction
    /// marks used legitimately in mixed-direction text.
    /// </summary>
    private static readonly SearchValues<char> Invisible = SearchValues.Create(
    [
        '­', '᠎', '​', '⁠', '﻿',
        '‪', '‫', '‬', '‭', '‮',
        '⁦', '⁧', '⁨', '⁩'
    ]);

    /// <summary>Joiners, removed only on request — dropping these corrupts emoji and Indic text.</summary>
    private static readonly SearchValues<char> Joiners = SearchValues.Create(['‌', '‍']);

    private static readonly SearchValues<char> Quotes = SearchValues.Create(['‘', '’', '‚', '‛', '“', '”', '„', '‟']);
    private static readonly SearchValues<char> Dashes = SearchValues.Create(['‒', '–', '—', '―', '−']);

    /// <summary>
    /// The safe pass: invalid and invisible characters out, Unicode composed, line endings and
    /// trailing whitespace consistent. Nothing here alters wording, spacing within a line, or
    /// paragraph structure — those are the opt-in methods below.
    /// </summary>
    /// <param name="content">The content to normalise.</param>
    /// <param name="compatibility">
    /// Whether to use NFKC rather than NFC. NFKC also folds compatibility forms, turning 'ﬁ' into
    /// 'fi' and '①' into '1' — useful for a search key, wrong for preserving what someone wrote.
    /// </param>
    /// <param name="lineEnding">What line endings become.</param>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? Normalise(string? content, bool compatibility = false, string lineEnding = "\n")
    {
        if(string.IsNullOrEmpty(content))
            return content;

        var result = RemoveLoneSurrogates(content);
        result = NormaliseUnicode(result, compatibility);
        result = RemoveInvisibleCharacters(result);
        result = NormaliseLineEndings(result, lineEnding);
        result = TrimLineEnds(result, lineEnding);

        return result.Trim();
    }

    /// <summary>
    /// Applies Unicode normalisation, composed by default. Lone surrogates are removed first, since
    /// <see cref="string.Normalize(NormalizationForm)"/> throws on them.
    /// </summary>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? NormaliseUnicode(string? content, bool compatibility = false)
    {
        if(string.IsNullOrEmpty(content))
            return content;

        var form = compatibility ? NormalizationForm.FormKC : NormalizationForm.FormC;
        var safe = RemoveLoneSurrogates(content);

        return safe.IsNormalized(form) ? safe : safe.Normalize(form);
    }

    /// <summary>
    /// Removes surrogates missing their pair — half a character, produced by cutting a string at a
    /// character count that lands mid-pair.
    /// </summary>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? RemoveLoneSurrogates(string? content)
    {
        if(string.IsNullOrEmpty(content) || !content.AsSpan().ContainsAnyInRange('\uD800', '\uDFFF'))
            return content;

        var builder = new StringBuilder(content.Length);

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if(char.IsHighSurrogate(c))
            {
                if(i + 1 < content.Length && char.IsLowSurrogate(content[i + 1]))
                {
                    builder.Append(c).Append(content[i + 1]);
                    i++;
                }

                continue;
            }

            if(!char.IsLowSurrogate(c))
                builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Removes zero-width and direction-override characters.
    /// </summary>
    /// <param name="content">The content to clean.</param>
    /// <param name="removeJoiners">
    /// Whether to also remove U+200C and U+200D. Only safe where the content is known to be plain
    /// Latin text — these join emoji sequences and are required in Arabic, Persian and Indic scripts.
    /// </param>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? RemoveInvisibleCharacters(string? content, bool removeJoiners = false)
    {
        if(string.IsNullOrEmpty(content))
            return content;

        var hasInvisible = content.AsSpan().ContainsAny(Invisible);
        var hasJoiners = removeJoiners && content.AsSpan().ContainsAny(Joiners);

        if(!hasInvisible && !hasJoiners)
            return content;

        var builder = new StringBuilder(content.Length);
        foreach (var c in content)
        {
            if(Invisible.Contains(c) || (removeJoiners && Joiners.Contains(c)))
                continue;

            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>Makes line endings consistent.</summary>
    /// <param name="content">The content to convert.</param>
    /// <param name="lineEnding">What line endings become.</param>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? NormaliseLineEndings(string? content, string lineEnding = "\n")
    {
        ArgumentNullException.ThrowIfNull(lineEnding);

        if(string.IsNullOrEmpty(content))
            return content;

        //Pairs first: reversed, every \r\n would become two line endings rather than one
        var result = content.Replace("\r\n", "\n").Replace('\r', '\n');

        return lineEnding == "\n" ? result : result.Replace("\n", lineEnding);
    }

    /// <summary>Removes trailing whitespace from each line, leaving the lines themselves intact.</summary>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? TrimLineEnds(string? content, string lineEnding = "\n")
    {
        if(string.IsNullOrEmpty(content))
            return content;

        var lines = content.Split(lineEnding);
        for (var i = 0; i < lines.Length; i++)
            lines[i] = lines[i].TrimEnd();

        return string.Join(lineEnding, lines);
    }

    /// <summary>
    /// Reduces runs of spaces and tabs to one space, leaving line breaks alone. Destructive where
    /// spacing carries meaning — aligned tables, indented code.
    /// </summary>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? CollapseWhitespace(string? content)
        => string.IsNullOrEmpty(content) ? content : HorizontalWhitespaceRegex().Replace(content, " ");

    /// <summary>Reduces runs of blank lines.</summary>
    /// <param name="content">The content to collapse.</param>
    /// <param name="maxBlankLines">The most consecutive blank lines to leave.</param>
    /// <param name="lineEnding">The line ending in use. Run after <see cref="NormaliseLineEndings"/>.</param>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? CollapseBlankLines(string? content, int maxBlankLines = 1, string lineEnding = "\n")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxBlankLines);

        if(string.IsNullOrEmpty(content))
            return content;

        var lines = content.Split(lineEnding);
        var kept = new List<string>(lines.Length);
        var blank = 0;

        foreach (var line in lines)
        {
            if(string.IsNullOrWhiteSpace(line))
            {
                if(++blank > maxBlankLines)
                    continue;
            }
            else
            {
                blank = 0;
            }

            kept.Add(line);
        }

        return string.Join(lineEnding, kept);
    }

    /// <summary>
    /// Replaces typographic quotes with straight ones. Guillemets are left alone — they are the
    /// quotation marks of several languages rather than a stylistic variant.
    /// </summary>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? NormaliseQuotes(string? content)
    {
        if(string.IsNullOrEmpty(content) || !content.AsSpan().ContainsAny(Quotes))
            return content;

        var builder = new StringBuilder(content.Length);
        foreach (var c in content)
        {
            builder.Append(c switch
            {
                '‘' or '’' or '‚' or '‛' => '\'',
                '“' or '”' or '„' or '‟' => '"',
                _ => c
            });
        }

        return builder.ToString();
    }

    /// <summary>Replaces en dashes, em dashes and the minus sign with a hyphen.</summary>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? NormaliseDashes(string? content)
    {
        if(string.IsNullOrEmpty(content) || !content.AsSpan().ContainsAny(Dashes))
            return content;

        var builder = new StringBuilder(content.Length);
        foreach (var c in content)
            builder.Append(Dashes.Contains(c) ? '-' : c);

        return builder.ToString();
    }

    /// <summary>
    /// Strips accents, leaving the base letters — 'café' becomes 'cafe'. For search keys and
    /// comparison rather than for content being kept.
    /// </summary>
    [return: NotNullIfNotNull(nameof(content))]
    public static string? RemoveDiacritics(string? content)
    {
        if(string.IsNullOrEmpty(content))
            return content;

        var decomposed = RemoveLoneSurrogates(content).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if(CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^\S\r\n]+")]
    private static partial Regex HorizontalWhitespaceRegex();
}