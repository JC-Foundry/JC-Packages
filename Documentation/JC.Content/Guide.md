# JC.Content — Guide

Covers profanity moderation, content comparison, format conversion, HTML sanitisation, text normalisation, and the pipeline that composes them. See [Setup](Setup.md) for registration and option defaults.

Each feature area works on its own, and the last section covers `ContentManager`, which runs several of them in one call. If you registered with `AddContentManager` and want the composed API, [start there](#the-content-pipeline).

## Moderation

### Basic usage

Inject `ProfanityModerator` and ask it what it found:

```csharp
using JC.Content.Moderation.Services;

public class CommentService(ProfanityModerator moderator)
{
    public bool IsAcceptable(string? comment)
    {
        var result = moderator.Analyse(comment);
        return !result.ShouldBlock;
    }
}
```

`Analyse` reports and nothing more. The content is never altered, nothing is rejected on your behalf, and `ShouldBlock` is the package's reading of the level in force — you are free to ignore it and apply your own thresholds to what was found.

Null or whitespace content comes back clean, so there is no need to guard the call.

### Reading a result

```csharp
var result = moderator.Analyse(comment);

if (result.ShouldBlock)
{
    logger.LogWarning(
        "Comment rejected: {Severity} {Category} at {Score}% confidence, {Count} match(es)",
        result.Severity, result.Category, result.ConfidenceScore, result.CountedMatches.Count());
}
```

| Member | Type | Description |
|--------|------|-------------|
| `ShouldBlock` | `bool` | Whether anything met the level's floors |
| `Severity` | `ProfanitySeverity` | The worst severity found, whether or not it counted |
| `Confidence` | `ProfanityConfidence` | Confidence in the `Severity` finding — not the highest confidence anywhere |
| `ConfidenceScore` | `int` | The percentage behind `Confidence` |
| `Category` | `ProfanityCategory` | The category of the finding that set `Severity` |
| `Matches` | `IReadOnlyList<ProfanityMatch>` | Everything found, counted or not |
| `CountedMatches` | `IEnumerable<ProfanityMatch>` | Only the matches that met the floors |
| `HasMatches` | `bool` | Whether anything was found at all |
| `Level` | `ProfanityLevel` | The level applied, from registration or the call |
| `Truncated` | `bool` | Whether the content ran past `MaxContentLength` |
| `ScannedLength` | `int` | How many characters were examined |

Each `ProfanityMatch` carries the term id, the matched text as it appears in the content, its `Index` and `Length` in the original, a `Context` window either side, the severity, category and source, the confidence band and score, the `Transformations` the matcher had to apply, and three flags — `Counted`, `Allowed` and `Superseded`.

### Masking, removing and tagging

`ProfanityMasker` rewrites what the moderator found. It returns a `ProfanityModerationMaskResult`, which carries the moderation result alongside the rewritten text:

```csharp
using JC.Content.Moderation.Services;

public class PostService(ProfanityMasker masker)
{
    public string? Clean(string? body)
    {
        var result = masker.AnalyseAndMask(body);

        // result.UpdatedContent     — the rewritten text
        // result.OriginalContent    — as supplied
        // result.ReplacementCount   — how many matches were replaced
        // result.WasModified        — whether the two differ
        // result.ModerationResult   — everything the moderator reported
        return result.UpdatedContent;
    }
}
```

**Masking** replaces each match with a run of one character:

```csharp
masker.AnalyseAndMask(body);                              // "gobshite" -> "****"
masker.AnalyseAndMask(body, maskChar: '#');               // "gobshite" -> "####"
masker.AnalyseAndMask(body, cappedMaskLength: null);      // "gobshite" -> "********"
masker.AnalyseAndMask(body, preserveLength: true, cappedMaskLength: 4);  // a 3-letter match -> "***"
```

The two length parameters work together:

| `preserveLength` | `cappedMaskLength` | Run written |
|------------------|--------------------|-------------|
| `false` (default) | `4` (default) | Always four characters, whatever the match length |
| `true` | `4` | The match length, up to four |
| either | `null` | The match length, uncapped |

The default hides the length of the original, which is why a fixed run rather than a matching one is the starting point.

**Removal** strips the match, collapsing the whitespace either side of the seam to one:

```csharp
var result = masker.AnalyseAndRemove(body);
// "what a gobshite that is" -> "what a that is"
```

**Tagging** replaces each match with a template:

```csharp
masker.AnalyseAndTag(body);                            // -> "[Removed]"
masker.AnalyseAndTag(body, "[{severity}]");            // -> "[Medium]"
masker.AnalyseAndTag(body, "[{category}:{severity}]"); // -> "[General:Medium]"
masker.AnalyseAndTag(body, "[redacted]");              // -> "[redacted]"
```

`{category}` and `{severity}` are substituted case-insensitively; a format naming neither is used verbatim. A match with no category substitutes `Removed`. The placeholders and the default format are available as constants — `ProfanityMasker.CategoryTag`, `SeverityTag`, `GenericTag` and `GenericTagValue` — so a call site can build a format without hard-coding the braces.

### Overriding the level for one call

Every moderation method takes an optional level that overrides the registered default:

```csharp
using JC.Content.Moderation.Enums;

// A display name is held to a stricter standard than a private message
var name = masker.AnalyseAndMask(model.DisplayName, level: ProfanityLevel.SuperStrict);
var message = masker.AnalyseAndMask(model.Body, level: ProfanityLevel.Lax);
```

Detection is identical at every level — the same terms are found and reported. The level decides only which of those findings count, and therefore whether the content blocks and what gets rewritten.

### Applying your own thresholds

`ShouldBlock` is a convenience, not a verdict. `ProfanityLevelPolicy` is public so an application can apply the same arithmetic to its own rules without restating them:

```csharp
using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Helpers;

var result = moderator.Analyse(comment);

// Registered at Safe, but this field is held to Strict
var breachesStrict = result.Matches.Any(m =>
    m is { Allowed: false, Superseded: false }
    && ProfanityLevelPolicy.Counts(ProfanityLevel.Strict, m.Severity, m.Confidence));

// Or ignore the bands entirely and work from the score
var confidentSlur = result.Matches.Any(m =>
    m.Category is ProfanityCategory.Racial or ProfanityCategory.Sexuality
    && m.ConfidenceScore >= 80);
```

`ProfanityLevelPolicy.Floors(level)` returns the severity and confidence pair a level demands, which is useful for showing the current policy on a settings screen.

### Changing terms at runtime

The registry is a singleton, so terms and allowlist entries can be changed after startup — an admin screen adding a banned word, for instance. The moderator notices and rebuilds its index on the next call:

```csharp
using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Models;
using JC.Content.Moderation.Services;

public class ModerationSettingsService(ProfanityTermRegistry registry)
{
    public void Ban(string word)
        => registry.TryAddTerm(new ProfanityTerm(
            id: $"custom-{word}",
            matches: [word],
            severity: ProfanitySeverity.Medium,
            category: ProfanityCategory.Custom));

    public void Forgive(string word) => registry.Allow(word);

    public int TermCount => registry.Count;
}
```

Indexing the term set is the expensive part of moderation, so the matcher is built once and reused until `Version` moves. A burst of changes costs one rebuild on the next call, not one per change. The full method list is in [Setup](Setup.md#configuring-terms).

### Nuances and gotchas

**Nothing is rewritten unless the content breaches the level.** All three `AnalyseAnd*` methods return the content untouched when `ShouldBlock` is `false`, and rewrite only the matches that counted when it is `true`. A Mild finding at the default `Safe` level is reported but left in the text:

```csharp
var result = masker.AnalyseAndMask("what a pillock");

// result.ModerationResult.HasMatches   == true   — 'pillock' was found
// result.ModerationResult.ShouldBlock  == false  — Mild does not count at Safe
// result.WasModified                   == false  — so nothing was masked
```

If you want every finding rewritten regardless of severity, moderate at `ProfanityLevel.SuperStrict`.

**`Matches` includes what did not count.** Allowed matches, low-confidence matches and superseded overlaps are all reported, deliberately — that list is the tuning surface. Use `CountedMatches` when counting findings, and the full list when working out why something was or was not caught.

**`Severity` and `Confidence` describe the same finding.** The pair is taken from one match, so a shaky severe finding does not read as certain because some obvious mild one sat alongside it. Do not treat `Confidence` as the highest confidence in the content.

**A match inside a longer word or across a word break can never block.** Both are capped below `MediumConfidenceMinimum`, so no level can count them. They are still reported, which is how deliberate padding shows up, but code that acts on `HasMatches` rather than `ShouldBlock` will fire on ordinary prose.

**Truncation cuts the returned content, not just the scan.** When `MaxContentLength` is exceeded, `UpdatedContent` is cut to `ScannedLength` — the tail was never examined, so it is not handed back as though it had been. Check `Truncated` before storing the result as the whole of anything.

**Allowing a word and removing a term are different.** `Allow` keeps the term but forgives the whole word the match landed inside, and the match is still reported at zero confidence so you can see the allowlist working. `TryRemoveTerm` drops the term entirely.

**`AnalyseAndTag` throws on an empty format.** Passing `null` or `""` raises `ArgumentException` — use `AnalyseAndRemove` to strip matches instead.

## Comparison

### Basic usage

```csharp
using JC.Content.Comparison.Services;

public class RevisionService(ContentComparer comparer)
{
    public bool HasChanged(string? before, string? after)
        => comparer.Compare(before, after).HasChanges;
}
```

Null is treated as empty on either side, and two identical versions short-circuit without being chunked.

### Working with segments

`Segments` is the result; everything else is a projection over it. Runs are contiguous and in order, so concatenating them reconstructs either version — skipping `Added` for the original, `Removed` for the revised:

```csharp
using JC.Content.Comparison.Enums;

var result = comparer.Compare(before, after);

foreach (var segment in result.Changes)
{
    switch (segment.Type)
    {
        case ContentChangeType.Added:
            logger.LogInformation("Added at {Index}: {Text}", segment.RevisedIndex, segment.Text);
            break;
        case ContentChangeType.Removed:
            logger.LogInformation("Removed at {Index}: {Text}", segment.OriginalIndex, segment.Text);
            break;
    }
}
```

`OriginalIndex` is `null` on an addition and `RevisedIndex` is `null` on a removal, because neither has a position in the version it is absent from. `Changes` filters to the changed runs; `Segments` keeps the unchanged ones too, which is what you want when rendering the whole document.

### Rendering a diff

`Render` walks the segments and wraps the changed ones. The defaults produce a plain-text diff:

```csharp
result.Render();
// additions wrapped as {+added+}, removals as [-removed-], unchanged runs written as they are
```

Supply your own markers and an encoder to produce HTML. The markers are written as given; only the content passes through the encoder, so tags survive and the content cannot become markup:

```csharp
using System.Net;

var html = result.Render(
    addedOpen: "<ins>",
    addedClose: "</ins>",
    removedOpen: "<del>",
    removedClose: "</del>",
    encode: value => WebUtility.HtmlEncode(value));
```

Anything `Render` cannot express — a side-by-side view, a summary count, changes grouped by paragraph — is built by walking `Segments` directly rather than by parsing rendered text back apart.

### Choosing granularity

The registered default applies unless a call names its own:

```csharp
using JC.Content.Comparison.Enums;

// Prose reads best word by word — the default
var body = comparer.Compare(before, after);

// A title is short enough for character precision to be useful
var title = comparer.Compare(oldTitle, newTitle, ComparisonGranularity.Character);

// Structured content belongs on line boundaries
var config = comparer.Compare(oldYaml, newYaml, ComparisonGranularity.Line);
```

### Nuances and gotchas

**Content is compared exactly as supplied.** Nothing is normalised, trimmed or case-folded on the way in, so a change of line ending or of casing is a change. Deciding otherwise is the caller's job — run the content through [normalisation](#normalisation) first, or use the [pipeline](#the-content-pipeline), which does it for you.

**Line granularity keeps terminators, so a line-ending change marks every line as changed.** That is the correct answer to the question asked, but it is rarely the question intended. Normalise line endings before comparing content that may have come from different platforms.

**Word granularity attaches trailing whitespace to the word before it.** A spacing change is therefore reported against that word rather than as an edit of its own, and leading whitespace at the very start of the content becomes its own piece.

**Segment indices point into the result's own copies of the content.** Use `result.OriginalContent` and `result.RevisedContent`, not the strings you passed in. When `Truncated` is `true` those copies are the cut versions, and the indices are only valid against them.

**Adjacent runs of the same type are combined.** An unchanged paragraph is one segment, not one per word, so do not assume a segment corresponds to a single chunk of the configured granularity.

**Cost rises with how much the two versions differ, not with their length alone.** Two long and largely unrelated documents at `Character` granularity is the expensive case; `MaxContentLength` exists for content you do not control.

## Conversion

### Basic usage

```csharp
using JC.Content.Conversion.Enums;
using JC.Content.Conversion.Services;

public class ArticleRenderer(ContentConverter converter)
{
    public string? ToHtml(string? markdown)
        => converter.Convert(markdown, ContentFormat.Markdown, ContentFormat.Html);
}
```

Six named shortcuts cover every pair, and each is equivalent to the `Convert` call behind it:

```csharp
converter.MarkdownToHtml(markdown);
converter.MarkdownToText(markdown);
converter.HtmlToMarkdown(html);
converter.HtmlToText(html);
converter.TextToHtml(text);
converter.TextToMarkdown(text);
```

Content is returned unchanged when the two formats match, and `null` in gives `null` out — the return is annotated `NotNullIfNotNull`, so a non-null argument gives a non-null result without a null-forgiving operator at the call site.

### What conversion does

Conversion is structural, not cosmetic. An element with no equivalent in the target format has its markup removed and its content kept, so the result is a document in that format rather than one format wrapped inside another.

HTML is the hub: Markdown reaches plain text through it, so plain-text output reads the same whichever format it came from.

| From → to | Behaviour |
|-----------|-----------|
| Markdown → HTML | Rendered by Markdig. Raw HTML is stripped unless `AllowRawHtml` is on |
| HTML → Markdown | Headings, emphasis, links, images, lists, blockquotes, code blocks and tables map onto Markdown syntax; anything else loses its tag and keeps its content |
| HTML → plain text | Blocks are separated, list items keep their marker, tables become tab-separated rows, an image becomes its `alt` text |
| Markdown → plain text | Converted to HTML first, then to text |
| Plain text → HTML | Encoded, then blank lines become paragraphs and single breaks become `<br />` |
| Plain text → Markdown | Escaped so it renders exactly as written |

`script`, `style`, `noscript`, `template`, `head`, `title`, `meta` and `link` are dropped with their contents in both HTML directions — their text is code or metadata rather than prose.

Code blocks keep their language when the conventional `language-x` class is present, fences grow to outrun any backticks inside them, and link destinations containing spaces or brackets are wrapped in angle brackets so they do not end the link early.

### Nuances and gotchas

**Plain text to markup is escaping, not interpretation.** Plain text carries no structure to recover — "Important Information" could be a heading, a title or a sentence — so nothing guesses. `TextToMarkdown` escapes the text so Markdown renders it as written, and `TextToHtml` encodes it.

**`TextToHtml` encodes before it does anything else, and that is not optional.** Text containing `<script>` is text; replacing its line breaks without encoding would turn it into markup.

**Raw HTML inside Markdown is stripped by default.** Markdown permits it, so a document from an untrusted author can carry a script tag through unaltered. Turning on `AllowRawHtml` removes that protection and makes [sanitisation](#html-sanitisation) mandatory before the output is rendered.

**Without `GithubFlavoured`, a table has no syntax to become.** An HTML table converted to Markdown collapses to its cell text, one row per line.

**Pipe tables cannot express merged or nested cells.** A `colspan` becomes empty cells beside the content and an inner table flattens to the text of its own cells — that is as close as the syntax reaches.

**Round-tripping is not lossless.** HTML → Markdown → HTML returns a document with everything that has no Markdown equivalent stripped. Convert once, in the direction you need, and store the result.

**Blank lines matter to plain-text input.** `TextToHtml` starts a new paragraph on a blank line and a `<br />` on a single one, so content that arrived with its line endings mangled produces one long paragraph. Normalise first.

## HTML sanitisation

### Basic usage

Sanitise on the way in, when the value is saved:

```csharp
using JC.Content.Helpers;

public class ArticleService(IArticleStore store)
{
    public async Task SaveAsync(ArticleInput input)
    {
        var article = new Article
        {
            Title = input.Title,
            Body = ContentSanitiser.SanitiseContent(input.Body)
        };

        await store.SaveAsync(article);
    }
}
```

The stored value is then trustworthy for every reader, including other applications sharing the database, instead of each render site having to remember. That is what keeps `@Html.Raw` honest.

### Tailoring a policy

Where one policy is used repeatedly, construct a sanitiser once and hold it:

```csharp
using JC.Content.Helpers;
using JC.Content.Models.Options;

public class CommentService
{
    // Comment-sized policy: inline formatting, lists, quotes and links
    private static readonly ContentSanitiser Sanitiser = new(ContentSanitiserOptions.Basic());

    public string? Clean(string? comment) => Sanitiser.Sanitise(comment);
}
```

Start from a preset and adjust rather than building an allowlist by hand:

```csharp
// Rich text, minus inline images
var sanitiser = new ContentSanitiser(options =>
{
    options.AllowInlineImages = false;
    options.AllowedTags.Remove("img");
});

// Rich text, restricted to a known set of classes
var themed = new ContentSanitiser(options =>
{
    options.AllowedClasses.Add("callout");
    options.AllowedClasses.Add("callout-warning");
});
```

The presets and every option are in [Setup](Setup.md#contentsanitiseroptions).

### Nuances and gotchas

**Treat this as the only XSS control on that content.** An editor's own sanitiser and paste-cleanup settings run in the browser, and the value normally reaches the server through an ordinary form field — so anything holding a valid antiforgery token can post straight past them. Editors exposing a source-code view make arbitrary markup an expected input, not an exotic attack.

**Whitespace-only content returns `null`, not an empty string.** A visually empty editor stores "no content" rather than stray markup, so a publish guard downstream still reads it as unpublishable. Code expecting `string.Empty` will see `null`.

**An empty `AllowedClasses` allows every class.** It is a restriction list, not an allowlist that starts closed — populate it only when you intend to narrow classes to a known set. This is the one option where empty means "everything".

**`KeepChildNodes` is on by default, so a disallowed wrapper loses its tags but not its text.** Set it to `false` where an unrecognised element should take its contents with it.

**Event handlers are removed whatever `AllowedAttributes` says.** You cannot allow `onclick` back in.

**Adding `data` to `AllowedSchemes` yourself is not narrowed to images.** `AllowInlineImages` adds the scheme *and* restricts it to `data:image/*` on `img` elements; listing it manually allows `data:text/html` on a link, which executes script.

**A fresh `HtmlSanitizer` is built for every call.** The library documents no thread-safety guarantee and the options are mutable, so a shared instance could be reconfigured mid-sanitise. Content saves are rare enough for that to cost nothing — but it is not the right tool for sanitising thousands of rows in a tight loop.

## Normalisation

### Basic usage

`NormalisationHelper` is static and needs no registration. `Normalise` is the safe pass — it cleans content up without changing what it says:

```csharp
using JC.Content.Helpers;

var clean = NormalisationHelper.Normalise(input);
```

That single call removes lone surrogates, applies Unicode composition, strips invisible and direction-override characters, makes line endings consistent, trims trailing whitespace from every line, and trims the content as a whole. Nothing in it alters wording, spacing within a line, or paragraph structure.

### The opt-in cleanups

Everything more destructive is a separate call, because each changes what was written:

```csharp
var text = NormalisationHelper.Normalise(input);

text = NormalisationHelper.CollapseWhitespace(text);         // runs of spaces and tabs -> one space
text = NormalisationHelper.CollapseBlankLines(text, 1);      // runs of blank lines -> at most one
text = NormalisationHelper.NormaliseQuotes(text);            // curly quotes -> straight
text = NormalisationHelper.NormaliseDashes(text);            // en, em and minus -> hyphen
text = NormalisationHelper.RemoveDiacritics(text);           // 'cafe' from the accented form
```

The individual pieces of the safe pass are public too, for content that needs one of them and not the others:

```csharp
NormalisationHelper.NormaliseUnicode(value);                       // NFC
NormalisationHelper.NormaliseUnicode(value, compatibility: true);  // NFKC
NormalisationHelper.RemoveLoneSurrogates(value);
NormalisationHelper.RemoveInvisibleCharacters(value);
NormalisationHelper.NormaliseLineEndings(value, "\r\n");
NormalisationHelper.TrimLineEnds(value);
```

A search key wants more folding than content you intend to keep:

```csharp
// Stored as the author wrote it
var body = NormalisationHelper.Normalise(input);

// Indexed for searching: compatibility forms folded, accents stripped, case ignored elsewhere
var key = NormalisationHelper.RemoveDiacritics(
    NormalisationHelper.NormaliseUnicode(input, compatibility: true));
```

### Nuances and gotchas

**Every method returns `null` only for `null` input.** Empty and whitespace strings come back as themselves, so a null check on the result tells you nothing about whether there was any content.

**`Normalise` trims the whole string.** Leading and trailing whitespace is gone, not just trailing whitespace per line. Content where the indentation of the first line matters needs the individual methods instead.

**NFKC is for keys, not for content you keep.** Compatibility folding turns the 'fi' ligature into two letters and '①' into '1'. That is what you want for a search index and wrong for preserving what someone wrote.

**Joiners are kept unless you ask for them.** `RemoveInvisibleCharacters` leaves U+200C and U+200D alone, because they are required in Arabic, Persian and Indic scripts and join emoji sequences. Pass `removeJoiners: true` only where the content is known to be plain Latin text.

**`CollapseBlankLines` and `TrimLineEnds` split on the line ending you give them.** Both default to `"\n"`. Run them after `NormaliseLineEndings` and pass the same value, or they silently do nothing:

```csharp
var text = NormalisationHelper.NormaliseLineEndings(input, "\r\n");
text = NormalisationHelper.CollapseBlankLines(text, maxBlankLines: 1, lineEnding: "\r\n");
```

**`CollapseWhitespace` is destructive where spacing carries meaning.** Aligned tables and indented code do not survive it.

**This is not the profanity canonicaliser.** Moderation folds content much harder — leetspeak, homoglyphs, masking characters — but that output is lossy, internal, and only ever matched against. Normalisation results are meant to be kept.

## The content pipeline

`ContentManager` runs the stages above in one call, in a fixed order: normalise, then moderate, then convert or compare.

### Basic usage

```csharp
using JC.Content.Services;

public class ArticleService(ContentManager content)
{
    public string? Clean(string? body)
    {
        var response = content.NormaliseAndModerate(body);

        // response.OriginalContent — exactly what was passed in
        // response.ProfanityModerationMaskResult.UpdatedContent — normalised and moderated
        return response.ProfanityModerationMaskResult.UpdatedContent;
    }
}
```

All three methods take an optional settings object and fall back to defaults when it is omitted.

### Configuring a call

`ManagerSettings` carries the two stages every method runs:

```csharp
using JC.Content.Models;
using JC.Content.Moderation.Enums;

var settings = new ManagerSettings
{
    NormalisationSettings = new NormalisationSettings
    {
        CollapseWhitespace = true,
        CollapseBlankLines = true,
        MaxBlankLines = 1,
        NormaliseQuotes = true,
        NormaliseDashes = true
    },
    ProfanitySettings = new ProfanitySettings(
        ProfanitySettings.ProfanityMaskType.Tag,
        ProfanityLevel.Strict)
};

var response = content.NormaliseAndModerate(body, settings);
```

`NormalisationSettings` switches on the opt-in cleanups; the safe pass always runs.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Compatibility` | `bool` | `false` | Use NFKC rather than NFC |
| `LineEnding` | `string` | `"\n"` | What line endings become, and what the blank-line collapse splits on |
| `CollapseWhitespace` | `bool` | `false` | Runs of spaces and tabs become one space |
| `CollapseBlankLines` | `bool` | `false` | Runs of blank lines are reduced |
| `MaxBlankLines` | `int` | `1` | The most consecutive blank lines to leave |
| `NormaliseQuotes` | `bool` | `false` | Typographic quotes become straight ones |
| `NormaliseDashes` | `bool` | `false` | En dashes, em dashes and the minus sign become hyphens |
| `RemoveDiacritics` | `bool` | `false` | Accents are stripped |

`ProfanitySettings` chooses how matches are rewritten. Its properties are read-only and set through methods, so a settings object cannot end up describing two rewriting modes at once:

```csharp
var profanity = new ProfanitySettings();          // Mask, four asterisks, registered level

profanity.SetToMask('#', cappedMaskLength: null); // full-length runs of '#'
profanity.SetToRemove();                          // strip matches entirely
profanity.SetToTag("[{severity}]");               // replace with a template
profanity.ChangeProfanityLevel(ProfanityLevel.SuperStrict);
```

Each `SetTo*` call clears the settings the other modes use, so switching mode never leaves a stale mask character or tag format behind.

### Converting through the pipeline

```csharp
using JC.Content.Conversion.Enums;
using JC.Content.Models;

var settings = new ManagerConvertSettings();      // Markdown -> HTML by default

if (!settings.ChangeFormats(ContentFormat.Html, ContentFormat.PlainText))
    throw new InvalidOperationException("Unsupported format pair.");

var response = content.NormaliseModerateAndConvert(html, settings);

// response.ConvertedContent — the finished content in the target format
```

`ChangeFormats` returns `false` and changes nothing when the pair would be invalid — the same format on both sides, or a new value that collides with the one being kept. Both formats are read-only otherwise, so the settings object cannot describe a conversion that does not exist.

**The order of the two stages depends on the target format.** When the target is HTML, moderation runs on the source before conversion; otherwise conversion runs first and moderation works on the output. Either way HTML is never the text being scanned, because masking inside a tag, attribute or URL would corrupt the markup.

That has one visible consequence: `ProfanityModerationMaskResult` — and every `Index` on the matches inside it — describes the source content when the target is HTML, and the converted output otherwise.

### Comparing through the pipeline

Both versions are normalised and moderated with the same settings before the comparison runs:

```csharp
using JC.Content.Comparison.Enums;
using JC.Content.Models;

var settings = new ManagerCompareSettings
{
    GranularityOverride = ComparisonGranularity.Line
};

var response = content.NormaliseModerateAndCompare(published, draft, settings);

// response.OriginalContent                     — the first version, as supplied
// response.OriginalComparedContent             — the second version, as supplied
// response.ProfanityModerationMaskResult       — moderation of the first
// response.ComparedProfanityModerationMaskResult — moderation of the second
// response.ContentComparisonResult             — the diff of the two moderated versions
```

### Nuances and gotchas

**The response's `OriginalContent` and the mask result's `OriginalContent` are different things.** The response holds what you passed in; the mask result holds the *normalised* text it moderated. So `WasModified` compares moderated against normalised, and reads `false` when normalisation was the only thing that changed the content:

```csharp
var response = content.NormaliseAndModerate("  spaced  out  ", settings);

// response.OriginalContent                                  == "  spaced  out  "
// response.ProfanityModerationMaskResult.OriginalContent    == "spaced out"
// response.ProfanityModerationMaskResult.WasModified        == false
```

Compare the response's `OriginalContent` against `UpdatedContent` yourself if you need to know whether the pipeline changed anything at all.

**The comparison runs on the moderated text, so masking can hide a change.** Two different terms rewritten to the same run — `****`, or the same `[Removed]` tag — read as `Unchanged` in the diff even though the content genuinely changed. Where the diff must be accurate rather than displayable, compare the raw versions with `ContentComparer` directly and moderate separately.

**`ChangeFormats` fails silently if you ignore its return value.** It reports a rejected pair rather than throwing, and the settings object keeps its previous formats, so the conversion still runs — just not the one you asked for.

**The pipeline does not sanitise.** Converting untrusted Markdown to HTML with `AllowRawHtml` on produces markup that still needs `ContentSanitiser` before it is rendered. `ContentManager` has no sanitising stage; call it yourself on the output.

**A mask or tag is inserted verbatim into whatever is being moderated.** The default `*` is emphasis syntax in Markdown, so masking a Markdown document writes literal asterisk runs into it. Choose a mask character that is not syntax in the format you are moderating, or use `SetToRemove` or a tag.

**Settings objects are per call, not per registration.** Nothing is cached between calls, so a settings instance can be built once and reused — but treat it as immutable once shared, since the `SetTo*` methods mutate in place.
