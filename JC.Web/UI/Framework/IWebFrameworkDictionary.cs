namespace JC.Web.UI.Framework;

/// <summary>
/// The CSS class dictionary for JC.Web's own tag helpers and HTML builders. One implementation
/// exists per supported <see cref="UIFramework"/>, and the configured framework decides which is
/// resolved from the container.
/// </summary>
/// <remarks>
/// This covers JC.Web only. Packages layered on top declare their own contract deriving from
/// <see cref="IFrameworkDictionary"/> and register it with
/// <c>AddFrameworkDictionary</c>, so a new tag helper elsewhere never requires a change here.
/// </remarks>
public interface IWebFrameworkDictionary : IFrameworkDictionary
{
    /// <summary>Classes for alert components.</summary>
    AlertClasses Alert { get; }

    /// <summary>Classes for breadcrumb navigation.</summary>
    BreadcrumbClasses Breadcrumb { get; }

    /// <summary>Classes for pagination controls.</summary>
    PaginationClasses Pagination { get; }

    /// <summary>Classes for generated tables.</summary>
    TableClasses Table { get; }

    /// <summary>Classes for the bug reporter widget.</summary>
    BugReporterClasses BugReporter { get; }

    /// <summary>Classes for element states shared across components.</summary>
    StateClasses State { get; }
}
