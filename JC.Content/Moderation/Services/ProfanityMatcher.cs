using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Helpers;
using JC.Content.Moderation.Models;

namespace JC.Content.Moderation.Services;

/// <summary>
/// Finds terms in folded content and reports where they sit in the original. Built once per term-set
/// version and reused, since indexing the terms is the expensive part and the term set rarely moves.
/// </summary>
internal sealed class ProfanityMatcher
{
    private readonly Dictionary<char, List<IndexedSpelling>> _byFirstCharacter = new();
    private readonly List<IndexedSpelling> _all = [];
    private readonly HashSet<string> _allowed;

    /// <summary>The registry version this was built from. Stale once the registry moves past it.</summary>
    public int Version { get; }

    public ProfanityMatcher(IEnumerable<ProfanityTerm> terms, IEnumerable<string> allowed, int version)
    {
        Version = version;
        _allowed = new HashSet<string>(allowed.Select(a => a.ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);

        foreach (var term in terms)
        {
            //Distinct spellings can fold to the same form - 'blow-job' and 'blow job' both become
            //'blow job'. Indexing both would match the same span twice and report one finding as two
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var spelling in term.Matches)
            {
                var canonical = ProfanityCanonicaliser.CanonicaliseTerm(spelling);
                if(canonical.Length == 0 || !seen.Add(canonical))
                    continue;

                var indexed = new IndexedSpelling(term, canonical);
                _all.Add(indexed);

                if(!_byFirstCharacter.TryGetValue(canonical[0], out var bucket))
                    _byFirstCharacter[canonical[0]] = bucket = [];

                bucket.Add(indexed);
            }
        }
    }

    /// <summary>
    /// Scans <paramref name="canonical"/> and reports every hit, including ones that will not count.
    /// Overlaps are resolved here — a longer or more severe match supersedes what it contains.
    /// </summary>
    public List<ProfanityMatch> Find(string content, CanonicalText canonical, bool matchInsideWords, int contextCharacters)
    {
        var found = new List<ProfanityMatch>(); //Unresolved; overlaps are settled at the end

        for (var start = 0; start < canonical.Length; start++)
        {
            if(canonical.IsSeparator[start])
                continue;

            //A masked first character could begin any term, so the index cannot narrow it
            var candidates = canonical.IsWildcard[start]
                ? _all
                : _byFirstCharacter.GetValueOrDefault(canonical.Value[start]);

            if(candidates == null)
                continue;

            foreach (var candidate in candidates)
            {
                if(!TryMatch(canonical, start, candidate.Canonical, out var end, out var flags, out var leet, out var diacritics))
                    continue;

                var match = Build(content, canonical, candidate, start, end, flags, leet, diacritics, matchInsideWords, contextCharacters);
                if(match != null)
                    found.Add(match);
            }
        }

        return Resolve(found);
    }

    /// <summary>
    /// Walks a spelling against the folded content from <paramref name="start"/>.
    /// </summary>
    /// <remarks>
    /// A run in the content may be longer than the term's but never shorter. That asymmetry is what
    /// separates evasion from coincidence: 'niiigger' reaches 'nigger', while 'Niger' — one 'g' where
    /// the term has two — cannot, and neither can 'as' reach 'ass'.
    /// </remarks>
    private static bool TryMatch(CanonicalText text,
        int start,
        string spelling,
        out int end,
        out ProfanityTransformation flags,
        out int leetCount,
        out int diacriticCount)
    {
        var ti = start;
        var si = 0;
        flags = ProfanityTransformation.None;
        leetCount = 0;
        diacriticCount = 0;
        end = start;

        while (si < spelling.Length)
        {
            var sc = spelling[si];

            var need = 1;
            while (si + need < spelling.Length && spelling[si + need] == sc)
                need++;

            //A space in the term is a real word break in a phrase, so the content must have one too
            if(sc == ' ')
            {
                if(ti >= text.Length || !text.IsSeparator[ti])
                    return false;

                while (ti < text.Length && text.IsSeparator[ti])
                    ti++;

                si += need;
                continue;
            }

            var have = 0;
            while (ti < text.Length)
            {
                //A mask only fills a place the term still needs. Letting it extend a run would have
                //the previous letter swallow it, leaving nothing to stand in for the letter it hid
                if(text.Value[ti] == sc || (text.IsWildcard[ti] && have < need))
                {
                    if(text.IsWildcard[ti])
                        flags |= ProfanityTransformation.MaskWildcard;

                    var applied = text.Applied[ti];
                    flags |= applied;

                    if(applied.HasFlag(ProfanityTransformation.Leetspeak))
                        leetCount++;

                    if(applied.HasFlag(ProfanityTransformation.DiacriticsRemoved))
                        diacriticCount++;

                    have++;
                    ti++;
                    continue;
                }

                //Punctuation pushed between the letters - 's.h.i.t'. Gated on position, not on how
                //much of the current letter has matched: that count resets for every letter of the
                //term, so gating on it meant the first separator could never be stepped over
                if(ti > start && text.IsSeparator[ti])
                {
                    var skip = ti;
                    while (skip < text.Length && text.IsSeparator[skip])
                        skip++;

                    if(skip < text.Length && (text.Value[skip] == sc || text.IsWildcard[skip]))
                    {
                        flags |= ProfanityTransformation.SeparatorsRemoved;
                        ti = skip;
                        continue;
                    }
                }

                break;
            }

            if(have < need)
                return false;

            if(have > need)
                flags |= ProfanityTransformation.RunExpanded;

            si += need;
        }

        end = ti;
        return end > start;
    }

