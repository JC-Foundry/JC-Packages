using System.Text.Json;
using System.Text.Json.Serialization;

namespace JC.Web.SEO.Helpers.JsonLd;

/// <summary>
/// Base for schema.org structured data emitted as JSON-LD. Derive from this for types not covered
/// by the built-in schemas, or pass any object to
/// <see cref="SeoBuilder.JsonLd(object)"/> when you would rather shape the payload yourself.
/// </summary>
public abstract class SchemaObject
{
    /// <summary>
    /// The JSON-LD context. Always <c>https://schema.org</c>.
    /// </summary>
    [JsonPropertyName("@context")]
    [JsonPropertyOrder(-2)]
    public string Context => "https://schema.org";

    /// <summary>
    /// The schema.org type name, for example <c>Product</c>.
    /// </summary>
    [JsonPropertyName("@type")]
    [JsonPropertyOrder(-1)]
    public abstract string Type { get; }
}

/// <summary>
/// Serialises structured data for embedding in a <c>&lt;script type="application/ld+json"&gt;</c> block.
/// </summary>
internal static class JsonLdSerialiser
{
    /// <summary>
    /// Serialisation settings for JSON-LD output.
    /// </summary>
    /// <remarks>
    /// This deliberately uses the <b>default</b> encoder rather than
    /// <c>JavaScriptEncoder.UnsafeRelaxedJsonEscaping</c>. The relaxed encoder leaves <c>&lt;</c>
    /// unescaped, which would let a <c>&lt;/script&gt;</c> sequence in any database-sourced field
    /// — a product name or article headline — close the script element and execute whatever
    /// followed it. The default encoder escapes it to <c><</c>, so the payload cannot break out.
    /// </remarks>
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serialises the supplied structured data to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialise.</param>
    /// <returns>The JSON representation.</returns>
    public static string Serialise(object value)
        => JsonSerializer.Serialize(value, value.GetType(), Options);
}
