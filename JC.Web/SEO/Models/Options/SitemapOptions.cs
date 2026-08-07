namespace JC.Web.SEO.Models.Options;

/// <summary>
/// Configuration for sitemap generation and serving. Bound from <c>Web:SEO:Sitemap</c> when an
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> is supplied at registration.
/// </summary>
public class SitemapOptions
{
    /// <summary>
    /// Configuration section bound by the <c>AddSitemap</c> overloads that take an
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    public const string ConfigSection = "Web:SEO:Sitemap";

    /// <summary>
    /// Absolute base URL used to resolve site-relative locations, for example <c>https://example.com</c>.
    /// </summary>
    /// <remarks>
    /// When null the request's scheme and host are used instead. Behind a reverse proxy that
    /// yields the internal host unless forwarded headers are configured, which silently produces
    /// a sitemap full of unreachable URLs — so setting this explicitly is the reliable option.
    /// </remarks>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Path the sitemap is served from. Defaults to <c>/sitemap.xml</c>. When the sitemap is split,
    /// the numbered files derive from this path — <c>/sitemap-1.xml</c>, <c>/sitemap-2.xml</c>, and so on.
    /// </summary>
    public string Path { get; set; } = "/sitemap.xml";

    /// <summary>
    /// Whether to discover Razor Page routes from the application's endpoints. Defaults to <c>true</c>.
    /// </summary>
    public bool DiscoverRoutes { get; set; } = true;

    /// <summary>
    /// Whether discovery also covers MVC controller actions. Defaults to <c>false</c>, since controller
    /// routes more often expose endpoints that should not be indexed.
    /// </summary>
    public bool DiscoverControllers { get; set; }

    /// <summary>
    /// Route prefixes excluded from discovery. Defaults to <c>/api</c> and <c>/_</c>.
    /// </summary>
    public List<string> ExcludePrefixes { get; set; } = ["/api", "/_"];

    /// <summary>
    /// Maximum URLs per sitemap file. Beyond this the sitemap is split and
    /// <see cref="Path"/> serves an index instead. Defaults to 50,000, the protocol limit.
    /// </summary>
    public int MaxUrlsPerFile { get; set; } = 50_000;

    /// <summary>
    /// Value used for the <c>Cache-Control: public, max-age</c> header on sitemap responses.
    /// Defaults to one hour. Set to <see cref="TimeSpan.Zero"/> to omit the header.
    /// </summary>
    /// <remarks>
    /// This is downstream caching only — the sitemap is still regenerated whenever a request
    /// reaches the server, so every provider runs again on each miss.
    /// </remarks>
    public TimeSpan ClientCacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// URLs added explicitly, in addition to any discovered routes or provider results.
    /// </summary>
    public List<SitemapUrl> Urls { get; set; } = [];

    /// <summary>
    /// Adds a URL to the sitemap.
    /// </summary>
    /// <param name="location">A site-relative path such as <c>/about</c>, or an absolute URL.</param>
    /// <param name="priority">Optional relative importance from 0.0 to 1.0.</param>
    /// <param name="changeFrequency">Optional expected change frequency.</param>
    /// <param name="lastModified">Optional last-modified timestamp.</param>
    /// <returns>The options instance, so calls can be chained.</returns>
    public SitemapOptions Add(string location, double? priority = null,
        ChangeFrequency? changeFrequency = null, DateTime? lastModified = null)
    {
        Urls.Add(new SitemapUrl(location)
        {
            Priority = priority,
            ChangeFrequency = changeFrequency,
            LastModified = lastModified
        });

        return this;
    }
}
