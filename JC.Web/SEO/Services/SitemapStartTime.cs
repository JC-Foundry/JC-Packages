namespace JC.Web.SEO.Services;

/// <summary>
/// Holds the moment the application started, used as the <c>lastmod</c> for discovered and manually
/// added URLs.
/// </summary>
/// <remarks>
/// Registered as a pre-constructed singleton so the value is captured during service registration.
/// Reading the clock per request would report every page as having changed moments ago, which is
/// the pattern that leads crawlers to stop trusting the signal altogether.
/// </remarks>
internal sealed class SitemapStartTime
{
    /// <summary>
    /// UTC timestamp captured when this instance was created.
    /// </summary>
    public DateTime StartedUtc { get; } = DateTime.UtcNow;
}
