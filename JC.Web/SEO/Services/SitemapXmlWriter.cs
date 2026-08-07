using System.Globalization;
using System.Xml.Linq;
using JC.Web.SEO.Models;

namespace JC.Web.SEO.Services;

/// <summary>
/// Renders sitemap XML. Built with <see cref="XDocument"/> rather than string concatenation so
/// that reserved characters are escaped — an unescaped <c>&amp;</c> from a query string produces
/// a malformed document that crawlers reject outright.
/// </summary>
internal static class SitemapXmlWriter
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>
    /// Renders a <c>urlset</c> document for the supplied URLs.
    /// </summary>
    /// <param name="urls">The URLs to include.</param>
    /// <param name="baseUrl">Absolute base URL used to resolve site-relative locations.</param>
    /// <returns>The sitemap XML.</returns>
    public static string WriteUrlSet(IEnumerable<SitemapUrl> urls, string baseUrl)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "urlset",
                urls.Select(url => BuildUrlElement(url, baseUrl))));

        return Render(document);
    }

    /// <summary>
    /// Renders a <c>sitemapindex</c> document pointing at the numbered sitemap files.
    /// </summary>
    /// <param name="fileCount">How many numbered files exist.</param>
    /// <param name="baseUrl">Absolute base URL used to resolve the file paths.</param>
    /// <param name="sitemapPath">The configured sitemap path, used to derive the numbered file names.</param>
    /// <param name="lastModified">Timestamp reported for each file.</param>
    /// <returns>The sitemap index XML.</returns>
    public static string WriteIndex(int fileCount, string baseUrl, string sitemapPath, DateTime lastModified)
    {
        var entries = Enumerable.Range(1, fileCount)
            .Select(index => new XElement(Ns + "sitemap",
                new XElement(Ns + "loc", ToAbsolute(baseUrl, BuildChunkPath(sitemapPath, index))),
                new XElement(Ns + "lastmod", FormatTimestamp(lastModified))));

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "sitemapindex", entries));

        return Render(document);
    }

    /// <summary>
    /// Derives the path of a numbered sitemap file — <c>/sitemap.xml</c> becomes <c>/sitemap-2.xml</c>.
    /// </summary>
    public static string BuildChunkPath(string sitemapPath, int index)
    {
        var extension = Path.GetExtension(sitemapPath);
        var withoutExtension = string.IsNullOrEmpty(extension)
            ? sitemapPath
            : sitemapPath[..^extension.Length];

        return $"{withoutExtension}-{index}{extension}";
    }

    private static XElement BuildUrlElement(SitemapUrl url, string baseUrl)
    {
        var element = new XElement(Ns + "url",
            new XElement(Ns + "loc", ToAbsolute(baseUrl, url.Location)));

        if (url.LastModified.HasValue)
            element.Add(new XElement(Ns + "lastmod", FormatTimestamp(url.LastModified.Value)));

        if (url.ChangeFrequency.HasValue)
            element.Add(new XElement(Ns + "changefreq",
                url.ChangeFrequency.Value.ToString().ToLowerInvariant()));

        if (url.Priority.HasValue)
        {
            // Out-of-range values invalidate the whole document, so clamp rather than emit them.
            var priority = Math.Clamp(url.Priority.Value, 0.0, 1.0);

            // Invariant culture is essential — a comma decimal separator is not a valid priority.
            element.Add(new XElement(Ns + "priority",
                priority.ToString("0.0", CultureInfo.InvariantCulture)));
        }

        return element;
    }

    /// <summary>
    /// Formats a timestamp as W3C datetime. Midnight values are written as a plain date, which is
    /// the more honest representation when only the day is actually known.
    /// </summary>
    private static string FormatTimestamp(DateTime value)
        => value.TimeOfDay == TimeSpan.Zero
            ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : DateTime.SpecifyKind(value, DateTimeKind.Utc)
                .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string ToAbsolute(string baseUrl, string location)
        => Uri.TryCreate(location, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : $"{baseUrl.TrimEnd('/')}/{location.TrimStart('/')}";

    private static string Render(XDocument document)
        => document.Declaration + Environment.NewLine + document;
}
