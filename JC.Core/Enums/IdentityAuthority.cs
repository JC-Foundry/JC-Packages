using System.ComponentModel;

namespace JC.Core.Enums;

/// <summary>
/// Identifies which authority authenticated the current user and supplied their identity to the
/// consuming application. Exposed by <see cref="JC.Core.Models.IUserInfo.Authority"/>.
/// </summary>
/// <remarks>
/// This is not the login method. Somebody who signs into the Central Admin Portal with an external
/// provider and is then passed through to a consuming application has an authority of
/// <see cref="CAP"/> — the authority is whoever handed the application the identity, not however
/// that party established it in the first place.
/// </remarks>
public enum IdentityAuthority
{
    /// <summary>
    /// No authentication took place. The user info holds its system or unknown defaults, which is
    /// the expected state for unauthenticated requests and background work.
    /// </summary>
    [Description("No authentication was performed on this instance of the populated User Info object")]
    None,

    /// <summary>The application authenticated the user against its own persisted identity store.</summary>
    [Description("Authentication was performed using local persisted data for this instance of the populated User Info object")]
    Local,

    /// <summary>The Central Admin Portal authenticated the user and supplied the identity by SSO.</summary>
    [Description("Authentication was performed using CAP (Central Admin Portal) SSO for this instance of the populated User Info object")]
    CAP,

    /// <summary>An authentication mechanism the consuming application supplied itself.</summary>
    [Description("Authentication was performed using a custom authentication mechanism (outside of provided JC Packages) for this instance of the populated User Info object")]
    Custom
}
