using JC.Web.UI.Framework;

namespace JC.FileStorage.Web.Framework;

/// <summary>
/// jc-tailwind-ui classes for JC.FileStorage.Web's tag helpers. Selected when
/// <see cref="UIFramework.CustomJCTailwind"/> is configured.
/// </summary>
/// <remarks>
/// The framework borrows Bootstrap's class vocabulary and ships <c>form-text</c> as its own help-text
/// treatment, so this matches <see cref="BootstrapFileStorageDictionary"/> exactly. It exists so the
/// framework is a deliberate choice rather than a fallback to the Bootstrap dictionary — and so the
/// value can diverge later without a registration change.
/// <para>
/// Nothing needs declaring to Tailwind today, because <c>form-text</c> is an authored CSS rule in
/// that framework's bundle rather than a generated utility. Add a stock utility here and it does —
/// see <c>jc-filestorage.tailwind.css</c>.
/// </para>
/// </remarks>
public sealed class CustomJCTailwindFileStorageDictionary : IFileStorageFrameworkDictionary
{
    /// <inheritdoc />
    public UploadConstraintsClasses UploadConstraints { get; } = new()
    {
        Container = "form-text"
    };
}
