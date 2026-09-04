using CAP.SSO.Endpoints;
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

    /// <summary>The account home: profile, security and personal data.</summary>
    public string Manage => For(SsoEndpoints.SsoManagePath);

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

    /// <summary>Starts a password reset.</summary>
    public string ForgotPassword => For(SsoEndpoints.SsoForgotPasswordPath);

    /// <summary>Where a refused sign-in lands.</summary>
    public string Denied => For(SsoEndpoints.SsoDeniedPath);

    /// <summary>CAP's discovery document.</summary>
    public string Discovery => Absolute(ProtocolEndpoints.DiscoveryPath);

    /// <summary>Any <see cref="SsoEndpoints"/> route, branded for this application.</summary>
    public string For(string route) => Absolute(SsoEndpoints.ForApplication(route, _clientId));

    private string Absolute(string path) => new Uri(_issuer, path.TrimStart('/')).AbsoluteUri;
}
