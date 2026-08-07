using JC.Web.UI.HTML;

namespace JC.Web.UI.Framework;

/// <summary>
/// Bootstrap 5 class names for JC.Web's tag helpers and HTML builders. Selected when
/// <see cref="UIFramework.Bootstrap"/> is configured, which is the default.
/// </summary>
/// <remarks>
/// Every value here is the markup JC.Web emitted before class names were made configurable, so
/// this dictionary reproduces the previous output exactly.
/// </remarks>
public sealed class BootstrapDictionary : IWebFrameworkDictionary
{
    /// <inheritdoc />
    public AlertClasses Alert { get; } = new()
    {
        Container = "alert",
        Dismissible = "alert-dismissible fade show",
        CloseButton = "btn-close",
        Variants = new Dictionary<AlertType, string>
        {
            [AlertType.Success] = "alert-success",
            [AlertType.Warning] = "alert-warning",
            [AlertType.Error] = "alert-danger",
            [AlertType.Info] = "alert-info"
        }
    };

    /// <inheritdoc />
    public BreadcrumbClasses Breadcrumb { get; } = new()
    {
        // Bootstrap styles the list rather than the nav, so Nav is deliberately empty.
        List = "breadcrumb",
        Item = "breadcrumb-item",
        ActiveItem = "breadcrumb-item active"
    };

    /// <inheritdoc />
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
        Table = "table"
    };

    /// <inheritdoc />
    public BugReporterClasses BugReporter { get; } = new()
    {
        ToggleButton = "d-print-none",
        PanelFormat = "card border-{0} d-print-none",
        DefaultColour = "danger",
        PanelBody = "card-body p-3",
        TitleFormat = "card-title text-{0}",
        Field = "mb-2",
        Label = "form-label",
        Select = "form-select form-select-sm",
        TextArea = "form-control form-control-sm",
        Hidden = "d-none",
        Actions = "d-flex justify-content-between",
        CancelButton = "btn btn-sm btn-outline-secondary",
        SubmitButtonFormat = "btn btn-sm btn-{0}",
        FeedbackFormat = "alert alert-{0} py-1 px-2 mb-2 small"
    };

    /// <inheritdoc />
    public StateClasses State { get; } = new()
    {
        Active = "active",
        Disabled = "disabled"
    };
}
