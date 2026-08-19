# JC.Content — API reference

Complete reference of all public types, properties, and methods in JC.Content. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Setup.md), not here. That covers `ProfanityModerationOptions`, `ContentComparisonOptions`, `ContentConversionOptions` and `ContentSanitiserOptions` — the last of these is not a registration options class, but it is configuration and is documented alongside the others.

Much of the package is `internal` and therefore absent here: the matcher and canonicaliser behind moderation, the chunkers behind comparison, the format writers behind conversion, and the importer behind the bundled term list.

---

# Models

## ManagerSettings

**Namespace:** `JC.Content.Models`

Settings for a `ContentManager` call. Carries the two stages every pipeline method runs. Supplied per call, never registered.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ProfanitySettings` | `ProfanitySettings` | `new()` | get; set; | How matches are rewritten, and the level to apply. |
| `NormalisationSettings` | `NormalisationSettings` | `new()` | get; set; | Which optional normalisation steps run. |

Both are non-nullable but settable. Assigning `null` to either produces a `NullReferenceException` when the pipeline runs, not at assignment.

---

## ManagerConvertSettings

**Namespace:** `JC.Content.Models`

Sealed. Extends [`ManagerSettings`](#managersettings) with the format pair for `ContentManager.NormaliseModerateAndConvert`. Inherited members are not repeated here.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `SourceFormat` | `ContentFormat` | `Markdown` | get; private set; | The format the content is being read as. |
| `TargetFormat` | `ContentFormat` | `Html` | get; private set; | The format the content is being written to. |

Both setters are private, so the pair can only change through `ChangeFormats` and cannot be left describing a conversion that does not exist.

### Methods

#### ChangeFormats(ContentFormat? sourceFormat = null, ContentFormat? targetFormat = null)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `sourceFormat` | `ContentFormat?` | `null` | The new source format, or `null` to keep the current one. |
| `targetFormat` | `ContentFormat?` | `null` | The new target format, or `null` to keep the current one. |

Validates the requested pair and applies it, returning whether it was applied. Returns `false` without changing anything when neither argument is supplied, when both are supplied and equal, when only the source is supplied and it equals the current target, or when only the target is supplied and it equals the current source. When both are supplied and differ, both are applied and the previous values are irrelevant.

The rejection is reported through the return value rather than an exception, so a caller that ignores it proceeds with the previous format pair still in place.

---

## ManagerCompareSettings

**Namespace:** `JC.Content.Models`

Sealed. Extends [`ManagerSettings`](#managersettings) with the granularity for `ContentManager.NormaliseModerateAndCompare`. Inherited members are not repeated here.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `GranularityOverride` | `ComparisonGranularity?` | `null` | get; set; | Overrides the registered granularity for this call. `null` uses the registered default. |

---

## NormalisationSettings

**Namespace:** `JC.Content.Models`

Sealed. Which optional normalisation steps a `ContentManager` call applies. The safe pass always runs; every property here switches on something beyond it.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Compatibility` | `bool` | `false` | get; set; | Use NFKC rather than NFC, folding compatibility forms. |
| `LineEnding` | `string` | `"\n"` | get; set; | What line endings become. Also passed to the blank-line collapse. |
| `CollapseWhitespace` | `bool` | `false` | get; set; | Reduce runs of spaces and tabs to one space. |
| `CollapseBlankLines` | `bool` | `false` | get; set; | Reduce runs of blank lines to `MaxBlankLines`. |
| `MaxBlankLines` | `int` | `1` | get; set; | The most consecutive blank lines to leave. Only read when `CollapseBlankLines` is `true`. |
| `NormaliseQuotes` | `bool` | `false` | get; set; | Replace typographic quotes with straight ones. |
| `NormaliseDashes` | `bool` | `false` | get; set; | Replace en dashes, em dashes and the minus sign with a hyphen. |
| `RemoveDiacritics` | `bool` | `false` | get; set; | Strip accents, leaving the base letters. |

Steps run in the order listed: whitespace, blank lines, quotes, dashes, diacritics.

---

## ProfanitySettings

**Namespace:** `JC.Content.Models`

Sealed. How a `ContentManager` call rewrites what moderation found. Every property is read-only from outside and set through the `SetTo*` methods, so an instance can never describe two rewriting modes at once.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `MaskType` | `ProfanityMaskType` | `Mask` | get; private set; | Which rewriting mode applies. |
| `LevelOverride` | `ProfanityLevel?` | `null` | get; private set; | Overrides the registered level for this call. |
| `MaskChar` | `char?` | `'*'` | get; private set; | The character a match is filled with. `null` outside `Mask` mode. |
| `CappedMaskLength` | `ushort?` | `4` | get; private set; | The longest run to write. `null` outside `Mask` mode, and `null` within it means the run matches the match length. |
| `TagFormat` | `string?` | `null` | get; private set; | The replacement template. Non-null only in `Tag` mode. |

Defaults shown are those left by the constructor's default `Mask` mode.

### Constructor

#### ProfanitySettings(ProfanityMaskType maskType = ProfanityMaskType.Mask, ProfanityLevel? levelOverride = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `maskType` | `ProfanityMaskType` | `Mask` | The rewriting mode to start in. |
| `levelOverride` | `ProfanityLevel?` | `null` | The level to apply, or `null` for the registered default. |

Calls the `SetTo*` method matching `maskType`, then applies the level. An unrecognised `maskType` value falls through to `Mask`.

### Methods

#### ChangeProfanityLevel(ProfanityLevel? level)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `level` | `ProfanityLevel?` | — | The level to apply, or `null` to fall back to the registered default. |

#### SetToMask(char maskChar = '\*', ushort? cappedMaskLength = 4)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `maskChar` | `char` | `'*'` | The character to fill a match with. |
| `cappedMaskLength` | `ushort?` | `4` | The longest run to write, or `null` for a run the length of the match. |

