namespace JC.Web.SEO.Models.Options;

/// <summary>
/// Site-wide defaults applied by <see cref="Helpers.SeoBuilder"/> when a page does not supply
/// its own value. Bound from <c>Web:SEO:Meta</c> when an
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> is supplied at registration.
/// </summary>
public class SeoMetaOptions
{
    /// <summary>
    /// Configuration section bound by the <c>AddSeoMeta</c> overloads that take an
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    public const string ConfigSection = "Web:SEO:Meta";

    /// <summary>
    /// The site name, used for <c>og:site_name</c> and as the title suffix when
    /// <see cref="TitleSuffixSeparator"/> is set.
    /// </summary>
    public string? SiteName { get; set; }

    /// <summary>
    /// Separator placed between a page title and <see cref="SiteName"/>, for example <c>" | "</c>.
    /// When null the title is emitted exactly as supplied.
    /// </summary>
    public string? TitleSuffixSeparator { get; set; }

    /// <summary>
    /// Absolute base URL used to resolve relative canonical and image URLs. Canonical URLs and
    /// <c>og:image</c> should be absolute, so leaving this null on a page that supplies a
    /// relative path emits the relative value unchanged.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Fallback image for <c>og:image</c> and the Twitter card when a page supplies none.
    /// </summary>
    public string? DefaultImage { get; set; }

    /// <summary>
    /// Default description used when a page supplies none.
    /// </summary>
    public string? DefaultDescription { get; set; }

    /// <summary>
    /// Twitter <c>@handle</c> for the site, emitted as <c>twitter:site</c>.
    /// </summary>
    public string? TwitterSite { get; set; }

    /// <summary>
    /// Whether pages are indexable unless they say otherwise. Defaults to <c>true</c>.
    /// Set to <c>false</c> on staging so every page emits <c>noindex</c> by default.
    /// </summary>
    public bool DefaultIndex { get; set; } = true;

    /// <summary>
    /// Whether links are followed unless a page says otherwise. Defaults to <c>true</c>.
    /// </summary>
    public bool DefaultFollow { get; set; } = true;
}
