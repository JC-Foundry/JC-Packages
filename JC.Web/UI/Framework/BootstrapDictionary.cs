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
    public StateClasses State { get; } = new()
    {
        Active = "active",
        Disabled = "disabled"
    };
}
