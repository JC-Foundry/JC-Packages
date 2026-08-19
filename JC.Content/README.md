# JC.Content

Tools for working with content itself — profanity moderation, diffing, conversion between plain text, Markdown and HTML, HTML sanitisation, and Unicode normalisation. It knows nothing about where the content came from or where it is going: no database, no ASP.NET Core dependency and no configuration file, so it behaves identically in a request, a background job and a console application.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Content/JC.Content.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK
- Nothing else — no `DbContext`, no migrations, no middleware and no `appsettings.json` keys

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddContentManager();
```

That registers all three feature areas and the pipeline that composes them. `AddContentModeration`, `AddContentComparison` and `AddContentConversion` are available individually where an application needs one and not the others.

### Using it

```csharp
public class ArticleService(ContentManager content)
{
    public string? Clean(string? body)
        => content.NormaliseAndModerate(body).ProfanityModerationMaskResult.UpdatedContent;
}
```

`ContentSanitiser` and `NormalisationHelper` are static or directly constructed and need no registration at all.

## Feature areas

### Moderation

```csharp
var result = masker.AnalyseAndMask(comment);   // "what a gobshite" -> "what a ****"
```

Detection survives the things people actually do to get past a filter: case, accents, repeated letters, punctuation between the letters, leetspeak, masking characters, Cyrillic and Greek look-alikes, and Latin letters standing in for one another. Every fold is recorded and priced, so a match is reported with a confidence percentage rather than a yes or no — and the folds that could fabricate a match from innocent text are the only ones that cost anything.

`ProfanityModerator` reports and nothing else; `ProfanityMasker` masks, strips or tags what it found. **Nothing is rewritten unless the content breaches the level in force**, and only the matches that counted are touched.

Terms come from a curated set and a bundled third-party list, and an application can add its own, drop ones it disagrees with, or allow words that produce false positives — at registration or at runtime.

### Comparison

```csharp
var diff = comparer.Compare(published, draft);

var html = diff.Render(
    addedOpen: "<ins>", addedClose: "</ins>",
    removedOpen: "<del>", removedClose: "</del>",
    encode: value => WebUtility.HtmlEncode(value));
```

Line, word or character granularity, chosen at registration or per call. The splitting is lossless — lines keep their terminators, words keep their trailing whitespace, characters stay whole through surrogate pairs and combining marks — so concatenating the segments reconstructs either version exactly, and every segment carries its index into both.

Content is compared exactly as supplied. Nothing is normalised or case-folded on the way in, so a change of line ending is a change.

### Conversion

```csharp
var html = converter.MarkdownToHtml(markdown);
var text = converter.HtmlToText(html);
```

HTML is the hub, so plain-text output reads the same whichever format it came from. Conversion is structural rather than cosmetic: an element with no equivalent in the target format loses its markup and keeps its content, so the result is a document in that format rather than one format wrapped inside another.

Raw HTML inside Markdown is stripped by default, at the parser, rather than relying on a later sanitise.

### HTML sanitisation

```csharp
article.Body = ContentSanitiser.SanitiseContent(input.Body);
```

Allowlist-based sanitisation for editor output, with three presets — `RichText`, `Basic` and `Empty` — and `data:` URIs narrowed to images on `img` elements rather than allowed outright.

Treat it as the only XSS control on that content: an editor's own sanitiser runs in the browser, and the value reaches the server through an ordinary form field. Sanitise on write, so the stored value is trustworthy for every reader.

### Normalisation

```csharp
var clean = NormalisationHelper.Normalise(input);
```

The safe pass removes lone surrogates and invisible characters, applies Unicode composition, makes line endings consistent and trims. It does not touch wording, spacing within a line, or paragraph structure — collapsing whitespace, folding quotes and dashes, and stripping accents are separate calls, because each changes what was written.

### The pipeline

```csharp
var response = content.NormaliseModerateAndConvert(markdown, settings);
```

`ContentManager` runs normalisation, then moderation, then conversion or comparison, in one call. Where a conversion involves HTML, the order of the middle two stages follows the target format so that HTML is never the text being scanned — masking inside a tag, attribute or URL would corrupt the markup.

## Defaults

| Default | Value |
|---------|-------|
| Registration | One call — `AddContentManager()` |
| Service lifetimes | Singleton throughout |
| Profanity level | `Safe` — Medium severity at Medium confidence |
| Bundled term list | Loaded, alongside the curated terms |
| Rewriting | Masked with four asterisks, and only where the content breached the level |
| Inside-word matches | Reported, capped in Low confidence, and unable to block at any level |
| Comparison granularity | `Word` |
| Content ceilings | **None** — moderation and comparison read everything given to them |
| Markdown flavour | GitHub-flavoured, with raw HTML stripped |
| Sanitiser policy | `RichText` — and the sanitiser is not registered in DI |
| Configuration | None. No configuration keys, no database, no middleware |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Content/Setup.md) — registration, every option, term configuration, sanitiser policies
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Content/Guide.md) — moderating, comparing, converting, sanitising and normalising, and the pipeline
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Content/API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
