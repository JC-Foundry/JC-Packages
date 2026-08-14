using JC.Content.Moderation.Data;
using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Models;

namespace JC.Content.Moderation.Services;

/// <summary>
/// The active set of blocked terms, and the words that suppress a match. Seeded once and held for
/// the life of the application, so a matcher can index it rather than rebuilding per call.
/// </summary>
/// <remarks>
/// Terms are keyed by id. Where two sources supply the same id the higher-precedence one wins —
/// <see cref="ProfanityTermSource.Configured"/>, then <see cref="ProfanityTermSource.BuiltIn"/>,
/// then <see cref="ProfanityTermSource.Imported"/> — so an application can restate a term it
/// disagrees with rather than having to remove and re-add it.
/// </remarks>
public class ProfanityTermRegistry
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, ProfanityTerm> _terms = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase);

    private int _version;

    /// <summary>
    /// Increments on every change to the terms or the allowlist. A matcher holding a prepared index
    /// compares this to know whether the index it built is still current.
    /// </summary>
    public int Version
    {
        get { lock (_lock) return _version; }
    }

    /// <summary>The number of terms currently registered.</summary>
    public int Count
    {
        get { lock (_lock) return _terms.Count; }
    }

    /// <summary>Every registered term, in no particular order.</summary>
    public IReadOnlyList<ProfanityTerm> GetTerms()
    {
        lock (_lock)
            return _terms.Values.ToList();
    }

    /// <summary>Every registered term from one source.</summary>
    public IReadOnlyList<ProfanityTerm> GetTerms(ProfanityTermSource source)
    {
        lock (_lock)
            return _terms.Values.Where(t => t.Source == source).ToList();
    }

    /// <summary>
    /// Words and phrases that suppress a match falling inside them, whatever term matched. Applies on
    /// top of a term's own <see cref="ProfanityTerm.Exceptions"/>, and is where an application kills
    /// a false positive it has actually seen.
    /// </summary>
    public IReadOnlyCollection<string> GetAllowed()
    {
        lock (_lock)
            return _allowed.ToList();
    }

    public bool TryGetTerm(string id, out ProfanityTerm? term)
    {
        term = null;
        if(string.IsNullOrWhiteSpace(id))
            return false;

        lock (_lock)
            return _terms.TryGetValue(id.Trim(), out term);
    }

    /// <summary>
    /// Registers a term, replacing an existing one of the same id when this one comes from an equal
    /// or higher-precedence source.
    /// </summary>
    /// <returns>
    /// <c>false</c> when a term of that id is already registered from a higher-precedence source,
    /// leaving the existing one in place.
    /// </returns>
    public bool TryAddTerm(ProfanityTerm term)
    {
        ArgumentNullException.ThrowIfNull(term);

        lock (_lock)
        {
            if(_terms.TryGetValue(term.Id, out var existing) && Precedence(existing.Source) > Precedence(term.Source))
                return false;

            _terms[term.Id] = term;
            _version++;
            return true;
        }
    }

    /// <summary>Registers several terms, returning how many were taken.</summary>
    public int AddTerms(IEnumerable<ProfanityTerm> terms)
    {
        ArgumentNullException.ThrowIfNull(terms);
        return terms.Count(TryAddTerm);
    }

    /// <summary>
    /// Removes a term entirely, so it is never looked for. Different from allowing a word: this drops
    /// the term, where <see cref="Allow(string)"/> keeps it but forgives a specific context.
    /// </summary>
    public bool TryRemoveTerm(string id)
    {
        if(string.IsNullOrWhiteSpace(id))
            return false;

        lock (_lock)
        {
            if(!_terms.Remove(id.Trim()))
                return false;

            _version++;
            return true;
        }
    }

    /// <summary>Empties the term set, including everything seeded at startup.</summary>
    public void ClearTerms()
    {
        lock (_lock)
        {
            _terms.Clear();
            _version++;
        }
    }

    /// <summary>Removes every term from one source, leaving the others in place.</summary>
    public int RemoveTerms(ProfanityTermSource source)
    {
        lock (_lock)
        {
            var ids = _terms.Values.Where(t => t.Source == source).Select(t => t.Id).ToList();
            foreach (var id in ids)
                _terms.Remove(id);

            if(ids.Count > 0)
                _version++;

            return ids.Count;
        }
    }

    /// <summary>Adds a word or phrase that suppresses any match falling inside it.</summary>
    public bool Allow(string word)
    {
        if(string.IsNullOrWhiteSpace(word))
            return false;

        lock (_lock)
        {
            if(!_allowed.Add(word.Trim()))
                return false;

            _version++;
            return true;
        }
    }

    /// <summary>Adds several allowed words, returning how many were taken.</summary>
    public int Allow(IEnumerable<string> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        return words.Count(Allow);
    }

    public bool Disallow(string word)
    {
        if(string.IsNullOrWhiteSpace(word))
            return false;

        lock (_lock)
        {
            if(!_allowed.Remove(word.Trim()))
                return false;

            _version++;
            return true;
        }
    }

    public void ClearAllowed()
    {
        lock (_lock)
        {
            _allowed.Clear();
            _version++;
        }
    }

    /// <summary>
    /// Seeds the term set. The curated terms always load; <paramref name="includeImported"/> decides
    /// whether the broader bundled set loads with them.
    /// </summary>
    /// <remarks>
    /// The bundled file is read either way, because the curated slurs are promotions of entries
    /// within it and carry upstream's spellings and exceptions. What the flag controls is whether
    /// everything else in that file is kept — which is the switch for an application that wants
    /// accuracy over coverage.
    /// </remarks>
    /// <param name="includeImported">Whether to keep the bundled terms our curation did not promote.</param>
    /// <returns>The number of terms registered.</returns>
    internal int Seed(bool includeImported)
    {
        var imported = ProfanityDataImporter.Import();

        var terms = includeImported
            ? imported
            : imported.Where(t => t.Source == ProfanityTermSource.BuiltIn).ToList();

        return AddTerms(terms) + AddTerms(CuratedProfanityTerms.Additions());
    }

    private static int Precedence(ProfanityTermSource source)
        => source switch
        {
            ProfanityTermSource.Configured => 2,
            ProfanityTermSource.BuiltIn => 1,
            _ => 0
        };
}