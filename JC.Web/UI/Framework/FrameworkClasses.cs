using JC.Web.UI.HTML;

namespace JC.Web.UI.Framework;

/*
 * Every property below holds a complete class attribute value, not a single token — Bootstrap's
 * dismissible alert is "alert-dismissible fade show", three classes in one string.
 *
 * Values are also complete rather than compositional. ActiveItem carries "breadcrumb-item active"
 * rather than just the "active" modifier, because only Bootstrap builds its states by appending a
 * modifier to a base class. A framework whose active item shares nothing with its inactive one can
 * still express that here, which is the whole point of storing the finished value.
 *
 * Every property defaults to an empty string, so adding one is not a breaking change for existing
 * dictionaries — they simply render without it until updated. Adding a whole new group to a
 * dictionary contract is the breaking case, and that is far rarer.
 */

/// <summary>
/// Classes for alert components.
/// </summary>
public sealed record AlertClasses
{
    /// <summary>The alert container.</summary>
    public string Container { get; init; } = "";

    /// <summary>Added to the container when the alert can be dismissed.</summary>
    public string Dismissible { get; init; } = "";

    /// <summary>The dismiss button.</summary>
    public string CloseButton { get; init; } = "";

    /// <summary>The class for each alert type. Types absent from the map render without a variant class.</summary>
    public IReadOnlyDictionary<AlertType, string> Variants { get; init; }
        = new Dictionary<AlertType, string>();

    /// <summary>
    /// Returns the variant class for the given alert type, or an empty string when the dictionary
    /// does not define one.
    /// </summary>
    /// <param name="type">The alert type.</param>
    public string Variant(AlertType type) => Variants.GetValueOrDefault(type, "");
}

/// <summary>
/// Classes for breadcrumb navigation.
/// </summary>
public sealed record BreadcrumbClasses
{
    /// <summary>The wrapping navigation element.</summary>
    public string Nav { get; init; } = "";

    /// <summary>The list containing the trail.</summary>
    public string List { get; init; } = "";

    /// <summary>An ordinary trail item.</summary>
    public string Item { get; init; } = "";

    /// <summary>The final item, representing the current page.</summary>
    public string ActiveItem { get; init; } = "";
}

/// <summary>
/// Classes for pagination controls.
/// </summary>
public sealed record PaginationClasses
{
    /// <summary>The wrapping navigation element.</summary>
    public string Nav { get; init; } = "";

    /// <summary>The list containing the page items.</summary>
    public string List { get; init; } = "";

    /// <summary>An ordinary page item.</summary>
    public string Item { get; init; } = "";

    /// <summary>The item for the current page.</summary>
    public string ActiveItem { get; init; } = "";

    /// <summary>An item that cannot be navigated to, such as Previous on the first page.</summary>
    public string DisabledItem { get; init; } = "";

    /// <summary>The link inside a page item.</summary>
    public string Link { get; init; } = "";
}

/// <summary>
/// Classes for generated tables.
/// </summary>
public sealed record TableClasses
{
    /// <summary>The table element. Used when the caller supplies no explicit table class.</summary>
    public string Table { get; init; } = "";

    /// <summary>The table head.</summary>
    public string Head { get; init; } = "";

    /// <summary>The table body.</summary>
    public string Body { get; init; } = "";

    /// <summary>A body row.</summary>
    public string Row { get; init; } = "";

    /// <summary>A header cell.</summary>
    public string HeaderCell { get; init; } = "";

    /// <summary>A body cell.</summary>
    public string Cell { get; init; } = "";
}

/// <summary>
/// Classes for element states shared across components, applied by <see cref="HtmlTagBuilder"/>.
/// </summary>
public sealed record StateClasses
{
    /// <summary>Marks an element as the active or current one.</summary>
    public string Active { get; init; } = "";

    /// <summary>Marks an element as unavailable.</summary>
    public string Disabled { get; init; } = "";
}
