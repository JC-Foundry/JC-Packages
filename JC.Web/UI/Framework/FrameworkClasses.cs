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
/// Classes for the floating bug reporter widget.
/// </summary>
/// <remarks>
/// The widget takes a contextual colour as a tag helper attribute, so the values it appears in are
/// stored as formats and read through the accessor methods below. See
/// <see cref="FrameworkClass.Format"/> for why the whole format lives here rather than the widget
/// appending a colour to a base class.
/// </remarks>
public sealed record BugReporterClasses
{
    /// <summary>The floating button that opens the widget.</summary>
    public string ToggleButton { get; init; } = "";

    /// <summary>The report panel. <c>{0}</c> is the configured colour.</summary>
    public string PanelFormat { get; init; } = "";

    /// <summary>The contextual colour used when the caller specifies none.</summary>
    public string DefaultColour { get; init; } = "";

    /// <summary>The panel's inner body.</summary>
    public string PanelBody { get; init; } = "";

    /// <summary>The panel heading. <c>{0}</c> is the configured colour.</summary>
    public string TitleFormat { get; init; } = "";

    /// <summary>The wrapper around a single form field.</summary>
    public string Field { get; init; } = "";

    /// <summary>A field label.</summary>
    public string Label { get; init; } = "";

    /// <summary>The report type select.</summary>
    public string Select { get; init; } = "";

    /// <summary>The description textarea.</summary>
    public string TextArea { get; init; } = "";

    /// <summary>Hides an element. Applied to the feedback area until there is something to say.</summary>
    public string Hidden { get; init; } = "";

    /// <summary>The row holding the cancel and submit buttons.</summary>
    public string Actions { get; init; } = "";

    /// <summary>The cancel button.</summary>
    public string CancelButton { get; init; } = "";

    /// <summary>The submit button. <c>{0}</c> is the configured colour.</summary>
    public string SubmitButtonFormat { get; init; } = "";

    /// <summary>
    /// The inline feedback message shown after submitting. <c>{0}</c> is the outcome — one of
    /// <c>success</c>, <c>warning</c> or <c>danger</c>.
    /// </summary>
    /// <remarks>
    /// Substituted in the browser rather than on the server, since the outcome is not known until
    /// the request completes. The format is emitted into the widget's script as-is.
    /// </remarks>
    public string FeedbackFormat { get; init; } = "";

    /// <summary>Returns the panel class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string Panel(string colour) => FrameworkClass.Format(PanelFormat, colour);

    /// <summary>Returns the heading class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string Title(string colour) => FrameworkClass.Format(TitleFormat, colour);

    /// <summary>Returns the submit button class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string SubmitButton(string colour) => FrameworkClass.Format(SubmitButtonFormat, colour);
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
