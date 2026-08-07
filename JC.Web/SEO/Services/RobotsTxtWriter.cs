using System.Text;
using JC.Web.SEO.Models.Options;

namespace JC.Web.SEO.Services;

/// <summary>
/// Renders robots.txt from the configured rules.
/// </summary>
internal static class RobotsTxtWriter
{
    /// <summary>
    /// Renders the file contents.
    /// </summary>
    /// <param name="options">The configured robots rules.</param>
    /// <param name="sitemapUrl">
    /// Absolute sitemap URL to advertise, or null when no sitemap is registered. A directive is
    /// never written for a sitemap that does not exist.
    /// </param>
    /// <returns>The robots.txt contents.</returns>
    public static string Write(RobotsOptions options, string? sitemapUrl)
    {
        var builder = new StringBuilder();

        // An empty file would leave crawler behaviour to interpretation, so fall back to an
        // explicit permissive group.
        var rules = options.Rules.Count > 0
            ? options.Rules
            : [new Models.RobotsRule { UserAgent = "*" }];

        foreach (var rule in rules)
        {
            builder.Append("User-agent: ").AppendLine(rule.UserAgent);

            foreach (var path in rule.Allow)
                builder.Append("Allow: ").AppendLine(path);

            foreach (var path in rule.Disallow)
                builder.Append("Disallow: ").AppendLine(path);

            if (rule.CrawlDelay.HasValue)
                builder.Append("Crawl-delay: ").AppendLine(rule.CrawlDelay.Value.ToString());

            builder.AppendLine();
        }

        if (options.IncludeSitemapDirective && !string.IsNullOrWhiteSpace(sitemapUrl))
            builder.Append("Sitemap: ").AppendLine(sitemapUrl);

        foreach (var additional in options.AdditionalSitemaps.Where(s => !string.IsNullOrWhiteSpace(s)))
            builder.Append("Sitemap: ").AppendLine(additional);

        return builder.ToString();
    }
}
