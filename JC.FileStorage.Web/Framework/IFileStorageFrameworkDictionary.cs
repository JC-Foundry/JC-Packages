using JC.Web.UI.Framework;

namespace JC.FileStorage.Web.Framework;

/// <summary>
/// The CSS class dictionary for JC.FileStorage.Web's tag helpers. One implementation exists per
/// supported <see cref="UIFramework"/>, and the configured framework decides which is resolved from
/// the container.
/// </summary>
/// <remarks>
/// This contract belongs to JC.FileStorage.Web, not JC.Web, so adding a tag helper here needs no
/// change to JC.Web and no JC.Web release. Register with <c>AddFrameworkDictionary</c>, which
/// selects the implementation from the same <see cref="UIFrameworkService.Framework"/> that drives
/// every other package's dictionary.
/// </remarks>
public interface IFileStorageFrameworkDictionary : IFrameworkDictionary
{
    /// <summary>Classes for the upload constraints help text.</summary>
    UploadConstraintsClasses UploadConstraints { get; }
}
