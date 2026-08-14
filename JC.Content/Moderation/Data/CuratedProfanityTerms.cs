using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Models;

namespace JC.Content.Moderation.Data;

/// <summary>
/// Our curation on top of the imported list, and the main source of
/// <see cref="ProfanitySeverity.Severe"/> — an imported slur reaches that band only by being rated
/// 4 upstream and promoted a rung.
/// </summary>
/// <remarks>
/// <para>
/// Severe is reserved for slurs aimed at people — race, ethnicity, sexuality, gender identity. It is
/// not a "worse swear word" rung: ordinary profanity tops out at High however coarse it is.
/// </para>
/// <para>
/// Most of these already exist upstream at severity 3 or 4, so <see cref="Promotions"/> raises them
/// in place rather than restating them. That keeps upstream's spellings and exceptions — several
/// carry variants worth having, and restating a term by hand would silently drop them the next time
/// the file is replaced. <see cref="Additions"/> covers only what upstream has no entry for.
/// </para>
/// <para>
/// A promoted term is re-sourced to <see cref="ProfanityTermSource.BuiltIn"/>, so an application that
/// drops the imported set to escape false positives keeps the slurs.
/// </para>
/// <para>
/// Some entries have an innocent whole-word meaning in British English — a <c>fag</c> is a cigarette,
/// a <c>dyke</c> is an embankment, a <c>chink</c> is a gap. They stay Severe regardless. An
/// application needing one allows it explicitly through <c>ProfanityTermRegistry.Allow</c>, which is
/// a deliberate decision on its part rather than a default made on everyone's behalf.
/// </para>
/// </remarks>
internal static class CuratedProfanityTerms
{
    /// <summary>
    /// Upstream ids promoted to <see cref="ProfanitySeverity.Severe"/>, against the category we hold
    /// them under — which occasionally corrects upstream, as with <c>raghead</c>, tagged religious
    /// there but functioning as an ethnic slur.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, ProfanityCategory> Promotions =
        new Dictionary<string, ProfanityCategory>(StringComparer.OrdinalIgnoreCase)
        {
            ["nigger"] = ProfanityCategory.Racial,
            ["nigga"] = ProfanityCategory.Racial,
            ["sand-nigger"] = ProfanityCategory.Racial,
            ["timber-nigger"] = ProfanityCategory.Racial,
            ["kike"] = ProfanityCategory.Racial,
            ["chink"] = ProfanityCategory.Racial,
            ["gook"] = ProfanityCategory.Racial,
            ["spic"] = ProfanityCategory.Racial,
            ["wetback"] = ProfanityCategory.Racial,
            ["coon"] = ProfanityCategory.Racial,
            ["paki"] = ProfanityCategory.Racial,
            ["wog"] = ProfanityCategory.Racial,
            ["raghead"] = ProfanityCategory.Racial,
            ["towelhead"] = ProfanityCategory.Racial,
            ["darkie"] = ProfanityCategory.Racial,
            ["jigaboo"] = ProfanityCategory.Racial,
            ["golliwog"] = ProfanityCategory.Racial,

            ["faggot"] = ProfanityCategory.Sexuality,
            ["fag"] = ProfanityCategory.Sexuality,
            ["fag-bomb"] = ProfanityCategory.Sexuality,
            ["dyke"] = ProfanityCategory.Sexuality,
            ["bulldyke"] = ProfanityCategory.Sexuality,
            ["tranny"] = ProfanityCategory.Sexuality,
            ["shemale"] = ProfanityCategory.Sexuality,
            ["poof"] = ProfanityCategory.Sexuality,
            ["batty-boy"] = ProfanityCategory.Sexuality,
            ["lady-boy"] = ProfanityCategory.Sexuality
        };

    /// <summary>
    /// Slurs the imported list has no entry for. Whole-word only, like everything at this severity:
    /// these are short, several sit inside innocent words, and a term that blocks at every level
    /// leaves an application no setting to escape to.
    /// </summary>
    public static IReadOnlyList<ProfanityTerm> Additions() =>
    [
        Racial("abo", ["abo", "abos"], ["about", "above", "abode", "abort", "abolish", "aboriginal"]),
        Racial("gyppo", ["gyppo", "gyppos", "gypo", "gypos"]),
        Racial("sambo", ["sambo", "sambos"]),
        Racial("pickaninny", ["pickaninny", "pickaninnies", "piccaninny", "piccaninnies"]),
        Racial("half-caste", ["half-caste", "half caste", "halfcaste"]),

        // 'queer' is deliberately absent: widely reclaimed and used self-referentially, so it cannot
        // be blocked at every level without blocking the people it belongs to
        Sexuality("poofter", ["poofter", "poofters"])
    ];

    private static ProfanityTerm Racial(string id, IEnumerable<string> matches, IEnumerable<string>? exceptions = null)
        => Curated(id, matches, ProfanityCategory.Racial, exceptions);

    private static ProfanityTerm Sexuality(string id, IEnumerable<string> matches, IEnumerable<string>? exceptions = null)
        => Curated(id, matches, ProfanityCategory.Sexuality, exceptions);

    private static ProfanityTerm Curated(string id,
        IEnumerable<string> matches,
        ProfanityCategory category,
        IEnumerable<string>? exceptions)
        => new(id,
            matches,
            ProfanitySeverity.Severe,
            category,
            ProfanityTermSource.BuiltIn,
            exceptions,
            wholeWordOnly: true);
}
