using System.Globalization;
using System.Text;
using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Services;

/// <summary>
/// Folds content into a comparable form: case, accents, leetspeak and inserted punctuation all come
/// out, and every fold is recorded so the matcher can charge it against confidence.
/// </summary>
/// <remarks>
/// Deliberately separate from any public normalisation. This output is lossy and is only ever
/// matched against — it is never shown to anyone, and it must not be confused with cleaning content
/// an application intends to keep.
/// </remarks>
internal static class ProfanityCanonicaliser
{
    /// <summary>
    /// Digits and symbols that stand in for letters. Deliberately conservative: every entry here
    /// widens what matches, and a wrong one produces false positives on ordinary text containing
    /// numbers.
    /// </summary>
    private static readonly Dictionary<char, char> Leet = new()
    {
        ['0'] = 'o', ['1'] = 'i', ['3'] = 'e', ['4'] = 'a', ['5'] = 's',
        ['7'] = 't', ['8'] = 'b', ['9'] = 'g',
        ['@'] = 'a', ['$'] = 's', ['!'] = 'i', ['|'] = 'l', ['+'] = 't'
    };

    /// <summary>
    /// Characters used to blank out a letter. Treated as "any single letter" rather than removed,
    /// because someone typing 'f*ck' has already shown they knew what they were writing.
    /// </summary>
    private static readonly HashSet<char> Masks = ['*', '#', '%'];

    /// <summary>
    /// Letters from other scripts that render as Latin ones, keyed on the lower-cased form so the
    /// upper-case pairs fold through the same entries. Where the two cases point at different Latin
    /// letters the capital wins — Greek 'ν' resembles 'v', but its capital is 'N'.
    /// </summary>
    private static readonly Dictionary<char, char> Homoglyphs = new()
    {
        //Cyrillic
        ['а'] = 'a', ['в'] = 'b', ['е'] = 'e', ['к'] = 'k', ['м'] = 'm',
        ['н'] = 'h', ['о'] = 'o', ['р'] = 'p', ['с'] = 'c', ['т'] = 't',
        ['у'] = 'y', ['х'] = 'x', ['ѕ'] = 's', ['і'] = 'i', ['ј'] = 'j',
        ['ѵ'] = 'v', ['һ'] = 'h', ['ԁ'] = 'd', ['ԛ'] = 'q', ['ԝ'] = 'w',

        //Greek
        ['α'] = 'a', ['β'] = 'b', ['ε'] = 'e', ['ζ'] = 'z', ['η'] = 'h',
        ['ι'] = 'i', ['κ'] = 'k', ['μ'] = 'm', ['ν'] = 'n', ['ο'] = 'o',
        ['ρ'] = 'p', ['ς'] = 's', ['τ'] = 't', ['υ'] = 'y', ['χ'] = 'x'
    };

    /// <summary>Placeholder for a masked letter. A private-use code point, so it cannot occur in
    /// real content or in a term, and only ever matches by the rule that handles it.</summary>
    public const char Wildcard = '';

    public static CanonicalText Canonicalise(string text)
    {
        var value = new List<char>(text.Length);
        var sourceIndex = new List<int>(text.Length);
        var sourceLength = new List<int>(text.Length);
        var applied = new List<ProfanityTransformation>(text.Length);
        var isSeparator = new List<bool>(text.Length);
        var isWildcard = new List<bool>(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var original = text[i];
            var transformation = ProfanityTransformation.None;
            var c = original;

            //Accents come off one character at a time. Decomposing the whole string first would
            //shift every index and cost us the map back to the original
            if(c > 127)
            {
                var stripped = StripDiacritic(c);
                if(stripped != c)
                {
                    c = stripped;
                    transformation |= ProfanityTransformation.DiacriticsRemoved;
                }
            }

            var lowered = char.ToLowerInvariant(c);
            if(lowered != c)
                transformation |= ProfanityTransformation.CaseFolded;
            c = lowered;

            if(Homoglyphs.TryGetValue(c, out var latin))
            {
                c = latin;
                transformation |= ProfanityTransformation.HomoglyphFolded;
            }

            if(Masks.Contains(c))
            {
                Append(Wildcard, ProfanityTransformation.MaskWildcard, separator: false, wildcard: true);
                continue;
            }

            if(Leet.TryGetValue(c, out var decoded))
            {
                Append(decoded, transformation | ProfanityTransformation.Leetspeak, separator: false, wildcard: false);
                continue;
            }

            if(char.IsLetterOrDigit(c))
            {
                Append(c, transformation, separator: false, wildcard: false);
                continue;
            }

            //A run of punctuation or whitespace becomes one separator. Kept rather than dropped so
            //phrases still match on their spaces, and so word boundaries survive the fold
            if(value.Count > 0 && isSeparator[^1])
            {
                sourceLength[^1] = i - sourceIndex[^1] + 1;
                continue;
            }

            Append(' ', ProfanityTransformation.None, separator: true, wildcard: false);
            continue;

            void Append(char ch, ProfanityTransformation applying, bool separator, bool wildcard)
            {
                value.Add(ch);
                sourceIndex.Add(i);
                sourceLength.Add(1);
                applied.Add(applying);
                isSeparator.Add(separator);
                isWildcard.Add(wildcard);
            }
        }

        return new CanonicalText
        {
            Value = [.. value],
            SourceIndex = [.. sourceIndex],
            SourceLength = [.. sourceLength],
            Applied = [.. applied],
            IsSeparator = [.. isSeparator],
            IsWildcard = [.. isWildcard]
        };
    }

    /// <summary>
    /// Folds a term the same way, minus the bookkeeping. Terms are authored in plain lower-case, so
    /// this is mostly a guard against one arriving with an accent or stray punctuation.
    /// </summary>
    public static string CanonicaliseTerm(string term)
    {
        var builder = new StringBuilder(term.Length);
        var lastWasSeparator = false;

        foreach (var original in term)
        {
            var c = original > 127 ? StripDiacritic(original) : original;
            c = char.ToLowerInvariant(c);

            if(Homoglyphs.TryGetValue(c, out var latin))
                c = latin;

            if(Leet.TryGetValue(c, out var decoded))
                c = decoded;

            if(char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastWasSeparator = false;
                continue;
            }

            if(lastWasSeparator)
                continue;

            builder.Append(' ');
            lastWasSeparator = true;
        }

        return builder.ToString().Trim();
    }

    private static char StripDiacritic(char c)
    {
        //Half a character on its own is not valid Unicode, and Normalize throws on it. Surrogates
        //carry no diacritic to strip anyway, and fall through to being treated as a separator
        if(char.IsSurrogate(c))
            return c;

        var decomposed = c.ToString().Normalize(NormalizationForm.FormD);
        foreach (var part in decomposed)
        {
            if(CharUnicodeInfo.GetUnicodeCategory(part) != UnicodeCategory.NonSpacingMark)
                return part;
        }

        return c;
    }
}
