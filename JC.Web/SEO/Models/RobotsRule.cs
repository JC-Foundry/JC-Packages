namespace JC.Web.SEO.Models;

/// <summary>
/// A single <c>User-agent</c> group in robots.txt, with its allow and disallow paths.
/// </summary>
public sealed class RobotsRule
{
    /// <summary>
    /// The crawler this group applies to. <c>*</c> matches all crawlers. Defaults to <c>*</c>.
    /// </summary>
    public string UserAgent { get; set; } = "*";

    /// <summary>
    /// Paths this crawler must not fetch. A single <c>/</c> blocks the whole site.
    /// </summary>
    public List<string> Disallow { get; set; } = [];

    /// <summary>
    /// Paths this crawler may fetch, used to carve exceptions out of a broader <see cref="Disallow"/>.
    /// </summary>
    public List<string> Allow { get; set; } = [];

    /// <summary>
    /// Seconds a crawler should wait between requests. Omitted when null.
    /// Google ignores this; most other crawlers honour it.
    /// </summary>
    public int? CrawlDelay { get; set; }
}
