using CAP.SSO.Endpoints;
using Microsoft.AspNetCore.WebUtilities;
using JC.CAP.Models.Options;
using Microsoft.Extensions.Options;

namespace JC.CAP.Services;

/// <summary>
/// The absolute, branded URLs into CAP's account surface, built from the issuer and client id the application
/// already holds. CAP.SSO supplies the paths; composing them onto the issuer needs this application's configuration.
/// </summary>
public class CapLinks
{
    private readonly Uri _issuer;
    private readonly string _clientId;

    public CapLinks(IOptions<CapOptions> options)
    {
        var baseUrl = options.Value.BaseUrl;

        // A trailing slash, so a host carrying a path segment is not truncated when combined.
        _issuer = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute);
        _clientId = options.Value.ClientId;
    }

    /// <summary>
    /// The account home: the applications this account can reach, with tabs to profile, security and
    /// personal data.
    /// </summary>
    public string Manage => For(SsoEndpoints.SsoManagePath);

    /// <summary>The account's own details: display name, email and phone.</summary>
    public string Profile => For(SsoEndpoints.SsoProfilePath);

    /// <summary>Password and two-factor.</summary>
    public string Security => For(SsoEndpoints.SsoSecurityPath);

    /// <summary>Download or delete the personal data CAP holds.</summary>
    public string PersonalData => For(SsoEndpoints.SsoPersonalDataPath);

    /// <summary>Enrol an authenticator.</summary>
    public string EnableAuthenticator => For(SsoEndpoints.SsoEnableAuthenticatorPath);

    /// <summary>The forced set-password screen.</summary>
    public string ForcedPassword => For(SsoEndpoints.SsoForcedPasswordPath);

    /// <summary>Self-registration. Meaningful only when CAP reports standard registration for this application.</summary>
    public string Register => For(SsoEndpoints.SsoRegisterPath);

    /// <summary>
    /// Self-registration, returning to <paramref name="returnUrl"/> once the account is confirmed and signed
    /// in. Without one the user is left on CAP's account pages, which have no way back to the application.
    /// </summary>
    /// <param name="returnUrl">See <see cref="For(string, string?)"/>.</param>
    public string RegisterReturningTo(string? returnUrl) => For(SsoEndpoints.SsoRegisterPath, returnUrl);

    /// <summary>Starts a password reset.</summary>
    public string ForgotPassword => For(SsoEndpoints.SsoForgotPasswordPath);

    /// <summary>Where a refused sign-in lands.</summary>
    public string Denied => For(SsoEndpoints.SsoDeniedPath);

    /// <summary>CAP's discovery document.</summary>
    public string Discovery => Absolute(ProtocolEndpoints.DiscoveryPath);

    /// <summary>Any <see cref="SsoEndpoints"/> route, branded for this application.</summary>
    public string For(string route) => Absolute(SsoEndpoints.ForApplication(route, _clientId));

    /// <summary>
    /// The same, carrying where CAP should send the user once it is done with them.
    /// </summary>
    /// <param name="route">An <see cref="SsoEndpoints"/> constant.</param>
    /// <param name="returnUrl">
    /// An absolute URL on an origin this application registered with CAP. CAP checks it against those on
    /// arrival and ignores anything it did not declare, so a value that reaches it by some other route
    /// cannot redirect a user off-site. Null or blank appends nothing.
    /// <para>
    /// It rides CAP's confirmation email, so it still works when that is opened later or on another device.
    /// Never pass an authorize request: that is bound to the browser which began it and expires in minutes.
    /// </para>
    /// </param>
    public string For(string route, string? returnUrl)
        => string.IsNullOrWhiteSpace(returnUrl)
            ? For(route)
            : QueryHelpers.AddQueryString(For(route), SsoEndpoints.ReturnUrlParameter, returnUrl);

    private string Absolute(string path) => new Uri(_issuer, path.TrimStart('/')).AbsoluteUri;
}
