using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Models;

namespace JC.Content.Moderation.Data;

/// <summary>
/// Our curation on top of the imported list: the slurs it under-rates, and the words it has no entry
/// for at all. Also the main source of <see cref="ProfanitySeverity.Severe"/> — an imported slur
/// reaches that band only by being rated 4 upstream and promoted a rung.
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
    /// Terms the imported list has no entry for — slurs it misses, inflections it carries no spelling
    /// for, and British slang it does not cover.
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
        Sexuality("poofter", ["poofter", "poofters"]),

        //Inflections upstream has no spelling for. 'shitty' is already there, 'shitting' is not
        General("shitting", ProfanitySeverity.Low, ["shitting", "shits", "shitter", "shitters"]),
        General("shite", ProfanitySeverity.Low, ["shite", "shites"]),
        General("gobshite", ProfanitySeverity.Medium, ["gobshite", "gobshites"]),
        General("cunty", ProfanitySeverity.High, ["cunty", "cunting", "cunted"]),

        //British slang the upstream list does not carry. Banded against what it does: 'bollocks' and
        //'bugger' sit at Mild, 'wanker' and 'tosser' at Low, 'twat' and 'bellend' at Medium
        Sexual("slag", ProfanitySeverity.Medium, ["slag", "slags"]),
        Sexual("slut", ProfanitySeverity.Medium, ["slut", "sluts", "slutty", "slutting"]),
        Sexual("cumslut", ProfanitySeverity.Medium, ["cumslut", "cumsluts", "cum slut", "cum sluts"]),
        Sexual("minge", ProfanitySeverity.Medium, ["minge", "minges"]),
        Sexual("bint", ProfanitySeverity.Low, ["bint", "bints"]),
        General("knobhead", ProfanitySeverity.Medium, ["knobhead", "knobheads", "knob-head"]),
        General("minger", ProfanitySeverity.Low, ["minger", "mingers", "minging"]),
        General("chav", ProfanitySeverity.Low, ["chav", "chavs", "chavvy"]),
        General("pillock", ProfanitySeverity.Mild, ["pillock", "pillocks"]),
        General("plonker", ProfanitySeverity.Mild, ["plonker", "plonkers"]),
        General("prat", ProfanitySeverity.Mild, ["prat", "prats"]),
        General("berk", ProfanitySeverity.Mild, ["berk", "berks"]),
        General("numpty", ProfanitySeverity.Mild, ["numpty", "numpties"]),
        General("wazzock", ProfanitySeverity.Mild, ["wazzock", "wazzocks"]),
        Sexual("knob", ProfanitySeverity.Mild, ["knob", "knobs"], ["doorknob", "doorknobs"]),

        //Mild on purpose: far more often a version control system than an insult
        General("git", ProfanitySeverity.Mild, ["git", "gits"], ["github", "gitlab", "gitignore", "gitea"])
    ];

    //'slagging' is absent deliberately - slagging someone off is criticism, not the noun

    private static ProfanityTerm Racial(string id, IEnumerable<string> matches, IEnumerable<string>? exceptions = null)
        => Curated(id, ProfanitySeverity.Severe, ProfanityCategory.Racial, matches, exceptions);

    private static ProfanityTerm Sexuality(string id, IEnumerable<string> matches, IEnumerable<string>? exceptions = null)
        => Curated(id, ProfanitySeverity.Severe, ProfanityCategory.Sexuality, matches, exceptions);

    private static ProfanityTerm General(string id, ProfanitySeverity severity, IEnumerable<string> matches,
        IEnumerable<string>? exceptions = null)
        => Curated(id, severity, ProfanityCategory.General, matches, exceptions);

    private static ProfanityTerm Sexual(string id, ProfanitySeverity severity, IEnumerable<string> matches,
        IEnumerable<string>? exceptions = null)
        => Curated(id, severity, ProfanityCategory.Sexual, matches, exceptions);

    private static ProfanityTerm Curated(string id,
        ProfanitySeverity severity,
        ProfanityCategory category,
        IEnumerable<string> matches,
        IEnumerable<string>? exceptions)
        => new(id,
            matches,
            severity,
            category,
            ProfanityTermSource.BuiltIn,
            exceptions);
}
