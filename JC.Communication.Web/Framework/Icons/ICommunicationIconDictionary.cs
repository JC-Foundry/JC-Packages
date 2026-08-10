using JC.Web.UI.Framework;
using JC.Web.UI.Framework.Icons;

namespace JC.Communication.Web.Framework.Icons;

/// <summary>
/// The icon class dictionary for JC.Communication.Web's tag helpers. One implementation exists per
/// supported <see cref="IconFramework"/>, and the configured icon set decides which is resolved from
/// the container.
/// </summary>
/// <remarks>
/// Separate from <see cref="ICommunicationFrameworkDictionary"/> because the two answer to different
/// choices — this one to <see cref="UIFrameworkService.IconFramework"/>, that one to
/// <see cref="UIFrameworkService.Framework"/>. An application on Tailwind may still use Bootstrap
/// Icons, so the pairing is not fixed.
/// <para>
/// Register with <c>AddIconDictionary</c>.
/// </para>
/// </remarks>
public interface ICommunicationIconDictionary : IIconDictionary
{
    /// <summary>The icons this package's tag helpers render.</summary>
    CommunicationIcons Icons { get; }
}
