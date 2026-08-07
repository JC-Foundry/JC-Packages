namespace JC.Web.SEO.Models;

/// <summary>
/// Excludes a Razor Page model or controller from sitemap route discovery.
/// Has no effect on URLs supplied by an <see cref="Services.ISitemapUrlProvider"/> or added
/// explicitly through <see cref="Models.Options.SitemapOptions.Add"/>.
/// </summary>
/// <example>
/// <code>
/// [SitemapIgnore]
/// public class AdminDashboardModel : PageModel { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SitemapIgnoreAttribute : Attribute;