Switches to `Mask` mode and clears `TagFormat`.

#### SetToRemove()

**Returns:** `void`

Switches to `Remove` mode and clears `MaskChar`, `CappedMaskLength` and `TagFormat`.

#### SetToTag(string tagFormat = ProfanityMasker.GenericTag)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tagFormat` | `string` | `"[Removed]"` | The replacement template. |

Switches to `Tag` mode and clears `MaskChar` and `CappedMaskLength`. The format is not validated here — an empty string is accepted and raises `ArgumentException` later, when `ProfanityMasker.AnalyseAndTag` receives it.

---

## ManagerResponse

**Namespace:** `JC.Content.Models`

What a `ContentManager` call returns. All properties are get-only and set through the constructor.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `OriginalContent` | `string?` | get; | The content exactly as passed to the manager, before normalisation. |
| `ProfanityModerationMaskResult` | `ProfanityModerationMaskResult` | get; | The moderation outcome, whose own `OriginalContent` is the *normalised* text rather than this one. |

The two `OriginalContent` values differ whenever normalisation changed anything, which is why `ProfanityModerationMaskResult.WasModified` reports on moderation alone.

### Constructor

#### ManagerResponse(string? originalContent, ProfanityModerationMaskResult profanityModerationMaskResult)

Constructed by `ContentManager`; there is no reason for application code to build one outside a test.

---

## ManagerConvertResponse

**Namespace:** `JC.Content.Models`

