namespace JC.Web.SEO.Models.Options;

/// <summary>
/// Configuration for robots.txt generation. Bound from <c>Web:SEO:Robots</c> when an
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> is supplied at registration.
/// </summary>
/// <remarks>
/// Keeping these rules in configuration rather than code lets a staging environment disallow
/// everything through its own appsettings file, without a branch in application startup.
/// </remarks>
public class RobotsOptions
{
    /// <summary>
    /// Configuration section bound by the <c>AddRobots</c> overloads that take an
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    public const string ConfigSection = "Web:SEO:Robots";

    /// <summary>
    /// Path robots.txt is served from. Defaults to <c>/robots.txt</c>, which is the only location
    /// crawlers look in — change it only if something else already serves that path.
    /// </summary>
    public string Path { get; set; } = "/robots.txt";

    /// <summary>
    /// The user-agent groups written to the file. When empty, a single permissive
    /// <c>User-agent: *</c> group is written with nothing disallowed.
    /// </summary>
    public List<RobotsRule> Rules { get; set; } = [];

    /// <summary>
    /// Whether to append a <c>Sitemap:</c> directive. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// The directive is only written when a sitemap is actually registered or
    /// <see cref="SitemapUrl"/> is set, so robots.txt never advertises a location that 404s.
    /// </remarks>
    public bool IncludeSitemapDirective { get; set; } = true;

    /// <summary>
    /// Explicit absolute sitemap URL for the <c>Sitemap:</c> directive. When null, the URL is
    /// derived from the registered sitemap options.
    /// </summary>
    public string? SitemapUrl { get; set; }

    /// <summary>
    /// Additional absolute sitemap URLs to advertise, for sitemaps this application does not serve itself.
    /// </summary>
    public List<string> AdditionalSitemaps { get; set; } = [];

    /// <summary>
    /// Adds disallowed paths for a user agent, creating the group if it does not exist.
    /// </summary>
    /// <param name="userAgent">The crawler to apply this to, or <c>*</c> for all.</param>
    /// <param name="paths">One or more paths to disallow.</param>
    /// <returns>The options instance, so calls can be chained.</returns>
    public RobotsOptions Disallow(string userAgent, params string[] paths)
    {
        GetOrAddRule(userAgent).Disallow.AddRange(paths);
        return this;
    }

    /// <summary>
    /// Adds allowed paths for a user agent, creating the group if it does not exist.
    /// </summary>
    /// <param name="userAgent">The crawler to apply this to, or <c>*</c> for all.</param>
    /// <param name="paths">One or more paths to allow.</param>
    /// <returns>The options instance, so calls can be chained.</returns>
    public RobotsOptions Allow(string userAgent, params string[] paths)
    {
        GetOrAddRule(userAgent).Allow.AddRange(paths);
        return this;
    }

    private RobotsRule GetOrAddRule(string userAgent)
    {
        var rule = Rules.FirstOrDefault(r => string.Equals(r.UserAgent, userAgent, StringComparison.OrdinalIgnoreCase));
        if (rule is not null)
            return rule;

        rule = new RobotsRule { UserAgent = userAgent };
        Rules.Add(rule);
        return rule;
    }
}
