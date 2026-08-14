using System.Reflection;
using System.Text.Json;
using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Models;

namespace JC.Content.Moderation.Data;

/// <summary>
/// Turns the bundled third-party list into <see cref="ProfanityTerm"/>s. Every editorial decision
/// about that data lives here rather than in the file, so the file stays a verbatim copy of upstream
/// and can be replaced without re-deriving anything by hand.
/// </summary>
internal static class ProfanityDataImporter
{
    private const string ResourceName = "JC.Content.Moderation.Data.en.json";

    /// <summary>
    /// Spellings this short are only ever matched as whole words. 'cum' sits inside 'document' and
    /// 'circumstance', 'ass' inside 'classic' — at this length an inside-word hit is noise rather
    /// than evasion, so it is not looked for at all.
    /// </summary>
    private const int WholeWordOnlyLength = 4;

    /// <summary>
    /// Terms whose upstream severity we disagree with. These are ordinary English words that the
    /// upstream list rates as profanity, and at their imported severity they would block common,
    /// entirely innocent text — 'sex education', 'nude colour'.
    /// </summary>
    private static readonly Dictionary<string, ProfanitySeverity> SeverityOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sex"] = ProfanitySeverity.Mild,
        ["nude"] = ProfanitySeverity.Mild
    };

    /// <summary>
    /// Tags whose terms are slurs aimed at people rather than swearing. A slur outranks a swear word
    /// of the same upstream severity, so these promote a rung wherever they land.
    /// </summary>
    private static readonly HashSet<string> SlurTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "racial", "lgbtq"
    };

    /// <summary>
    /// Promotes only from <see cref="ProfanitySeverity.Medium"/>, unlike <see cref="SlurTags"/>.
    /// Below that band the entries are mild oaths — 'hell', 'damn', 'jesus'.
    /// </summary>
    private const string ReligiousTag = "religious";

    /// <summary>
    /// Reads the bundled list and maps it. Terms come back at <see cref="ProfanityTermSource.Imported"/>,
    /// so an application drowning in false positives can drop the whole set and keep the curated one.
    /// </summary>
    /// <exception cref="InvalidOperationException">The bundled list is missing or unreadable.</exception>
    public static IReadOnlyList<ProfanityTerm> Import()
    {
        var entries = ReadEntries();
        var terms = new List<ProfanityTerm>(entries.Count);

        foreach (var entry in entries)
        {
            var term = Map(entry);
            if(term != null)
                terms.Add(term);
        }

        return terms;
    }

    private static List<ImportedProfanityEntry> ReadEntries()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The bundled profanity list '{ResourceName}' is missing from the assembly.");

        return JsonSerializer.Deserialize<List<ImportedProfanityEntry>>(stream)
               ?? throw new InvalidOperationException($"The bundled profanity list '{ResourceName}' could not be read.");
    }

    private static ProfanityTerm? Map(ImportedProfanityEntry entry)
    {
        if(string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Match))
            return null;

        var spellings = SplitMatches(entry.Match);
        if(spellings.Count == 0)
            return null;

        var tag = entry.Tags?.FirstOrDefault();
        var exceptions = ExpandExceptions(entry.Exceptions, spellings);

        //A promoted slur keeps upstream's spellings and exceptions but becomes ours: Severe, and
        //sourced BuiltIn so dropping the imported set to escape false positives does not drop it
        var promoted = CuratedProfanityTerms.Promotions.TryGetValue(entry.Id, out var curatedCategory);
        var severity = promoted ? ProfanitySeverity.Severe : MapSeverity(entry, tag);
        var category = promoted ? curatedCategory : MapCategory(tag);
        var source = promoted ? ProfanityTermSource.BuiltIn : ProfanityTermSource.Imported;

        //Upstream never sets allow_partial, so the whole-word decision is ours. Honour it if it ever
        //starts appearing, otherwise fall back to length
        var wholeWordOnly = !entry.AllowPartial ?? spellings.Min(s => s.Length) <= WholeWordOnlyLength;

        return new ProfanityTerm(entry.Id,
            spellings,
            severity,
            category,
            source,
            exceptions,
            //A promoted slur is always whole-word only: it blocks at every level, so there is no
            //setting an application could drop to in order to escape an inside-word hit
            promoted || wholeWordOnly,
            entry.Severity);
    }

    /// <summary>
    /// Splits the alternatives and drops the repetition markers. <c>cu*n*t</c> becomes <c>cunt</c> —
    /// the canonicaliser collapses repeated letters anyway, so carrying the pattern would duplicate
    /// work the matcher already does.
    /// </summary>
    private static List<string> SplitMatches(string match)
        => match.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(m => m.Replace("*", string.Empty).Trim())
            .Where(m => m.Length > 0)
            .Distinct()
            .ToList();

    /// <summary>
    /// Maps upstream 1-4 straight onto Mild, Low, Medium and High, then promotes slurs by a rung —
    /// so a band-4 slur reaches <see cref="ProfanitySeverity.Severe"/>, while ordinary profanity tops
    /// out at High however coarse it is.
    /// </summary>
    private static ProfanitySeverity MapSeverity(ImportedProfanityEntry entry, string? tag)
    {
        if(SeverityOverrides.TryGetValue(entry.Id, out var overridden))
            return overridden;

        var severity = entry.Severity switch
        {
            <= 1 => ProfanitySeverity.Mild,
            2 => ProfanitySeverity.Low,
            3 => ProfanitySeverity.Medium,
            _ => ProfanitySeverity.High
        };

        return Promote(severity, tag);
    }

    /// <summary>
    /// Lifts a slur a rung above swearing of the same upstream rating. Without it 'chinaman' and
    /// 'wigger' sit level with 'shit' and pass at the default level.
    /// </summary>
    private static ProfanitySeverity Promote(ProfanitySeverity severity, string? tag)
    {
        if(tag == null)
            return severity;

        if (SlurTags.Contains(tag))
            return severity <= ProfanitySeverity.High ? severity + 1 : severity;

        return severity == ProfanitySeverity.Medium && ReligiousTag.Equals(tag, StringComparison.OrdinalIgnoreCase)
            ? ProfanitySeverity.High
            : severity;
    }

    private static ProfanityCategory MapCategory(string? tag)
        => tag?.ToLowerInvariant() switch
        {
            "sexual" => ProfanityCategory.Sexual,
            "racial" => ProfanityCategory.Racial,
            "lgbtq" => ProfanityCategory.Sexuality,
            "religious" => ProfanityCategory.Religious,
            "shock" => ProfanityCategory.Shock,
            "general" => ProfanityCategory.General,
            _ => ProfanityCategory.None
        };

    /// <summary>
    /// Turns exception patterns into the whole words they stand for. <c>m*cript</c> against
    /// <c>anus</c> gives <c>manuscript</c>. Expanding once here keeps the matcher to a plain
    /// containment check instead of a pattern evaluation per candidate.
    /// </summary>
    private static List<string> ExpandExceptions(List<string>? patterns, List<string> spellings)
    {
        if(patterns == null || patterns.Count == 0)
            return [];

        var expanded = new List<string>(patterns.Count * spellings.Count);
        foreach (var pattern in patterns)
        {
            if(string.IsNullOrWhiteSpace(pattern))
                continue;

            var trimmed = pattern.Trim();

            //A pattern with no placeholder is already the whole word
            if(!trimmed.Contains('*'))
            {
                expanded.Add(trimmed);
                continue;
            }

            //Expanded against every spelling. A nonsense pairing simply never matches anything
            foreach (var spelling in spellings)
                expanded.Add(trimmed.Replace("*", spelling));
        }

        return expanded;
    }
}