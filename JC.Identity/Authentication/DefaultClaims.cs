namespace JC.Identity.Authentication;

/// <summary>
/// Defines the custom claim type constants used by the JC.Identity claims pipeline.
/// These claims are added to the <see cref="System.Security.Claims.ClaimsIdentity"/> by
/// <see cref="DefaultClaimsPrincipalFactory{TUser, TRole}"/> and read by <see cref="JC.Identity.Middleware.UserInfoMiddleware"/>.
/// </summary>
public class DefaultClaims
{
    /// <summary>Whether the user's email address has been confirmed.</summary>
    public const string EmailConfirmed = "email_confirmed";

    /// <summary>The user's phone number.</summary>
    public const string PhoneNumber = "phone_number";

    /// <summary>Whether the user's phone number has been confirmed.</summary>
    public const string PhoneNumberConfirmed = "phone_number_confirmed";

    /// <summary>Whether two-factor authentication is enabled.</summary>
    public const string TwoFactorEnabled = "two_factor_enabled";

    /// <summary>Whether account lockout is enabled.</summary>
    public const string LockoutEnabled = "lockout_enabled";

    /// <summary>The UTC date and time when the lockout period ends.</summary>
    public const string LockoutEnd = "lockout_end";

    /// <summary>The number of consecutive failed access attempts.</summary>
    public const string AccessFailedCount = "access_failed_count";

    /// <summary>The tenant identifier the user belongs to.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>The user's display name.</summary>
    public const string DisplayName = "display_name";

    /// <summary>The UTC date and time of the user's last login.</summary>
    public const string LastLoginUtc = "last_login_utc";

    /// <summary>Whether the user account is enabled.</summary>
    public const string IsEnabled = "is_enabled";

    /// <summary>Whether the user is required to change their password.</summary>
    public const string RequirePasswordChange = "require_password_change";
}