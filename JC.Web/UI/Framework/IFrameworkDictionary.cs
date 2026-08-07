namespace JC.Web.UI.Framework;

/// <summary>
/// Marker for a package's CSS class dictionary. Each package defines its own contract deriving
/// from this — JC.Web supplies <see cref="IWebFrameworkDictionary"/> for its own tag helpers and
/// builders, and packages layered on top (JC.Communication.Web and similar) declare theirs.
/// </summary>
/// <remarks>
/// Keeping a dictionary per package means adding a tag helper anywhere does not require touching
/// JC.Web. Every implementation is selected from the same
/// <see cref="UIFrameworkService.Framework"/>, so a single application-level choice still drives
/// all of them.
/// </remarks>
public interface IFrameworkDictionary;
