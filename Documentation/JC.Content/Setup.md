# JC.Content — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- No database, `DbContext`, middleware or configuration file — JC.Content works on content in memory and reads nothing from `IConfiguration`
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Content`:

```xml
<ProjectReference Include="path/to/JC.Content/JC.Content.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Services — `Program.cs`

```csharp
using JC.Content.Extensions;

builder.Services.AddContentManager();
```

That single call registers all three feature areas — moderation, comparison and conversion — and the `ContentManager` that runs them as a pipeline. Inject `ContentManager` wherever content passes through:

```csharp
using JC.Content.Services;

public class ArticleService(ContentManager content)
{
    public string? Clean(string? body)
        => content.NormaliseAndModerate(body).ProfanityModerationMaskResult.UpdatedContent;
}
```

The individual services are also resolvable on their own — `ProfanityModerator`, `ProfanityMasker`, `ContentComparer` and `ContentConverter` — for code that needs one feature rather than the pipeline.

### Two things that need no registration

`ContentSanitiser` and `NormalisationHelper` are deliberately outside DI and are not touched by `AddContentManager`:

```csharp
using JC.Content.Helpers;

// HTML sanitisation — construct where you need it, or call the static shorthand
var clean = ContentSanitiser.SanitiseContent(model.Body);

// Normalisation — static throughout
var tidy = NormalisationHelper.Normalise(input);
```