Sealed. Extends [`ManagerResponse`](#managerresponse) with the conversion output. Inherited members are not repeated here.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `ConvertedContent` | `string?` | get; | The finished content in the target format, normalised, moderated and converted. |

### Constructor

#### ManagerConvertResponse(string? originalContent, ProfanityModerationMaskResult profanityModerationMaskResult, string? convertedContent)

---

## ManagerCompareResponse

**Namespace:** `JC.Content.Models`

Sealed. Extends [`ManagerResponse`](#managerresponse) with the second version and the diff. Inherited members are not repeated here.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `OriginalComparedContent` | `string?` | get; | The second version exactly as passed to the manager, before normalisation. |
| `ComparedProfanityModerationMaskResult` | `ProfanityModerationMaskResult` | get; | Moderation of the second version. |
| `ContentComparisonResult` | `ContentComparisonResult` | get; | The difference between the two *moderated* versions. |

The inherited `OriginalContent` and `ProfanityModerationMaskResult` describe the first version. The naming is asymmetric — the first version's raw content has no `Compared` counterpart in its name because it comes from the base class.

### Constructor

#### ManagerCompareResponse(string? originalContent, string? originalComparedContent, ProfanityModerationMaskResult profanityModerationMaskResult, ProfanityModerationMaskResult comparedProfanityModerationMaskResult, ContentComparisonResult contentComparisonResult)

The two raw content values come first, then the two moderation results, then the comparison — so the first and second versions are not adjacent in the parameter list.

---

## ProfanityTerm

**Namespace:** `JC.Content.Moderation.Models`

A single blocked term and the metadata a match reports. One term may have several spellings. All properties are get-only and set through the constructor.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Id` | `string` | get; | Stable identifier, and the key the registry holds the term under. Trimmed on construction. |
| `Matches` | `IReadOnlyList<string>` | get; | The spellings that match this term, lower-cased. Never empty. |
| `Exceptions` | `IReadOnlyList<string>` | get; | Whole words that contain a spelling but are not this term. A match falling inside one is reported at zero confidence rather than counted. |
| `Severity` | `ProfanitySeverity` | get; | How serious the term is. Never `None`. |
| `Category` | `ProfanityCategory` | get; | What kind of term it is. |
| `Source` | `ProfanityTermSource` | get; | Who supplied it, which decides precedence in the registry. |
| `WholeWordOnly` | `bool` | get; | Whether to stop reporting the term when it appears inside a longer word. |
| `SourceSeverity` | `int?` | get; | The severity the imported list gave the term before mapping. `null` for terms from anywhere else. |

### Constructor

#### ProfanityTerm(string id, IEnumerable&lt;string&gt; matches, ProfanitySeverity severity, ProfanityCategory category, ProfanityTermSource source = ProfanityTermSource.Configured, IEnumerable&lt;string&gt;? exceptions = null, bool wholeWordOnly = false, int? sourceSeverity = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | Stable identifier. Trimmed. |
| `matches` | `IEnumerable<string>` | — | The spellings that match. |
| `severity` | `ProfanitySeverity` | — | How serious the term is. |
| `category` | `ProfanityCategory` | — | What kind of term it is. |
| `source` | `ProfanityTermSource` | `Configured` | Who supplied it. |
| `exceptions` | `IEnumerable<string>?` | `null` | Innocent whole words containing a spelling. |
| `wholeWordOnly` | `bool` | `false` | Whether inside-word occurrences are suppressed entirely. |
| `sourceSeverity` | `int?` | `null` | The pre-mapping severity, set only by the importer. |

Both `matches` and `exceptions` are Unicode-normalised, stripped of invisible characters, trimmed, lower-cased and de-duplicated on the way in, so the matcher never has to account for how they arrived.

Throws `ArgumentException` when `id` is null or whitespace, when `matches` yields no usable spelling, or when `severity` is `ProfanitySeverity.None`.

---

## ProfanityMatch

**Namespace:** `JC.Content.Moderation.Models`

A record. One term found in the content, reported whether or not it counted towards the block decision.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `TermId` | `string` | `""` | get; init; | The term that matched. |
| `MatchedText` | `string` | `""` | get; init; | The spelling that matched, as it appears in the original content. |
| `Context` | `string` | `""` | get; init; | The matched text with surrounding characters, the width set by `ContextCharacters`. |
| `Index` | `int` | `0` | get; init; | Where the match starts in the original content. |
| `Length` | `int` | `0` | get; init; | How many characters of the original content the match covers. |
| `Severity` | `ProfanitySeverity` | `None` | get; init; | The matched term's severity. |
| `Category` | `ProfanityCategory` | `None` | get; init; | The matched term's category. |
| `Source` | `ProfanityTermSource` | `BuiltIn` | get; init; | The matched term's source. |
| `Confidence` | `ProfanityConfidence` | `None` | get; init; | Confidence band, derived from `ConfidenceScore`. |
| `ConfidenceScore` | `int` | `0` | get; init; | Confidence as a percentage. |
| `Transformations` | `ProfanityTransformation` | `None` | get; init; | What the matcher had to do to the text to find this. |
| `Counted` | `bool` | `false` | get; init; | Whether the match met the level's floors and so contributed to the block decision. |
| `Allowed` | `bool` | `false` | get; init; | Whether an allowlist entry or a term exception suppressed the match. |
| `Superseded` | `bool` | `false` | get; init; | Whether a longer or more severe overlapping match displaced this one. |

`Source` defaults to `BuiltIn` because that is the enum's zero value, not because the match came from the curated set.

---

## ProfanityModerationResult

**Namespace:** `JC.Content.Moderation.Models`

What moderation found. Reports only — nothing here alters content or rejects anything.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ShouldBlock` | `bool` | `false` | get; init; | Whether anything met the level's floors. `false` when nothing was found, everything found was allowed, or nothing reached the floors. |
| `Severity` | `ProfanitySeverity` | `None` | get; init; | The worst severity found, whether or not it met the floors. |
| `Confidence` | `ProfanityConfidence` | `None` | get; init; | Confidence in the `Severity` finding specifically, not the highest confidence anywhere. |
| `ConfidenceScore` | `int` | `0` | get; init; | The percentage behind `Confidence`. |
| `Category` | `ProfanityCategory` | `None` | get; init; | The category of the finding that set `Severity`. |
| `Matches` | `IReadOnlyList<ProfanityMatch>` | `[]` | get; init; | Everything found — allowed, low-confidence and superseded matches included. |
| `Level` | `ProfanityLevel` | `Minimal` | get; init; | The level applied, from registration or a per-call override. |
| `Truncated` | `bool` | `false` | get; init; | Whether the content ran past `MaxContentLength`. |
| `ScannedLength` | `int` | `0` | get; init; | How many characters were examined. |
| `HasMatches` | `bool` | — | get; | Whether `Matches` holds anything, regardless of whether it counted. |
| `CountedMatches` | `IEnumerable<ProfanityMatch>` | — | get; | The matches where `Counted` is `true`. Evaluated on each enumeration. |

`Level` defaults to `Minimal` only because it is the enum's zero value; every result produced by the moderator carries the level actually applied.

The root `Severity`/`Confidence`/`ConfidenceScore`/`Category` group describes one deciding match — the most severe counted match, then the most confident of those. Where nothing counted, it falls back to the most severe match that was neither allowed nor superseded.

### Methods

#### Clean(ProfanityLevel level, bool truncated = false, int scannedLength = 0)

**Returns:** `ProfanityModerationResult`

Static.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `level` | `ProfanityLevel` | — | The level that was applied. |
| `truncated` | `bool` | `false` | Whether the content was cut before scanning. |
| `scannedLength` | `int` | `0` | How many characters were examined. |

Builds a result for content with nothing found in it: `ShouldBlock` false, every severity and confidence at `None`, and an empty `Matches` list.

---

## ProfanityModerationMaskResult

**Namespace:** `JC.Content.Moderation.Models`

The outcome of a masking, removal or tagging pass, alongside the moderation result behind it. All properties are get-only.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `ModerationResult` | `ProfanityModerationResult` | get; | Everything the moderator reported. |
| `UpdatedContent` | `string?` | get; | The rewritten content, cut to `ScannedLength` when the content was truncated. |
| `OriginalContent` | `string?` | get; | The content as supplied to the masker, whole even where `UpdatedContent` was cut. |
| `ReplacementCount` | `int` | get; | How many matches were replaced. |
| `WasModified` | `bool` | get; | Whether `UpdatedContent` differs from `OriginalContent`, by ordinal comparison. Computed once at construction. |

### Constructors

#### ProfanityModerationMaskResult(ProfanityModerationResult result, string? updatedContent, string? originalContent, int replacementCount = 0)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `result` | `ProfanityModerationResult` | — | The moderation result behind the pass. |
| `updatedContent` | `string?` | — | The rewritten content. |
| `originalContent` | `string?` | — | The content as supplied. |
| `replacementCount` | `int` | `0` | How many matches were replaced. |

#### ProfanityModerationMaskResult(ProfanityLevel level)

Builds a result for content with nothing in it: a clean moderation result at `level`, and `null` for both content values.

#### ProfanityModerationMaskResult(ProfanityModerationResult result, string? content)

Uses `content` as both the updated and the original value, so `WasModified` is always `false` and `ReplacementCount` is always `0`.

---

## ContentChange

**Namespace:** `JC.Content.Comparison.Models`

A record. One run of content and what happened to it. Runs are contiguous and in order, so concatenating every `Text` reconstructs either version — skipping `Added` for the original, `Removed` for the revised.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Type` | `ContentChangeType` | `Unchanged` | get; init; | What happened to this run. |
| `Text` | `string` | `""` | get; init; | The run as it appears in the content it belongs to. |
| `OriginalIndex` | `int?` | `null` | get; init; | Where the run starts in the original content. `null` for an addition, which has no position there. |
| `RevisedIndex` | `int?` | `null` | get; init; | Where the run starts in the revised content. `null` for a removal. |
| `Length` | `int` | — | get; | How many characters the run covers. Computed from `Text`. |

---

## ContentComparisonResult

**Namespace:** `JC.Content.Comparison.Models`

What a comparison found. Reports only — neither version is altered.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `OriginalContent` | `string` | `""` | get; init; | The original as compared, cut to the configured maximum when `Truncated`. |
| `RevisedContent` | `string` | `""` | get; init; | The revised version as compared, cut to the configured maximum when `Truncated`. |
| `Segments` | `IReadOnlyList<ContentChange>` | `[]` | get; init; | Every run of content in order, changed or not. Adjacent runs of the same type are combined. |
| `Granularity` | `ComparisonGranularity` | `Line` | get; init; | The unit the comparison ran in. |
| `Truncated` | `bool` | `false` | get; init; | Whether either version ran past `MaxContentLength`. |
| `HasChanges` | `bool` | — | get; | Whether any segment is not `Unchanged`. Evaluated on each read. |
| `Changes` | `IEnumerable<ContentChange>` | — | get; | The changed runs alone, in order. Evaluated on each enumeration. |

Every index in `Segments` is valid against `OriginalContent` and `RevisedContent` on this result, not against the strings passed to `Compare` — the two differ when `Truncated`.

`Granularity` defaults to `Line` only because it is the enum's zero value; every result produced by the comparer carries the granularity actually applied.

### Methods

#### Identical(string content, ComparisonGranularity granularity, bool truncated = false)

**Returns:** `ContentComparisonResult`

Static.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string` | — | The content both versions hold. |
| `granularity` | `ComparisonGranularity` | — | The unit the comparison would have run in. |
| `truncated` | `bool` | `false` | Whether the content was cut before comparing. |

Builds a result for two versions that were already identical. `Segments` holds one `Unchanged` run covering the whole content, or is empty when the content is empty.

#### Render(string addedOpen = "{+", string addedClose = "+}", string removedOpen = "[-", string removedClose = "-]", Func&lt;string, string&gt;? encode = null)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `addedOpen` | `string` | `"{+"` | Written before an added run. |
| `addedClose` | `string` | `"+}"` | Written after an added run. |
| `removedOpen` | `string` | `"[-"` | Written before a removed run. |
| `removedClose` | `string` | `"-]"` | Written after a removed run. |
| `encode` | `Func<string, string>?` | `null` | Applied to each segment's text before it is written. |

Walks the segments in order, wrapping changed runs in the supplied markers and writing unchanged runs as they are. The markers are written verbatim and are never passed through `encode`, so HTML tags can be used as markers while the content itself is encoded.

---

# Enums

## ProfanityCategory

**Namespace:** `JC.Content.Moderation.Enums`

| Member | Value | Description |
|--------|-------|-------------|
| `None` | 0 | No category assigned. |
| `General` | 1 | General profanity and swearing. |
| `Sexual` | 2 | Sexual acts, anatomy and pornography. |
| `Racial` | 3 | Slurs and abuse directed at race, ethnicity or nationality. |
| `Sexuality` | 4 | Slurs and abuse directed at sexuality or gender identity. |
| `Religious` | 5 | Religious profanity, blasphemy and religious abuse. |
| `Shock` | 6 | Extreme or shock content, including references to violence and abuse. |
| `Custom` | 7 | A category defined by the consuming application. |

---

## ProfanityConfidence

**Namespace:** `JC.Content.Moderation.Enums`

Bands over `ProfanityMatch.ConfidenceScore`. The two interior boundaries are configurable; the values below are those of the default 40 and 70 floors.

| Member | Value | Description |
|--------|-------|-------------|
| `None` | 0 | A score of zero — nothing found, or the match was suppressed. |
| `Low` | 1 | Below `MediumConfidenceMinimum`. |
| `Medium` | 2 | At or above `MediumConfidenceMinimum`, below `HighConfidenceMinimum`. |
| `High` | 3 | At or above `HighConfidenceMinimum`, below 100. |
| `Certain` | 4 | A score of exactly 100. |

---

## ProfanityLevel

**Namespace:** `JC.Content.Moderation.Enums`

The floors a match must reach to count towards the block decision. Detection is identical at every level.

| Member | Value | Minimum severity | Minimum confidence |
|--------|-------|------------------|--------------------|
| `Minimal` | 0 | `High` | `High` |
| `Lax` | 1 | `High` | `Medium` |
| `Safe` | 2 | `Medium` | `Medium` |
| `Strict` | 3 | `Low` | `Medium` |
| `SuperStrict` | 4 | `Mild` | `Medium` |

---

## ProfanitySeverity

**Namespace:** `JC.Content.Moderation.Enums`

| Member | Value | Description |
|--------|-------|-------------|
| `None` | 0 | No profanity. Rejected by the `ProfanityTerm` constructor. |
| `Mild` | 1 | Mild-severity profanity. |
| `Low` | 2 | Low-severity profanity. |
| `Medium` | 3 | Medium-severity profanity. |
| `High` | 4 | High-severity profanity. |
| `Severe` | 5 | Severe-severity profanity. Reserved for slurs aimed at people. |

---

## ProfanityTermSource

**Namespace:** `JC.Content.Moderation.Enums`

Also the precedence order in the registry, but inverted: `Configured` outranks `BuiltIn`, which outranks `Imported`.

| Member | Value | Description |
|--------|-------|-------------|
| `BuiltIn` | 0 | Curated by JC.Content. Severity and category are assigned deliberately. |
| `Imported` | 1 | Derived from the bundled third-party list. Broad coverage, less exact metadata. |
| `Configured` | 2 | Registered by the consuming application. Takes precedence over both of the above. |

---

## ProfanityTransformation

**Namespace:** `JC.Content.Moderation.Enums`

A `[Flags]` enum. What the matcher had to do to the text before a term matched.

| Member | Value | Description |
|--------|-------|-------------|
| `None` | 0 | The term matched the text as written. |
| `CaseFolded` | 1 | Only the casing differed. Not penalised. |
| `DiacriticsRemoved` | 2 | Accents were stripped. |
| `RunExpanded` | 4 | A letter was repeated beyond what the term needs. |
| `Leetspeak` | 8 | A digit or symbol stood in for a letter. |
| `SeparatorsRemoved` | 16 | Punctuation was stepped over between the letters, within a single token. |
| `MaskWildcard` | 32 | A letter was masked out. Evidence of intent rather than of doubt. |
| `InsideWord` | 64 | The match sits inside a longer word. Caps confidence, so it can never block. |
| `HomoglyphFolded` | 128 | A letter from another script stood in for a Latin one. |
| `WordBreakRemoved` | 256 | Whitespace was stepped over. Caps confidence, so it can never block. |
| `ConfusableFolded` | 512 | One Latin letter stood in for another. |

Only `Leetspeak`, `MaskWildcard` and `ConfusableFolded` reduce the confidence score; `InsideWord` and `WordBreakRemoved` cap it instead. The rest are free, because each requires the term's letters to be present in order and so cannot invent a match.

---

## ProfanityMaskType

**Namespace:** `JC.Content.Models`

Nested in `ProfanitySettings`, so written `ProfanitySettings.ProfanityMaskType`.

| Member | Value | Description |
|--------|-------|-------------|
| `Mask` | 0 | Replace each match with a run of `MaskChar`. |
| `Remove` | 1 | Strip each match, collapsing the whitespace at the seam. |
| `Tag` | 2 | Replace each match with `TagFormat`. |

---

## ComparisonGranularity

**Namespace:** `JC.Content.Comparison.Enums`

| Member | Value | Description |
|--------|-------|-------------|
| `Line` | 0 | Whole lines, each carrying its own terminator. |
| `Word` | 1 | Words, each carrying the whitespace that follows it. |
| `Character` | 2 | Individual characters as a reader sees them — a surrogate pair or a base letter with its combining marks stays whole. |

---

## ContentChangeType

**Namespace:** `JC.Content.Comparison.Enums`

| Member | Value | Description |
|--------|-------|-------------|
| `Unchanged` | 0 | Present in both versions, unaltered. |
| `Added` | 1 | Present only in the revised content. |
| `Removed` | 2 | Present only in the original content. |

---

## ContentFormat

**Namespace:** `JC.Content.Conversion.Enums`

| Member | Value | Description |
|--------|-------|-------------|
| `PlainText` | 0 | Plain text, with no markup of any kind. |
| `Markdown` | 1 | Markdown. |
| `Html` | 2 | HTML. |

---

# Services

## ContentManager

**Namespace:** `JC.Content.Services`

Runs the feature areas as a pipeline: normalise, then moderate, then convert or compare. Registered as a singleton and injected directly — there is no interface.

### Constructor

#### ContentManager(ProfanityMasker profanityMasker, ContentConverter contentConverter, ContentComparer contentComparer)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `profanityMasker` | `ProfanityMasker` | — | Performs the moderation stage. |
| `contentConverter` | `ContentConverter` | — | Performs the conversion stage. |
| `contentComparer` | `ContentComparer` | — | Performs the comparison stage. |

All three are required even by calls that use only one of them, so `AddContentManager` registers all three feature areas.

### Methods

#### NormaliseAndModerate(string? content, ManagerSettings? settings = null)

**Returns:** `ManagerResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to process. |
| `settings` | `ManagerSettings?` | `null` | Per-call settings. A default instance is used when omitted. |

Normalises the content according to `settings.NormalisationSettings`, then moderates the result according to `settings.ProfanitySettings`. The response carries the untouched input alongside the moderation outcome.

#### NormaliseModerateAndConvert(string? content, ManagerConvertSettings? settings = null)

**Returns:** `ManagerConvertResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to process. |
| `settings` | `ManagerConvertSettings?` | `null` | Per-call settings including the format pair. A default instance — Markdown to HTML — is used when omitted. |

Normalises the content, then runs moderation and conversion in an order that depends on the target format. When `TargetFormat` is `Html`, moderation runs on the source and the moderated text is converted; otherwise conversion runs first and moderation is applied to its output. Either way the text being scanned is never HTML, because masking inside a tag, attribute or URL would corrupt the markup.

`ConvertedContent` holds the finished content in both cases. The moderation result — and every `ProfanityMatch.Index` within it — describes the source content when the target is HTML, and the converted output otherwise.

#### NormaliseModerateAndCompare(string? content, string? compareContent, ManagerCompareSettings? settings = null)

**Returns:** `ManagerCompareResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The first version. |
| `compareContent` | `string?` | — | The version to compare against the first. |
| `settings` | `ManagerCompareSettings?` | `null` | Per-call settings including the granularity override. A default instance is used when omitted. |

Normalises and moderates both versions with the same settings, then compares the two moderated results. Moderation runs before chunking because a match straddling a segment boundary would be invisible to every fragment it was split across.

Because the comparison sees the moderated text, two different terms rewritten to the same replacement read as unchanged.

---

## ProfanityModerator

**Namespace:** `JC.Content.Moderation.Services`

Reports what moderation found in a piece of content. Registered as a singleton and injected directly. It reports and nothing more — the content is never altered, and `ShouldBlock` is only this package's reading of the level in force.

### Constructor

#### ProfanityModerator(ProfanityTermRegistry registry, ProfanityModerationOptions options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `registry` | `ProfanityTermRegistry` | — | The active term set and allowlist. |
| `options` | `ProfanityModerationOptions` | — | Moderation settings. Validated here as well as at registration. |

Throws `ArgumentNullException` when either argument is `null`, and whatever `options.Validate()` throws when a setting is out of range.

### Methods

#### Analyse(string? content, ProfanityLevel? level = null)

**Returns:** `ProfanityModerationResult`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to examine. Null or whitespace comes back clean. |
| `level` | `ProfanityLevel?` | `null` | Overrides the registered level. Affects the block decision only. |

Cuts the content to `MaxContentLength` where one is set, backing off a character where the limit would fall between the two halves of one; folds it into a comparable form; and reports every term found. Each match is scored for confidence, checked against the level's floors, and marked `Counted` accordingly. Overlapping matches are resolved so a phrase and the word inside it are one finding, with the displaced one marked `Superseded` rather than dropped.

The matcher holding the prepared index is rebuilt when the registry's `Version` has moved on, so term changes made after startup take effect on the next call.

---

## ProfanityMasker

**Namespace:** `JC.Content.Moderation.Services`

Rewrites the matches `ProfanityModerator` found. Registered as a singleton and injected directly.

**All three methods return the content unchanged when the moderation result does not block.** Only matches that met the level's floors are rewritten, so a finding below them is reported and left in place.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `CategoryTag` | `string` | `"{category}"` | Placeholder substituted with the match's category. |
| `SeverityTag` | `string` | `"{severity}"` | Placeholder substituted with the match's severity. |
| `GenericTagValue` | `string` | `"Removed"` | Substituted for `CategoryTag` when the match has no category. |
| `GenericTag` | `string` | `"[Removed]"` | The default tag format. |

### Constructor

#### ProfanityMasker(ProfanityModerator moderator)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `moderator` | `ProfanityModerator` | — | Performs the analysis behind every rewrite. |

Throws `ArgumentNullException` when `moderator` is `null`.

### Methods

#### AnalyseAndMask(string? content, char maskChar = '\*', bool preserveLength = false, ushort? cappedMaskLength = 4, ProfanityLevel? level = null)

**Returns:** `ProfanityModerationMaskResult`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to examine and rewrite. |
| `maskChar` | `char` | `'*'` | The character to fill the match with. |
| `preserveLength` | `bool` | `false` | Whether the run matches the length of the text it replaces. |
| `cappedMaskLength` | `ushort?` | `4` | The longest run to write, or `null` for no cap. |
| `level` | `ProfanityLevel?` | `null` | Overrides the registered level. |

Replaces each counted match with a run of `maskChar`. With no cap the run is the match length whatever `preserveLength` says; with a cap it is the cap itself, or the match length limited by the cap when `preserveLength` is `true`.

#### AnalyseAndRemove(string? content, ProfanityLevel? level = null)

**Returns:** `ProfanityModerationMaskResult`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to examine and rewrite. |
| `level` | `ProfanityLevel?` | `null` | Overrides the registered level. |

Removes each counted match. Where the removal leaves whitespace on both sides of the seam, one is dropped; at either end of the content there is no seam, so the leading or trailing space is removed instead.

#### AnalyseAndTag(string? content, string tagFormat = GenericTag, ProfanityLevel? level = null)

**Returns:** `ProfanityModerationMaskResult`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to examine and rewrite. |
| `tagFormat` | `string` | `"[Removed]"` | The replacement template. |
| `level` | `ProfanityLevel?` | `null` | Overrides the registered level. |

Replaces each counted match with `tagFormat`, substituting `CategoryTag` and `SeverityTag` case-insensitively where they appear. A format naming neither is used verbatim. A match with no category substitutes `GenericTagValue`.

Throws `ArgumentException` when `tagFormat` is null or empty — the only one of the three methods that throws on its own arguments.

---

## ProfanityTermRegistry

**Namespace:** `JC.Content.Moderation.Services`

The active set of blocked terms and the words that suppress a match. Registered as a singleton, seeded on first resolve, and safe to mutate afterwards — every member locks internally.

Terms are keyed by id. Where two sources supply the same id the higher-precedence one wins: `Configured`, then `BuiltIn`, then `Imported`.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Version` | `int` | get; | Increments on every change to the terms or the allowlist. A matcher holding a prepared index compares this to know whether the index is still current. |
| `Count` | `int` | get; | How many terms are registered. |

### Methods

#### GetTerms()

**Returns:** `IReadOnlyList<ProfanityTerm>`

Every registered term, in no particular order. Returns a snapshot copy taken under the lock.

#### GetTerms(ProfanityTermSource source)

**Returns:** `IReadOnlyList<ProfanityTerm>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `source` | `ProfanityTermSource` | — | The source to filter by. |

#### GetAllowed()

**Returns:** `IReadOnlyCollection<string>`

Every word and phrase that suppresses a match falling inside it, whatever term matched. Applies on top of a term's own `Exceptions`.

#### TryGetTerm(string id, out ProfanityTerm? term)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The term id to look up. Trimmed, and matched case-insensitively. |
| `term` | `out ProfanityTerm?` | — | The term found, or `null`. |

Returns `false` without setting `term` when `id` is null or whitespace.

#### TryAddTerm(ProfanityTerm term)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `term` | `ProfanityTerm` | — | The term to register. |

Registers the term, replacing an existing one of the same id when this one comes from an equal or higher-precedence source. Returns `false` and leaves the existing term in place when it outranks the new one. Increments `Version` on success. Throws `ArgumentNullException` when `term` is `null`.

#### AddTerms(IEnumerable&lt;ProfanityTerm&gt; terms)

**Returns:** `int`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `terms` | `IEnumerable<ProfanityTerm>` | — | The terms to register. |

Registers each in turn and returns how many were taken. Throws `ArgumentNullException` when `terms` is `null`.

#### TryRemoveTerm(string id)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The term id to remove. Trimmed. |

Removes the term entirely, so it is never looked for. Distinct from `Allow`, which keeps the term but forgives a specific context. Returns `false` when `id` is null or whitespace, or when no term of that id is registered. Increments `Version` on success.

#### ClearTerms()

**Returns:** `void`

Empties the term set, including everything seeded at startup. Always increments `Version`.

#### RemoveTerms(ProfanityTermSource source)

**Returns:** `int`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `source` | `ProfanityTermSource` | — | The source whose terms are removed. |

Removes every term from one source, leaving the others in place, and returns how many went. Increments `Version` only when something was removed.

#### Allow(string word)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `word` | `string` | — | The word or phrase to allow. Trimmed, and compared case-insensitively. |

Adds a word or phrase that suppresses any match falling inside it. A suppressed match is still reported, at zero confidence and with `Allowed` set. Returns `false` when the word is null, whitespace, or already present. Increments `Version` on success.

#### Allow(IEnumerable&lt;string&gt; words)

**Returns:** `int`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `words` | `IEnumerable<string>` | — | The words to allow. |

Adds each in turn and returns how many were taken. Throws `ArgumentNullException` when `words` is `null`.

#### Disallow(string word)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `word` | `string` | — | The allowlist entry to remove. Trimmed. |

Returns `false` when the word is null, whitespace, or not present. Increments `Version` on success.

#### ClearAllowed()

**Returns:** `void`

Empties the allowlist. Always increments `Version`.

---

## ContentComparer

**Namespace:** `JC.Content.Comparison.Services`

Reports how two versions of a piece of content differ. Registered as a singleton and injected directly.

Content is compared exactly as supplied — nothing is normalised, trimmed or case-folded on the way in, so a change of line ending or of casing is a change.

### Constructor

#### ContentComparer(ContentComparisonOptions options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `ContentComparisonOptions` | — | Comparison settings. Validated here as well as at registration. |

Throws `ArgumentNullException` when `options` is `null`.

### Methods

#### Compare(string? original, string? revised, ComparisonGranularity? granularity = null)

**Returns:** `ContentComparisonResult`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `original` | `string?` | — | The version being changed from. Null is treated as empty. |
| `revised` | `string?` | — | The version being changed to. Null is treated as empty. |
| `granularity` | `ComparisonGranularity?` | `null` | Overrides the registered granularity. |

Cuts both versions to `MaxContentLength` where one is set, backing off a character where the limit would fall between the two halves of one. Two versions that are ordinally equal short-circuit to an `Identical` result without being chunked. Otherwise both are split into the configured unit, differenced, and walked into contiguous runs — the matching runs between the reported difference blocks are implied and filled in.

---

## ContentConverter

**Namespace:** `JC.Content.Conversion.Services`

Converts content between plain text, Markdown and HTML. Registered as a singleton and injected directly.

Conversion is structural, not cosmetic: an element with no equivalent in the target format has its markup removed and its content kept. HTML is the hub — Markdown reaches plain text through it, so plain-text output reads the same whichever format it came from.

### Constructor

#### ContentConverter(ContentConversionOptions options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `ContentConversionOptions` | — | Conversion settings. Not validated — every combination is legal. |

Throws `ArgumentNullException` when `options` is `null`. Builds the Markdown pipeline and both HTML writers once.

### Methods

#### Convert(string? content, ContentFormat from, ContentFormat to)

**Returns:** `string?`, annotated `NotNullIfNotNull(nameof(content))`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to convert. |
| `from` | `ContentFormat` | — | The format the content is in. |
| `to` | `ContentFormat` | — | The format to produce. |

Returns the content unchanged when it is null or empty, or when the two formats match. A Markdown-to-plain-text conversion is performed as Markdown to HTML to plain text.

The six methods below are named shortcuts over this one and behave identically.

#### HtmlToMarkdown(string? html)

**Returns:** `string?`, annotated `NotNullIfNotNull(nameof(html))`

Maps what it can onto Markdown syntax and drops the tags it cannot, keeping their content.

#### HtmlToText(string? html)

**Returns:** `string?`, annotated `NotNullIfNotNull(nameof(html))`

Produces readable text with blocks separated and list markers kept. Not tag stripping — that would run adjacent blocks together.

#### MarkdownToHtml(string? markdown)

**Returns:** `string?`, annotated `NotNullIfNotNull(nameof(markdown))`

Raw HTML embedded in the document is stripped unless `AllowRawHtml` is set.

#### MarkdownToText(string? markdown)

**Returns:** `string?`, annotated `NotNullIfNotNull(nameof(markdown))`

Converts via HTML.

#### TextToHtml(string? text)

**Returns:** `string?`, annotated `NotNullIfNotNull(nameof(text))`

Encodes the text first, then gives it paragraphs on blank lines and `<br />` breaks within them. The encoding is not optional — text containing markup is text.

#### TextToMarkdown(string? text)

**Returns:** `string?`, annotated `NotNullIfNotNull(nameof(text))`

Escapes the text so Markdown renders it exactly as written, including two trailing spaces where a typed line break must survive.

---

# Helpers

## ContentSanitiser

**Namespace:** `JC.Content.Helpers`

Server-side sanitisation for HTML authored by a user, typically the output of a rich-text editor. Everything outside the configured allowlist is removed: scripts, event handlers, `javascript:` URLs and unknown elements.

Not registered in DI. Construct it where it is needed, or use the static shorthand. A fresh underlying sanitiser is built for every call, because the library documents no thread-safety guarantee and the options are mutable.

### Constructors

#### ContentSanitiser()

Uses `ContentSanitiserOptions.RichText()`.

#### ContentSanitiser(ContentSanitiserOptions options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `ContentSanitiserOptions` | — | The allowlists to enforce. |

Throws `ArgumentNullException` when `options` is `null`.

#### ContentSanitiser(Action&lt;ContentSanitiserOptions&gt; configure)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configure` | `Action<ContentSanitiserOptions>` | — | Receives the rich-text options to modify. |

Starts from `ContentSanitiserOptions.RichText()` and applies the callback. Throws `ArgumentNullException` when `configure` is `null`.

### Methods

#### Sanitise(string? html)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `html` | `string?` | — | The untrusted HTML to sanitise. |

Returns the sanitised HTML, or `null` when `html` is null, empty or whitespace — so a visually empty editor stores "no content" rather than stray markup.

#### SanitiseContent(string? html)

**Returns:** `string?`

Static.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `html` | `string?` | — | The untrusted HTML to sanitise. |

Sanitises against `ContentSanitiserOptions.RichText()` without constructing an instance. Equivalent to `new ContentSanitiser().Sanitise(html)`, including the `null` return for empty content.

---

## NormalisationHelper

**Namespace:** `JC.Content.Helpers`

Static. Cleans up content without changing what it says. Not registered in DI, and not to be confused with the internal canonicaliser behind profanity matching — that output is lossy and only ever matched against, where these results are meant to be kept.

**Every method returns `null` only for `null` input**, and each is annotated `NotNullIfNotNull` on its content parameter. Empty and whitespace strings come back as themselves.

### Methods

#### Normalise(string? content, bool compatibility = false, string lineEnding = "\n")

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to normalise. |
| `compatibility` | `bool` | `false` | Use NFKC rather than NFC. |
| `lineEnding` | `string` | `"\n"` | What line endings become. |

The safe pass, in order: lone surrogates removed, Unicode normalised, invisible characters removed, line endings made consistent, trailing whitespace trimmed from each line, and the whole result trimmed. Nothing here alters wording, spacing within a line, or paragraph structure.

#### NormaliseUnicode(string? content, bool compatibility = false)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to normalise. |
| `compatibility` | `bool` | `false` | Use NFKC rather than NFC, folding compatibility forms such as the 'fi' ligature and circled digits. |

Removes lone surrogates first, since `string.Normalize` throws on them, and skips the work where the content is already in the requested form.

#### RemoveLoneSurrogates(string? content)

**Returns:** `string?`

Removes surrogates missing their pair — half a character, produced by cutting a string at a count that lands mid-pair. Returns the input unchanged when it contains no surrogate code units at all.

#### RemoveInvisibleCharacters(string? content, bool removeJoiners = false)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to clean. |
| `removeJoiners` | `bool` | `false` | Whether to also remove U+200C and U+200D. |

Removes zero-width and direction-override characters. The zero-width joiners are kept by default because they are required in Arabic, Persian and Indic scripts and join emoji sequences; the U+200E and U+200F direction marks are always kept, being legitimate in mixed-direction text.

#### NormaliseLineEndings(string? content, string lineEnding = "\n")

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to convert. |
| `lineEnding` | `string` | `"\n"` | What line endings become. |

Converts `\r\n` before lone `\r`, so a pair becomes one line ending rather than two. Throws `ArgumentNullException` when `lineEnding` is `null`.

#### TrimLineEnds(string? content, string lineEnding = "\n")

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to trim. |
| `lineEnding` | `string` | `"\n"` | The line ending in use. |

Removes trailing whitespace from each line, leaving the lines themselves intact. Splits on `lineEnding`, so a value that does not match the content's line endings leaves it untouched.

#### CollapseWhitespace(string? content)

**Returns:** `string?`

Reduces runs of spaces and tabs to one space, leaving line breaks alone. Takes no `lineEnding` parameter, unlike its siblings, because the pattern it uses excludes carriage returns and newlines by construction. Destructive where spacing carries meaning — aligned tables, indented code.

#### CollapseBlankLines(string? content, int maxBlankLines = 1, string lineEnding = "\n")

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string?` | — | The content to collapse. |
| `maxBlankLines` | `int` | `1` | The most consecutive blank lines to leave. |
| `lineEnding` | `string` | `"\n"` | The line ending in use. |

Splits on `lineEnding`, so it should be run after `NormaliseLineEndings` and given the same value — a mismatch leaves the content untouched. A line of only whitespace counts as blank. Throws `ArgumentOutOfRangeException` when `maxBlankLines` is negative.

#### NormaliseQuotes(string? content)

**Returns:** `string?`

Replaces typographic single and double quotes with straight ones. Guillemets are left alone, being the quotation marks of several languages rather than a stylistic variant.

#### NormaliseDashes(string? content)

**Returns:** `string?`

Replaces en dashes, em dashes, horizontal bars, figure dashes and the minus sign with a hyphen.

#### RemoveDiacritics(string? content)

**Returns:** `string?`

Decomposes the content, drops non-spacing marks, and recomposes — leaving the base letters. For search keys and comparison rather than for content being kept.

---

## ProfanityLevelPolicy

**Namespace:** `JC.Content.Moderation.Helpers`

Static. The thresholds behind each `ProfanityLevel`. Public so an application can ignore `ShouldBlock` and apply its own thresholds using the same arithmetic, rather than restating the rules and drifting from them.

### Methods

#### Floors(ProfanityLevel level)

**Returns:** `(ProfanitySeverity Severity, ProfanityConfidence Confidence)`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `level` | `ProfanityLevel` | — | The level to read. |

The minimum severity and confidence a match must reach to count at that level. Any value outside the defined levels returns the `SuperStrict` pair.

#### Counts(ProfanityLevel level, ProfanitySeverity severity, ProfanityConfidence confidence)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `level` | `ProfanityLevel` | — | The level to test against. |
| `severity` | `ProfanitySeverity` | — | The finding's severity. |
| `confidence` | `ProfanityConfidence` | — | The finding's confidence band. |

Whether a finding of that severity and confidence meets both of the level's floors. Note that this tests the floors alone — the moderator additionally requires a match to be neither allowed nor superseded before counting it.

#### ToConfidence(int score, ushort mediumMinimum, ushort highMinimum)

**Returns:** `ProfanityConfidence`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `score` | `int` | — | The confidence percentage. |
| `mediumMinimum` | `ushort` | — | The floor of the Medium band, from `ProfanityModerationOptions`. |
| `highMinimum` | `ushort` | — | The floor of the High band, from `ProfanityModerationOptions`. |

The band a percentage falls in. Half-open, so each score belongs to exactly one band: zero or below is `None`, and only exactly 100 is `Certain`. The two floors are parameters rather than constants because they are configurable at registration.
