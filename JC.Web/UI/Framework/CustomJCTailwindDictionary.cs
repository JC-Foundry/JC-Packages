using JC.Web.UI.HTML;

namespace JC.Web.UI.Framework;

/// <summary>
/// jc-tailwind-ui classes. Selected when <see cref="UIFramework.CustomJCTailwind"/> is configured.
/// </summary>
/// <remarks>
/// The framework borrows Bootstrap's class vocabulary, so most of this matches
/// <see cref="BootstrapDictionary"/>. Colour is the exception: treatments name no colour and read a
/// <c>tone-{type}</c> class, so the contextual colour composes as <c>tone-{0}</c>. Unlike Bootstrap's
/// per-type shorthands, that works for any colour the application defines a tone for.
/// <para>
/// Two requirements: <see cref="Pagination"/> needs the opt-in <c>interactive</c> layer, and the few
/// stock Tailwind utilities used below must be declared —
/// <c>@source inline("{hidden,flex,justify-between,font-semibold,mb-2,py-1,px-2,text-sm}")</c>,
/// plus <c>print:hidden</c> and <c>text-[var(--t-l)]</c>. The framework's own classes are always in
/// the bundle and need nothing.
/// </para>
/// </remarks>
public sealed class CustomJCTailwindDictionary : IWebFrameworkDictionary
{
    /// <inheritdoc />
    public AlertClasses Alert { get; } = new()
    {
        Container = "alert",

        // No fade/show — in Bootstrap those are JS animation hooks, not layout.
        Dismissible = "alert-dismissible",

        // btn-close draws its own × via ::before, so the empty button renders correctly.
        CloseButton = "btn-close",

        // alert-accent is the tone-reading treatment; the tone supplies the colour.
        Variants = new Dictionary<AlertType, string>
        {
            [AlertType.Success] = "alert-accent tone-success",
            [AlertType.Warning] = "alert-accent tone-warning",
            [AlertType.Error] = "alert-accent tone-danger",
            [AlertType.Info] = "alert-accent tone-info"
        }
    };

    /// <inheritdoc />
    public BreadcrumbClasses Breadcrumb { get; } = new()
    {
        // Styled on the list rather than the nav, as in Bootstrap. Separator is drawn by the CSS.
        List = "breadcrumb",
        Item = "breadcrumb-item",
        ActiveItem = "breadcrumb-item active"
    };

    /// <inheritdoc />
    /// <remarks>Identical to Bootstrap, but this component is in the opt-in interactive layer.</remarks>
    public PaginationClasses Pagination { get; } = new()
    {
        List = "pagination",
        Item = "page-item",
        ActiveItem = "page-item active",
        DisabledItem = "page-item disabled",
        Link = "page-link"
    };

    /// <inheritdoc />
    public TableClasses Table { get; } = new()
    {
        // .table styles its own thead/th/td, so the structural entries stay empty.
        Table = "table"
    };

    /// <inheritdoc />
    /// <remarks>
    /// The colour is a tone name. The panel sets it and custom properties inherit, so the title and
    /// buttons inside pick it up without repeating it.
    /// </remarks>
    public BugReporterClasses BugReporter { get; } = new()
    {
        ToggleButton = "print:hidden",
        PanelFormat = "card card-accent tone-{0} print:hidden",
        DefaultColour = "danger",

        // .card pads itself and there is no card-body.
        PanelBody = "",

        // No card-title either; --t-l is the tone's light shade.
        TitleFormat = "mb-2 font-semibold text-[var(--t-l)]",
        Field = "form-group",
        Label = "form-label",
        Select = "form-select",
        TextArea = "form-control form-control-sm",
        Hidden = "hidden",
        Actions = "flex justify-between",
        CancelButton = "btn btn-outline btn-sm tone-secondary",
        SubmitButtonFormat = "btn btn-solid btn-sm tone-{0}",

        // Substituted in the browser with success/warning/danger — each a built-in tone.
        FeedbackFormat = "alert alert-accent tone-{0} py-1 px-2 mb-2 text-sm"
    };

    /// <inheritdoc />
    public StateClasses State { get; } = new()
    {
        Active = "active",
        Disabled = "disabled"
    };
}
