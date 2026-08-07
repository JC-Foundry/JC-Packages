using JC.Web.SEO.Models;

namespace JC.Web.SEO.Services;

/// <summary>
/// Supplies sitemap URLs that route discovery cannot resolve — typically database-backed content
/// behind a parameterised route such as <c>/products/{id}</c>, which discovery skips because it
/// has no way to know the values.
/// </summary>
/// <remarks>
/// Implementations are resolved per request and may depend on scoped services such as a DbContext.
/// Results are not cached, so every sitemap request re-runs every provider.
/// </remarks>
/// <example>
/// <code>
/// public class ProductSitemapProvider(AppDbContext db) : ISitemapUrlProvider
/// {
///     public async Task&lt;IEnumerable&lt;SitemapUrl&gt;&gt; GetUrlsAsync(CancellationToken ct) =>
///         await db.Products
///             .Where(p =&gt; p.IsPublished)
///             .Select(p =&gt; new SitemapUrl($"/products/{p.Slug}") { LastModified = p.UpdatedUtc })
///             .ToListAsync(ct);
/// }
/// </code>
/// </example>
public interface ISitemapUrlProvider
{
    /// <summary>
    /// Returns the URLs this provider contributes to the sitemap.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The URLs to include. Return an empty sequence rather than null when there are none.</returns>
    Task<IEnumerable<SitemapUrl>> GetUrlsAsync(CancellationToken cancellationToken = default);
}