    private ProfanityMatch? Build(string content,
        CanonicalText canonical,
        IndexedSpelling candidate,
        int start,
        int end,
        ProfanityTransformation flags,
        int leetCount,
        int diacriticCount,
        bool matchInsideWords,
        int contextCharacters)
    {
        var term = candidate.Term;

        var originalStart = canonical.SourceIndex[start];
        var originalEnd = canonical.OriginalEnd(end - 1);

        //Boundaries are judged on the original, never the folded form. Folding removes the very
        //punctuation and spacing that says where a word starts
        var startsWord = originalStart == 0 || !char.IsLetterOrDigit(content[originalStart - 1]);
        var endsWord = originalEnd >= content.Length || !char.IsLetterOrDigit(content[originalEnd]);
        var wholeWord = startsWord && endsWord;

        if(!wholeWord)
        {
            if(term.WholeWordOnly || !matchInsideWords)
                return null;

            flags |= ProfanityTransformation.InsideWord;
        }

        var matchedText = content[originalStart..originalEnd];
        var containingWord = ContainingWord(content, originalStart, originalEnd);

        //Suppressed rather than dropped: an application tuning its allowlist needs to see it working
        var allowed = term.Exceptions.Contains(containingWord, StringComparer.OrdinalIgnoreCase)
                      || _allowed.Contains(containingWord)
                      || _allowed.Contains(matchedText);

        var score = allowed
            ? 0
            : ProfanityConfidenceScorer.Score(flags, leetCount, diacriticCount, !wholeWord);

        return new ProfanityMatch
        {
            TermId = term.Id,
            MatchedText = matchedText,
            Context = Context(content, originalStart, originalEnd, contextCharacters),
            Index = originalStart,
            Length = originalEnd - originalStart,
            Severity = term.Severity,
            Category = term.Category,
            Source = term.Source,
            Confidence = ProfanityLevelPolicy.ToConfidence(score),
            ConfidenceScore = score,
            Transformations = flags,
            Allowed = allowed
        };
    }

    /// <summary>
    /// The whole word the match sits within, taken from the original. This is what an exception or an
    /// allowlist entry is compared against — 'manuscript' rather than the 'anus' inside it.
    /// </summary>
    private static string ContainingWord(string content, int start, int end)
    {
        var left = start;
        while (left > 0 && char.IsLetterOrDigit(content[left - 1]))
            left--;

        var right = end;
        while (right < content.Length && char.IsLetterOrDigit(content[right]))
            right++;

        return content[left..right];
    }

    private static string Context(string content, int start, int end, int characters)
    {
        if(characters <= 0)
            return content[start..end];

        var from = Math.Max(0, start - characters);
        var to = Math.Min(content.Length, end + characters);
        return content[from..to];
    }

    /// <summary>
    /// Keeps one match per overlapping span — most severe, then longest, then most confident. A
    /// phrase and the word inside it are one finding, not two, and reporting both would inflate any
    /// count an application takes from this.
    /// </summary>
    private static List<ProfanityMatch> Resolve(List<ProfanityMatch> found)
    {
        //One term can reach the same span by two of its own spellings - 'sandnigger' gets there by
        //stepping over the space that 'sand nigger' matches directly. That is one finding, not two,
        //so the weaker route is dropped rather than reported as superseded
        var ordered = found
            .GroupBy(m => (m.TermId, m.Index, m.Length))
            .Select(g => g.OrderByDescending(m => m.ConfidenceScore).First())
            .OrderByDescending(m => m.Severity)
            .ThenByDescending(m => m.Length)
            .ThenByDescending(m => m.ConfidenceScore)
            .ToList();

        var kept = new List<ProfanityMatch>();
        var resolved = new List<ProfanityMatch>();

        foreach (var match in ordered)
        {
            var overlaps = kept.Any(k => match.Index < k.Index + k.Length && k.Index < match.Index + match.Length);

            resolved.Add(overlaps
                ? Supersede(match)
                : match);

            if(!overlaps)
                kept.Add(match);
        }

        return resolved.OrderBy(m => m.Index).ToList();
    }

    private static ProfanityMatch Supersede(ProfanityMatch match) => new()
    {
        TermId = match.TermId,
        MatchedText = match.MatchedText,
        Context = match.Context,
        Index = match.Index,
        Length = match.Length,
        Severity = match.Severity,
        Category = match.Category,
        Source = match.Source,
        Confidence = match.Confidence,
        ConfidenceScore = match.ConfidenceScore,
        Transformations = match.Transformations,
        Allowed = match.Allowed,
        Superseded = true
    };

    private sealed record IndexedSpelling(ProfanityTerm Term, string Canonical);
}
