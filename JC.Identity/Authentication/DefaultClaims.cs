namespace JC.Identity.Authentication;

public class DefaultClaims
{
    public const string EmailConfirmed = "email_confirmed";
    public const string PhoneNumber = "phone_number";
    public const string PhoneNumberConfirmed = "phone_number_confirmed";
    public const string TwoFactorEnabled = "two_factor_enabled";
    public const string LockoutEnabled = "lockout_enabled";
    public const string LockoutEnd = "lockout_end";
    public const string AccessFailedCount = "access_failed_count";
    
    public const string TenantId = "tenant_id";
    public const string DisplayName = "display_name";
    public const string LastLoginUtc = "last_login_utc";
    public const string IsEnabled = "is_enabled";
    
    public const string RequirePasswordChange = "require_password_change";
}