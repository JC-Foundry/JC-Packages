namespace JC.CAP.Authentication;

/// <summary>
/// The default local paths <c>MapCap</c> serves. Each is a default on <see cref="Models.Options.CapOptions"/>,
/// so read the option rather than the constant when building a link.
/// </summary>
public static class CapEndpoints
{
    /// <summary>Starts a sign-in by challenging CAP. Accepts <see cref="ReturnUrlParameter"/>.</summary>
    public const string LoginPath = "/cap/login";

    /// <summary>Ends the session here and at CAP. POST only, so a link cannot sign a visitor out.</summary>
    public const string LogoutPath = "/cap/logout";

    /// <summary>Re-reads the account from CAP now rather than at the next token expiry.</summary>
    public const string RefreshPath = "/cap/refresh";

    /// <summary>Where a disabled account is sent: re-checks with CAP, then hands over to CAP's denied page.</summary>
    public const string DeniedPath = "/cap/denied";

    /// <summary>Where an account owing two-factor is sent: re-checks with CAP, then hands over to enrolment.</summary>
    public const string TwoFactorPath = "/cap/two-factor";

    /// <summary>CAP returns the authorization code here. Matches the placeholder on CAP's settings page.</summary>
    public const string CallbackPath = "/signin-oidc";

    /// <summary>CAP returns here after ending its session.</summary>
    public const string PostLogoutCallbackPath = "/signout-callback-oidc";

    /// <summary>The query parameter naming a local URL to land on afterwards.</summary>
    public const string ReturnUrlParameter = "returnUrl";
}
