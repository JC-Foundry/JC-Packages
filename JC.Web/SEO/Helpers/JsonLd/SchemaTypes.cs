using System.Text.Json.Serialization;

namespace JC.Web.SEO.Helpers.JsonLd;

/// <summary>
/// An organisation, typically emitted once on the home page to describe the site owner.
/// </summary>
public sealed class OrganisationSchema : SchemaObject
{
    /// <inheritdoc />
    public override string Type => "Organization";

    /// <summary>The organisation's name.</summary>
    public string? Name { get; set; }

    /// <summary>The organisation's website URL.</summary>
    public string? Url { get; set; }

    /// <summary>Absolute URL of the organisation's logo.</summary>
    public string? Logo { get; set; }

    /// <summary>Contact telephone number.</summary>
    public string? Telephone { get; set; }

    /// <summary>URLs of official profiles elsewhere, used to link identities together.</summary>
    public List<string>? SameAs { get; set; }
}

/// <summary>
/// The website itself, describing the site rather than any single page.
/// </summary>
public sealed class WebSiteSchema : SchemaObject
{
    /// <inheritdoc />
    public override string Type => "WebSite";

    /// <summary>The site name.</summary>
    public string? Name { get; set; }

    /// <summary>An alternate or shortened name.</summary>
    public string? AlternateName { get; set; }

    /// <summary>The site's base URL.</summary>
    public string? Url { get; set; }
}

/// <summary>
/// A breadcrumb trail. Search engines use this to render the page's position in the site
/// hierarchy in place of a raw URL.
/// </summary>
public sealed class BreadcrumbListSchema : SchemaObject
{
    /// <inheritdoc />
    public override string Type => "BreadcrumbList";

    /// <summary>The ordered trail items.</summary>
    public List<BreadcrumbItem> ItemListElement { get; set; } = [];

    /// <summary>
    /// Adds an item to the trail, numbering it automatically from its position.
    /// </summary>
    /// <param name="name">The display name of this step.</param>
    /// <param name="url">Absolute URL of this step. Omit for the current page.</param>
    /// <returns>The schema instance, so calls can be chained.</returns>
    public BreadcrumbListSchema Add(string name, string? url = null)
    {
        ItemListElement.Add(new BreadcrumbItem
        {
            Position = ItemListElement.Count + 1,
            Name = name,
            Item = url
        });

        return this;
    }
}

/// <summary>
/// A single step within a <see cref="BreadcrumbListSchema"/>.
/// </summary>
public sealed class BreadcrumbItem
{
    /// <summary>The schema.org type name.</summary>
    [JsonPropertyName("@type")]
    [JsonPropertyOrder(-1)]
    public string Type => "ListItem";

    /// <summary>1-based position in the trail.</summary>
    public int Position { get; set; }

    /// <summary>The display name of this step.</summary>
    public string? Name { get; set; }

    /// <summary>Absolute URL of this step. Null for the current page, which needs no link.</summary>
    public string? Item { get; set; }
}

/// <summary>
/// An article or blog post.
/// </summary>
public sealed class ArticleSchema : SchemaObject
{
    /// <inheritdoc />
    public override string Type => "Article";

    /// <summary>The article headline.</summary>
    public string? Headline { get; set; }

    /// <summary>A short summary.</summary>
    public string? Description { get; set; }

    /// <summary>Absolute URLs of images associated with the article.</summary>
    public List<string>? Image { get; set; }

    /// <summary>When the article was first published.</summary>
    public DateTime? DatePublished { get; set; }

    /// <summary>When the article was last changed.</summary>
    public DateTime? DateModified { get; set; }

    /// <summary>The article's author.</summary>
    public PersonSchema? Author { get; set; }
}

/// <summary>
/// A person, used for article authorship.
/// </summary>
public sealed class PersonSchema : SchemaObject
{
    /// <inheritdoc />
    public override string Type => "Person";

    /// <summary>The person's name.</summary>
    public string? Name { get; set; }

    /// <summary>A URL identifying the person, such as a profile page.</summary>
    public string? Url { get; set; }
}

/// <summary>
/// A product, optionally carrying a price offer.
/// </summary>
public sealed class ProductSchema : SchemaObject
{
    /// <inheritdoc />
    public override string Type => "Product";

    /// <summary>The product name.</summary>
    public string? Name { get; set; }

    /// <summary>A description of the product.</summary>
    public string? Description { get; set; }

    /// <summary>Absolute URLs of product images.</summary>
    public List<string>? Image { get; set; }

    /// <summary>The stock keeping unit.</summary>
    public string? Sku { get; set; }

    /// <summary>The manufacturer or brand.</summary>
    public OrganisationSchema? Brand { get; set; }

    /// <summary>Pricing and availability.</summary>
    public OfferSchema? Offers { get; set; }
}

/// <summary>
/// A price offer attached to a <see cref="ProductSchema"/>.
/// </summary>
public sealed class OfferSchema : SchemaObject
{
    /// <inheritdoc />
    public override string Type => "Offer";

    /// <summary>The price, as a decimal value.</summary>
    public decimal? Price { get; set; }

    /// <summary>ISO 4217 currency code, for example <c>GBP</c>.</summary>
    public string? PriceCurrency { get; set; }

    /// <summary>
    /// Availability, as a schema.org URL such as <c>https://schema.org/InStock</c>.
    /// </summary>
    public string? Availability { get; set; }

    /// <summary>The URL where the product can be purchased.</summary>
    public string? Url { get; set; }
}
