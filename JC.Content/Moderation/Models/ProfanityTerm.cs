using JC.Content.Helpers;
using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Models;

/// <summary>
/// A single blocked term and the metadata a match reports. One term may have several spellings.
/// </summary>
public class ProfanityTerm
{
    /// <summary>Stable identifier. Also the key the registry holds the term under.</summary>
    public string Id { get; }

    /// <summary>The spellings that match this term, lower-cased. Never empty.</summary>
    public IReadOnlyList<string> Matches { get; }

    /// <summary>
    /// Whole words that contain a spelling but are not this term — 'manuscript' for 'anus'. A match
    /// falling inside one of these is reported at zero confidence rather than counted.
    /// </summary>
    public IReadOnlyList<string> Exceptions { get; }

    public ProfanitySeverity Severity { get; }
    public ProfanityCategory Category { get; }
    public ProfanityTermSource Source { get; }

    /// <summary>
    /// Whether to stop reporting this term when it turns up inside a longer word — 'cum' in
    /// 'document', 'ass' in 'classic'. Off by default: an inside-word match is capped in Low and can
    /// never block, so this only silences the noise a term makes in ordinary prose.
    /// </summary>
    public bool WholeWordOnly { get; }

    /// <summary>
    /// The severity the imported list gave this term, before our mapping. Null for terms that came
    /// from anywhere else. Kept so the mapping can be revised without re-importing.
    /// </summary>
    public int? SourceSeverity { get; }

    public ProfanityTerm(string id,
        IEnumerable<string> matches,
        ProfanitySeverity severity,
        ProfanityCategory category,
        ProfanityTermSource source = ProfanityTermSource.Configured,
        IEnumerable<string>? exceptions = null,
        bool wholeWordOnly = false,
        int? sourceSeverity = null)
    {
        if(string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A term needs an id.", nameof(id));

        var spellings = Normalise(matches);
        if(spellings.Count == 0)
            throw new ArgumentException("A term needs at least one spelling.", nameof(matches));

        if(severity == ProfanitySeverity.None)
            throw new ArgumentException("A term cannot have a severity of None.", nameof(severity));

        Id = id.Trim();
        Matches = spellings;
        Exceptions = Normalise(exceptions);
        Severity = severity;
        Category = category;
        Source = source;
        WholeWordOnly = wholeWordOnly;
        SourceSeverity = sourceSeverity;
    }

    //Cleaned, lower-cased and de-duplicated on the way in, so the matcher never has to care how they
    //arrived. A decomposed accent or a zero-width would otherwise reach the canonicaliser as a
    //non-letter, which reads as a separator and quietly turns the term into a phrase
    private static IReadOnlyList<string> Normalise(IEnumerable<string>? values)
        => values?
               .Select(v => NormalisationHelper.RemoveInvisibleCharacters(
                   NormalisationHelper.NormaliseUnicode(v)))
               .Where(v => !string.IsNullOrWhiteSpace(v))
               .Select(v => v.Trim().ToLowerInvariant())
               .Distinct()
               .ToList()
               .AsReadOnly()
           ?? (IReadOnlyList<string>)[];
}