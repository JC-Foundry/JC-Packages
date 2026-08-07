namespace JC.Web.SEO.Models;

/// <summary>
/// A single entry in the sitemap. <see cref="Location"/> may be a site-relative path
/// (resolved against the configured base URL) or an absolute URL.
/// </summary>
public sealed class SitemapUrl
{
    /// <summary>
    /// Creates an empty entry. Used by configuration binding.
    /// </summary>
    public SitemapUrl() { }

    /// <summary>
    /// Creates an entry for the given location.
    /// </summary>
    /// <param name="location">A site-relative path such as <c>/about</c>, or an absolute URL.</param>
    public SitemapUrl(string location) => Location = location;

    /// <summary>
    /// The page location. Site-relative paths are resolved against the configured base URL.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// When the page last changed. Omitted from the output when null.
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// How often the page is expected to change. Omitted when null.
    /// </summary>
    /// <remarks>
    /// Advisory only — Google has stated publicly that it ignores this element. It is emitted
    /// for crawlers that do read it, but it is not worth tuning.
    /// </remarks>
    public ChangeFrequency? ChangeFrequency { get; set; }

    /// <summary>
    /// Relative importance within this site, from 0.0 to 1.0. Omitted when null.
    /// </summary>
    /// <remarks>
    /// Advisory only, and likewise ignored by Google. Values outside the range are clamped
    /// when written, since an out-of-range priority makes the whole sitemap invalid.
    /// </remarks>
    public double? Priority { get; set; }
}

/// <summary>
/// How frequently a page is expected to change, as defined by the sitemap protocol.
/// </summary>
public enum ChangeFrequency
{
    /// <summary>Changes each time it is accessed.</summary>
    Always,

    /// <summary>Changes hourly.</summary>
    Hourly,

    /// <summary>Changes daily.</summary>
    Daily,

    /// <summary>Changes weekly.</summary>
    Weekly,

    /// <summary>Changes monthly.</summary>
    Monthly,

    /// <summary>Changes yearly.</summary>
    Yearly,

    /// <summary>Archived; will not change again.</summary>
    Never
}
