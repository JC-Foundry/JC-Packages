namespace JC.CAP.Authentication;

/// <summary>The names JC.CAP registers with ASP.NET Core authentication and OpenIddict.</summary>
public static class CapDefaults
{
    /// <summary>The cookie scheme a CAP user is signed in on, and the application's default scheme.</summary>
    public const string AuthenticationScheme = "JC.CAP";

    /// <summary>The session cookie's name.</summary>
    public const string CookieName = ".JC.CAP.Session";

    /// <summary>The OpenIddict client registration for CAP. One authority, so one registration.</summary>
    public const string RegistrationId = "cap";

    /// <summary>The provider name OpenIddict reports for the CAP registration.</summary>
    public const string ProviderName = "CAP";

    /// <summary>The rule set name the identity middleware logs under.</summary>
    public const string RuleSetName = "CAP";
}
