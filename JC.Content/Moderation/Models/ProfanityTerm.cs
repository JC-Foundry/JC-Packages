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
    /// Whether this term only matches as a whole word. Short terms need it — 'cum' sits inside
    /// 'document' and 'circumstance' — and it stops those being scanned for at all.
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
        bool wholeWordOnly = true,
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

    //Lower-cased and de-duplicated on the way in, so the matcher never has to care how they arrived
    private static IReadOnlyList<string> Normalise(IEnumerable<string>? values)
        => values?
               .Where(v => !string.IsNullOrWhiteSpace(v))
               .Select(v => v.Trim().ToLowerInvariant())
               .Distinct()
               .ToList()
               .AsReadOnly()
           ?? (IReadOnlyList<string>)[];
}