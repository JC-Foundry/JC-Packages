namespace JC.Identity.Extensions.Options;

/// <summary>
/// Configuration options for <see cref="JC.Identity.Middleware.IdentityMiddleware"/>,
/// controlling password change enforcement, 2FA enforcement, and route definitions.
/// </summary>
public class IdentityMiddlewareOptions
{
    /// <summary>Gets or sets whether users with <c>RequiresPasswordChange</c> are redirected. Defaults to <c>true</c>.</summary>
    public bool RequirePasswordChange { get; set; } = true;

    /// <summary>Gets or sets the route users are redirected to when a password change is required.</summary>
    public string ChangePasswordRoute { get; set; } = "/Identity/Account/Manage/SetPassword";

    /// <summary>Gets or sets whether all users without 2FA are redirected to configure it. Defaults to <c>false</c>.</summary>
    public bool EnforceTwoFactor { get; set; } = false;

    /// <summary>Gets or sets the route users are redirected to for 2FA setup.</summary>
    public string TwoFactorRoute { get; set; } = "/Identity/Account/Manage/EnableAuthenticator";

    /// <summary>Gets or sets the access denied route. Disabled users are redirected here.</summary>
    public string AccessDeniedRoute { get; set; } = "/Identity/Account/AccessDenied";

    /// <summary>Gets or sets the logout route.</summary>
    public string LogoutRoute { get; set; } = "/Identity/Account/Logout";

    /// <summary>Gets or sets the error route.</summary>
    public string ErrorRoute { get; set; } = "/Error";

    /// <summary>Gets the paths excluded from middleware enforcement (access denied, logout, and error routes).</summary>
    public string[] ExcludedPaths => [AccessDeniedRoute, LogoutRoute, ErrorRoute];
}