`ContentSanitiser` is covered under [full configuration](#contentsanitiser--configured-per-instance) because it has options of its own.

### Configuration — `appsettings.json`

None. Every setting is supplied in code at registration; JC.Content never reads `IConfiguration`.

### Defaults

Called with no arguments, `AddContentManager` registers:

| Registration | Lifetime | Description |
|--------------|----------|-------------|
| `ProfanityModerationOptions` | Singleton | The configured instance itself, not `IOptions<T>` |
| `ProfanityTermRegistry` | Singleton | The active term set and allowlist, seeded on first resolve |
| `ProfanityModerator` | Singleton | Reports what moderation found; alters nothing |
| `ProfanityMasker` | Singleton | Rewrites matches — masked, removed or tagged |
| `ContentComparisonOptions` | Singleton | The configured instance itself |
| `ContentComparer` | Singleton | Reports how two versions of content differ |
| `ContentConversionOptions` | Singleton | The configured instance itself |
| `ContentConverter` | Singleton | Converts between plain text, Markdown and HTML |
| `ContentManager` | Singleton | Normalise → moderate → convert or compare, as one call |

Everything is a singleton: the matcher holds a prepared index over the term set, the Markdown pipeline is built once, and nothing holds per-call state.

Default option values:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ProfanityModerationOptions.Level` | `ProfanityLevel` | `Safe` | Blocks Medium severity and above at Medium confidence and above |
| `ProfanityModerationOptions.ContextCharacters` | `int` | `5` | Characters of surrounding text kept either side of a reported match |
| `ProfanityModerationOptions.MatchInsideWords` | `bool` | `true` | Reports terms found inside longer words; these never block |
| `ProfanityModerationOptions.MatchAcrossWordBreaks` | `bool` | `false` | Does not step over whitespace to reach a term |
| `ProfanityModerationOptions.MaxContentLength` | `int` | `0` | No ceiling — all content is scanned |
| `ProfanityModerationOptions.MediumConfidenceMinimum` | `ushort` | `40` | Floor of the Medium confidence band |
| `ProfanityModerationOptions.HighConfidenceMinimum` | `ushort` | `70` | Floor of the High confidence band |
| `ContentComparisonOptions.Granularity` | `ComparisonGranularity` | `Word` | Compares word by word |
| `ContentComparisonOptions.MaxContentLength` | `int` | `0` | No ceiling — both versions are compared in full |
| `ContentConversionOptions.GithubFlavoured` | `bool` | `true` | Pipe tables, strikethrough, task lists and autolinks |
| `ContentConversionOptions.AllowRawHtml` | `bool` | `false` | Raw HTML inside Markdown is stripped rather than passed through |
| `ContentConversionOptions.IncludeLinkUrlsInText` | `bool` | `false` | Plain-text output keeps link text alone, without the destination |
| `includeImportedProfanityTerms` | `bool` | `true` | The bundled third-party term list loads alongside the curated one |

Three behaviours follow from the defaults and are worth knowing before you configure anything:

- **The term registry is seeded on first resolve, not at registration.** It is registered as a factory, so the bundled list is read and mapped the first time something asks for moderation. A `configureTerms` callback runs immediately after seeding, inside that same factory.
- **Options are registered as their own type.** Inject `ProfanityModerationOptions`, not `IOptions<ProfanityModerationOptions>`. This differs from the rest of the suite.
- **Everything registers with `TryAdd`, so the first registration wins.** See [registration order](#registration-order) for what that means if you call more than one of these methods.

## 2. Full configuration

### AddContentManager

The single entry point. Composes the three feature registrations and adds the pipeline on top.

```csharp
using JC.Content.Comparison.Enums;
using JC.Content.Extensions;
using JC.Content.Moderation.Enums;

builder.Services.AddContentManager(
    configureProfanityOptions: options =>
    {
        options.Level = ProfanityLevel.Safe;
        options.ContextCharacters = 5;
        options.MatchInsideWords = true;
        options.MatchAcrossWordBreaks = false;
        options.MaxContentLength = 0;
        options.MediumConfidenceMinimum = 40;
        options.HighConfidenceMinimum = 70;
    },
    includeImportedProfanityTerms: true,
    configureProfanityTerms: registry =>
    {
        registry.Allow("scunthorpe");
    },
    configureComparisonOptions: options =>
    {
        options.Granularity = ComparisonGranularity.Word;
        options.MaxContentLength = 0;
    },
    configureConversionOptions: options =>
    {
        options.GithubFlavoured = true;
        options.AllowRawHtml = false;
        options.IncludeLinkUrlsInText = false;
    });
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureProfanityOptions` | `Action<ProfanityModerationOptions>?` | `null` | Passed through to `AddContentModeration` |
| `includeImportedProfanityTerms` | `bool` | `true` | Passed through to `AddContentModeration` |
| `configureProfanityTerms` | `Action<ProfanityTermRegistry>?` | `null` | Passed through to `AddContentModeration` |
| `configureComparisonOptions` | `Action<ContentComparisonOptions>?` | `null` | Passed through to `AddContentComparison` |
| `configureConversionOptions` | `Action<ContentConversionOptions>?` | `null` | Passed through to `AddContentConversion` |

All five parameters are optional, so `AddContentManager()` on its own is valid and gives the defaults above.

### AddContentModeration

Registers profanity detection and rewriting. Call this instead of `AddContentManager` when the application needs moderation but not conversion or comparison.

```csharp
using JC.Content.Extensions;
using JC.Content.Moderation.Enums;

builder.Services.AddContentModeration(
    configureOptions: options =>
    {
        options.Level = ProfanityLevel.Safe;
        options.ContextCharacters = 5;
        options.MatchInsideWords = true;
        options.MatchAcrossWordBreaks = false;
        options.MaxContentLength = 0;
        options.MediumConfidenceMinimum = 40;
        options.HighConfidenceMinimum = 70;
    },
    includeImportedTerms: true,
    configureTerms: registry =>
    {
        registry.Allow("scunthorpe");
    });
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureOptions` | `Action<ProfanityModerationOptions>?` | `null` | Configures `ProfanityModerationOptions`. Validated immediately — a bad value throws at registration, not at first use |
| `includeImportedTerms` | `bool` | `true` | Whether the bundled third-party list loads alongside the curated terms |
| `configureTerms` | `Action<ProfanityTermRegistry>?` | `null` | Runs against the seeded registry on first resolve, so application terms take precedence over both bundled sources |

Registers `ProfanityModerationOptions`, `ProfanityTermRegistry`, `ProfanityModerator` and `ProfanityMasker`, all as singletons.

#### ProfanityModerationOptions

**Namespace:** `JC.Content.Moderation.Models.Options`

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Level` | `ProfanityLevel` | `Safe` | get; set; | The level applied when a call does not name one. Governs the block decision only — detection and reporting are identical at every level |
| `ContextCharacters` | `int` | `5` | get; set; | Characters of surrounding text kept either side of a match, for judging a false positive from a log entry. Zero reports the matched text alone |
| `MatchInsideWords` | `bool` | `true` | get; set; | Whether to look for terms inside longer words. Those matches are capped in the Low band and can never block, but they are reported, which is how deliberate padding shows up. Turning this off drops them entirely and makes matching cheaper |
| `MatchAcrossWordBreaks` | `bool` | `false` | get; set; | Whether to step over whitespace to reach a term. Off by default, because the same step joins words that were never one. Capped in Low either way |
| `MaxContentLength` | `int` | `0` | get; set; | The most characters to scan, or zero for no limit. Content past the limit is neither examined nor returned, and the result's `Truncated` flag reports it |
| `MediumConfidenceMinimum` | `ushort` | `40` | get; set; | Floor of the Medium confidence band. Also the ceiling that holds structurally unreliable matches in Low |
| `HighConfidenceMinimum` | `ushort` | `70` | get; set; | Floor of the High confidence band |

Validation runs at registration and throws `ArgumentOutOfRangeException` when `ContextCharacters` is negative, `MaxContentLength` is negative, `MediumConfidenceMinimum` is zero, `HighConfidenceMinimum` is not above `MediumConfidenceMinimum`, or `HighConfidenceMinimum` exceeds 100.

#### Choosing a level

`Level` sets the floors a match must reach to count towards the block decision. Nothing below Medium confidence blocks at any level.

| Level | Minimum severity | Minimum confidence |
|-------|------------------|--------------------|
| `Minimal` | `High` | `High` |
| `Lax` | `High` | `Medium` |
| `Safe` | `Medium` | `Medium` |
| `Strict` | `Low` | `Medium` |
| `SuperStrict` | `Mild` | `Medium` |

The level is a default only — every moderation call can override it, which is how a username can be held to a stricter standard than a private message without a second registration.

#### Confidence bands

Confidence is scored as a percentage and banded using the two floors above. With the defaults:

| Band | Score | Meaning |
|------|-------|---------|
| `None` | `0` | Nothing found, or the match was suppressed by the allowlist |
| `Low` | `1`–`39` | Reached only through heavy substitution, or found inside a longer word or across a word break |
| `Medium` | `40`–`69` | Some substitution, but the term is clearly present |
| `High` | `70`–`99` | Little or no substitution |
| `Certain` | `100` | The term as written, allowing for case and accents |

Case folding, accent stripping, homoglyph folding, repeated letters and punctuation inside a word cost nothing, because none of them can invent a match. Leetspeak, masking characters and look-alike Latin letters are charged in proportion to how much of the term they account for. Matches found inside a longer word or spanning a word break are capped below `MediumConfidenceMinimum`, which is what guarantees they can never block whatever level is in force.

#### The bundled term list

Two sources seed the registry:

| Source | Origin | Loaded when |
|--------|--------|-------------|
| `ProfanityTermSource.BuiltIn` | Curated by JC.Content — slurs promoted from the bundled file, plus British slang and inflections it has no entry for | Always |
| `ProfanityTermSource.Imported` | The bundled third-party list, mapped onto our severities and categories | Only when `includeImportedTerms` is `true` |

```csharp
builder.Services.AddContentModeration(includeImportedTerms: false);
```

Setting this to `false` trades coverage for accuracy: the broad imported set is dropped, and what remains is the curated terms only. The promoted slurs survive either way — they are re-sourced to `BuiltIn` during the import precisely so that dropping the imported set to escape false positives does not also drop them.

#### Configuring terms

`configureTerms` receives the seeded `ProfanityTermRegistry`, which is where an application adds its own terms, drops ones it disagrees with, or forgives words that were producing false positives.

```csharp
using JC.Content.Extensions;
using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Models;

builder.Services.AddContentModeration(configureTerms: registry =>
{
    // Add a term the bundled lists do not carry
    registry.TryAddTerm(new ProfanityTerm(
        id: "competitor-slur",
        matches: ["badword", "badwords"],
        severity: ProfanitySeverity.Medium,
        category: ProfanityCategory.Custom,
        exceptions: ["badwordsmith"],
        wholeWordOnly: true));

    // Drop a term outright, so it is never looked for
    registry.TryRemoveTerm("git");

    // Forgive a word that contains a term but is not one
    registry.Allow("scunthorpe");
    registry.Allow(["cockburn", "penistone"]);
});
```

| Method | Returns | Description |
|--------|---------|-------------|
| `TryAddTerm(ProfanityTerm term)` | `bool` | Registers a term, replacing one of the same id from an equal or lower-precedence source. `false` when an existing term outranks it |
| `AddTerms(IEnumerable<ProfanityTerm> terms)` | `int` | Registers several, returning how many were taken |
| `TryRemoveTerm(string id)` | `bool` | Removes a term entirely, so it is never looked for |
| `RemoveTerms(ProfanityTermSource source)` | `int` | Removes every term from one source, leaving the others in place |
| `ClearTerms()` | `void` | Empties the term set, including everything seeded at startup |
| `Allow(string word)` | `bool` | Adds a word or phrase that suppresses any match falling inside it |
| `Allow(IEnumerable<string> words)` | `int` | Adds several, returning how many were taken |
| `Disallow(string word)` | `bool` | Removes an allowlist entry |
| `ClearAllowed()` | `void` | Empties the allowlist |
| `TryGetTerm(string id, out ProfanityTerm? term)` | `bool` | Looks a term up by id |
| `GetTerms()` | `IReadOnlyList<ProfanityTerm>` | Every registered term, in no particular order |
| `GetTerms(ProfanityTermSource source)` | `IReadOnlyList<ProfanityTerm>` | Every term from one source |
| `GetAllowed()` | `IReadOnlyCollection<string>` | Every allowlist entry |
| `Count` | `int` | How many terms are registered |
| `Version` | `int` | Increments on every change; a matcher compares it to know whether its index is stale |

**Removing a term and allowing a word are different things.** `TryRemoveTerm` drops the term so it is never looked for at all; `Allow` keeps the term but forgives the specific whole word the match landed inside. An allowed match is still reported, at zero confidence, so an application tuning its allowlist can see it working.

Terms are keyed by id, and where two sources supply the same id the higher-precedence one wins:

| Precedence | Source |
|------------|--------|
| Highest | `ProfanityTermSource.Configured` |
| | `ProfanityTermSource.BuiltIn` |
| Lowest | `ProfanityTermSource.Imported` |

Because `ProfanityTerm` defaults its `source` parameter to `Configured`, a term added through `configureTerms` restates a bundled term of the same id rather than being rejected by it.

#### ProfanityTerm

**Namespace:** `JC.Content.Moderation.Models`

```csharp
new ProfanityTerm(
    string id,
    IEnumerable<string> matches,
    ProfanitySeverity severity,
    ProfanityCategory category,
    ProfanityTermSource source = ProfanityTermSource.Configured,
    IEnumerable<string>? exceptions = null,
    bool wholeWordOnly = false,
    int? sourceSeverity = null)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | Stable identifier, and the key the registry holds the term under |
| `matches` | `IEnumerable<string>` | — | The spellings that match this term. Lower-cased, trimmed and de-duplicated on the way in |
| `severity` | `ProfanitySeverity` | — | `Mild`, `Low`, `Medium`, `High` or `Severe`. `None` is rejected |
| `category` | `ProfanityCategory` | — | `General`, `Sexual`, `Racial`, `Sexuality`, `Religious`, `Shock`, `Custom` or `None` |
| `source` | `ProfanityTermSource` | `Configured` | Who supplied the term, which decides precedence |
| `exceptions` | `IEnumerable<string>?` | `null` | Whole words that contain a spelling but are not this term. A match falling inside one is reported at zero confidence rather than counted |
| `wholeWordOnly` | `bool` | `false` | Stops reporting the term when it turns up inside a longer word. Only silences noise — an inside-word match could never block anyway |
| `sourceSeverity` | `int?` | `null` | The severity the imported list gave the term before mapping. Only set by the importer |

Throws `ArgumentException` when `id` is null or whitespace, when `matches` yields no usable spelling, or when `severity` is `None`.

### AddContentComparison

Registers the diff engine on its own.

```csharp
using JC.Content.Comparison.Enums;
using JC.Content.Extensions;

builder.Services.AddContentComparison(options =>
{
    options.Granularity = ComparisonGranularity.Word;
    options.MaxContentLength = 0;
});
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureOptions` | `Action<ContentComparisonOptions>?` | `null` | Configures `ContentComparisonOptions`. Validated immediately |

Registers `ContentComparisonOptions` and `ContentComparer`, both as singletons.

#### ContentComparisonOptions

**Namespace:** `JC.Content.Comparison.Models.Options`

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Granularity` | `ComparisonGranularity` | `Word` | get; set; | The unit applied when a call does not name one |
| `MaxContentLength` | `int` | `0` | get; set; | The most characters to compare from either version, or zero for no limit. Content past the limit is neither examined nor returned, and the result's `Truncated` flag reports it |

Validation throws `ArgumentOutOfRangeException` when `MaxContentLength` is negative.

| Granularity | Unit | Suited to |
|-------------|------|-----------|
| `Line` | Whole lines, each carrying its own terminator | Anything structured — configuration, code, logs |
| `Word` | Words, each carrying the whitespace that follows | Prose, and the default |
| `Character` | Characters as a reader sees them, so a surrogate pair or an accented letter stays whole | Short strings where precision matters; noisy on prose |

`MaxContentLength` is worth setting where the content is user-supplied. The cost of a comparison rises with how much the two versions differ, so two long and largely unrelated documents are the expensive case, particularly at `Character`.

### AddContentConversion

Registers format conversion on its own.

```csharp
using JC.Content.Extensions;

builder.Services.AddContentConversion(options =>
{
    options.GithubFlavoured = true;
    options.AllowRawHtml = false;
    options.IncludeLinkUrlsInText = false;
});
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureOptions` | `Action<ContentConversionOptions>?` | `null` | Configures `ContentConversionOptions` |

Registers `ContentConversionOptions` and `ContentConverter`, both as singletons. Unlike the other two, these options have no validation step — every combination of values is legal.

#### ContentConversionOptions

**Namespace:** `JC.Content.Conversion.Models.Options`

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `GithubFlavoured` | `bool` | `true` | get; set; | Whether GitHub-flavoured Markdown is read and written — pipe tables, strikethrough, task lists and autolinks |
| `AllowRawHtml` | `bool` | `false` | get; set; | Whether raw HTML embedded in Markdown survives a conversion to HTML |
| `IncludeLinkUrlsInText` | `bool` | `false` | get; set; | Whether a link's destination follows its text when converting HTML to plain text |

**`AllowRawHtml` is a security setting.** Markdown permits raw HTML, so a document from an untrusted author can carry a script tag through unaltered. Left off, that route is removed at the parser. Turned on, the output has to reach `ContentSanitiser` before anything renders it.

Turning `GithubFlavoured` off leaves no table syntax for an HTML table to convert into, so one becomes its cell text instead.

### ContentSanitiser — configured per instance

`ContentSanitiser` is a standalone helper rather than a registered service, and none of the `Add*` methods touch it. Its options are constructor arguments, so they are documented here alongside the rest of the configuration.

```csharp
using JC.Content.Helpers;
using JC.Content.Models.Options;

// The rich-text policy, without constructing anything
var clean = ContentSanitiser.SanitiseContent(model.Body);

// A comment-sized policy, reused across calls
var sanitiser = new ContentSanitiser(ContentSanitiserOptions.Basic());
var comment = sanitiser.Sanitise(model.Comment);

// Rich text, minus inline images
var noImages = new ContentSanitiser(options =>
{
    options.AllowInlineImages = false;
    options.AllowedTags.Remove("img");
});
```

| Constructor | Policy |
|-------------|--------|
| `ContentSanitiser()` | `ContentSanitiserOptions.RichText()` |
| `ContentSanitiser(ContentSanitiserOptions options)` | Whatever you supply |
| `ContentSanitiser(Action<ContentSanitiserOptions> configure)` | `RichText()` with your adjustments applied |

`Sanitise` and the static `SanitiseContent` both return `null` for content that is null, empty or whitespace, so a visually empty editor stores "no content" rather than stray markup.

**Sanitise on write, not on render.** The stored value is then trustworthy for every reader, including other applications sharing the database, instead of each render site having to remember.

#### ContentSanitiserOptions

**Namespace:** `JC.Content.Models.Options`

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `AllowedTags` | `HashSet<string>` | Empty | get; set; | Element names that survive. Everything else is stripped, subject to `KeepChildNodes` |
| `AllowedAttributes` | `HashSet<string>` | Empty | get; set; | Attribute names that survive on any allowed element. Event handlers are removed regardless of what is listed |
| `AllowedCssProperties` | `HashSet<string>` | Empty | get; set; | CSS property names that survive inside a `style` attribute. Only consulted when `style` is allowed |
| `AllowedSchemes` | `HashSet<string>` | Empty | get; set; | URL schemes that survive in `href`, `src` and other URL attributes. An empty set strips every URL |
| `AllowedClasses` | `HashSet<string>` | Empty | get; set; | Class names that survive. **Empty allows all classes** — populate it only to restrict them |
| `KeepChildNodes` | `bool` | `true` | get; set; | Whether the children of a disallowed element survive when that element is stripped |
| `AllowInlineImages` | `bool` | `false` | get; set; | Whether images inlined as `data:` URIs are kept, narrowed to `data:image/*` on `img` elements |
| `Configure` | `Action<HtmlSanitizer>?` | `null` | get; set; | Escape hatch run against the underlying `HtmlSanitizer` after every other setting, so it can override them |

All five sets are case-insensitive, and every preset returns a fresh instance, so mutating one never affects another.

| Preset | Allows |
|--------|--------|
| `Empty()` | Nothing. Combined with the default `KeepChildNodes` this reduces markup to its text, which makes it a reasonable "strip all HTML" policy as well as a hand-built allowlist's starting point |
| `Basic()` | Inline formatting, lists, quotes and links — what a comment box or short description field needs. No images, tables, styles or classes, so the result cannot carry layout or colour into the page |
| `RichText()` | The full output of a WYSIWYG editor — headings, tables, images, classes and the inline styles used for font, colour and alignment. Inline images are on |

The `data` scheme is added by `AllowInlineImages` rather than listed in `AllowedSchemes`, because allowing it outright would also permit `data:text/html` on a link. Adding `data` to `AllowedSchemes` yourself keeps it allowed and unnarrowed, which is the behaviour you asked for.

### Registration order

Every registration in this package uses `TryAdd`, so **the first registration of a type wins and later ones are ignored**. Two consequences:

```csharp
// The moderation options here are kept; AddContentManager's are discarded
builder.Services.AddContentModeration(o => o.Level = ProfanityLevel.Strict);
builder.Services.AddContentManager(o => o.Level = ProfanityLevel.Lax);
```

Calling a feature method before `AddContentManager` is the supported way to configure one area differently while still getting the pipeline — but it is order-sensitive, and it fails silently. The second callback still runs and its options are still validated; it is the configured instance that is thrown away, with nothing logged. Configuring everything through `AddContentManager` avoids the question entirely.

The same applies to substituting your own implementation: register it before calling `AddContentManager` and yours is kept.

## 3. Verify

1. Run the application and resolve `ContentManager` from DI.
2. Pass content containing a term the curated list carries:

   ```csharp
   var result = content.NormaliseAndModerate("what a gobshite");

   // result.ProfanityModerationMaskResult.UpdatedContent  == "what a ****"
   // result.ProfanityModerationMaskResult.ModerationResult.ShouldBlock == true
   ```

   Four asterisks rather than eight is correct: the default mask is capped at four characters so the length of the original is not disclosed.
3. Convert something, to confirm the conversion area registered:

   ```csharp
   var converted = content.NormaliseModerateAndConvert("**bold**");

   // converted.ConvertedContent contains "<p><strong>bold</strong></p>"
   ```

## Next steps

- [Guide](Guide.md) — moderating, comparing and converting content, and the pipeline that combines them.
- [API Reference](API.md)