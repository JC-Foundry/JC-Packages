using JC.Web.UI.Framework;

namespace JC.FileStorage.Web.Framework;

/// <summary>
/// Bootstrap 5 class names for JC.FileStorage.Web's tag helpers. Selected when
/// <see cref="UIFramework.Bootstrap"/> is configured, which is the default.
/// </summary>
/// <remarks>
/// Every value here is the markup these tag helpers emitted before class names were made
/// configurable, so this dictionary reproduces the previous output exactly.
/// </remarks>
public sealed class BootstrapFileStorageDictionary : IFileStorageFrameworkDictionary
{
    /// <inheritdoc />
    public UploadConstraintsClasses UploadConstraints { get; } = new()
    {
        Container = "form-text"
    };
}
