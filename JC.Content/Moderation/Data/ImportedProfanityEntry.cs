using System.Text.Json.Serialization;

namespace JC.Content.Moderation.Data;

/// <summary>
/// One entry as it appears in the bundled list. Mirrors that file's shape exactly — mapping onto
/// <see cref="Models.ProfanityTerm"/> is <see cref="ProfanityDataImporter"/>'s job, so the file can
/// be replaced wholesale when upstream changes.
/// </summary>
internal sealed class ImportedProfanityEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Spellings separated by <c>|</c>. A <c>*</c> means the preceding letter may repeat, as in
    /// <c>ba*sta*rd</c> — which our canonicaliser already handles, so the importer strips it.
    /// </summary>
    [JsonPropertyName("match")]
    public string Match { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<string>? Tags { get; set; }

    /// <summary>Upstream severity, 1 (mild) to 4 (severe).</summary>
    [JsonPropertyName("severity")]
    public int Severity { get; set; }

    /// <summary>
    /// Innocent words containing a spelling, with <c>*</c> standing in for the term — <c>m*cript</c>
    /// against <c>anus</c> is <c>manuscript</c>.
    /// </summary>
    [JsonPropertyName("exceptions")]
    public List<string>? Exceptions { get; set; }

    /// <summary>Unused by the current file, but part of the upstream schema.</summary>
    [JsonPropertyName("allow_partial")]
    public bool? AllowPartial { get; set; }
}