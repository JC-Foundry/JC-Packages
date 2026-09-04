using CAP.SSO.Models;
using JC.CAP.Authentication;
using JC.CAP.Enums;

namespace JC.CAP.Models.Options;

/// <summary>
/// How this application talks to CAP: the registration CAP's operator issued, the scopes to request,
/// the local paths the package serves, and how long a session lives.
/// </summary>
public class CapOptions
{
    /// <summary>
    /// The configuration section the <c>IConfiguration</c> overload of <c>AddCap</c> binds: CAP.SSO's root,
    /// so CAP and its clients read the SSO host from the same key.
    /// </summary>
    public const string ConfigSection = CapDictionary.ConfigSection;

    /// <summary>
    /// CAP's SSO host as an absolute URL, bound from <see cref="CapDictionary.BaseUrlKey"/>. The OIDC issuer,
    /// and the origin the discovery document is read from.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>The client id CAP allocated to this application.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The client secret CAP showed once. Keep it in user secrets or the environment, never in source.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The scopes requested at sign-in. Defaults to <c>openid</c>, <c>roles</c>, <c>cap_identity</c> and
    /// <c>offline_access</c>. Configuration binding adds to this set rather than replacing it.
    /// </summary>
    public HashSet<string> Scopes { get; } = new(StringComparer.Ordinal)
    {
        OIDC.Scopes.OpenId, OIDC.Scopes.Roles, OIDC.Scopes.CapIdentity, OIDC.Scopes.OfflineAccess
    };

    /// <summary>Where CAP returns the authorization code. Register the absolute form with CAP's operator.</summary>
    public string CallbackPath { get; set; } = CapEndpoints.CallbackPath;

    /// <summary>Where CAP returns after ending its session. Register the absolute form with CAP's operator.</summary>
    public string PostLogoutCallbackPath { get; set; } = CapEndpoints.PostLogoutCallbackPath;

    /// <summary>The local sign-in endpoint, and the cookie's login path.</summary>
    public string SignInPath { get; set; } = CapEndpoints.SignInPath;

    /// <summary>The local sign-out endpoint, and the cookie's logout path.</summary>
    public string SignOutPath { get; set; } = CapEndpoints.SignOutPath;

    /// <summary>The endpoint that re-reads the account from CAP on demand.</summary>
    public string RefreshPath { get; set; } = CapEndpoints.RefreshPath;

    /// <summary>Where the identity rules send a disabled account.</summary>
    public string DeniedPath { get; set; } = CapEndpoints.DeniedPath;

    /// <summary>Where the identity rules send an account owing two-factor enrolment.</summary>
    public string TwoFactorPath { get; set; } = CapEndpoints.TwoFactorPath;

    /// <summary>What a role refusal becomes. Defaults to <see cref="CapAccessDenied.Forbid"/>, a plain 403.</summary>
    public CapAccessDenied AccessDenied { get; set; } = CapAccessDenied.Forbid;

    /// <summary>
    /// The application's own page for a role refusal, used only when <see cref="AccessDenied"/> is
    /// <see cref="CapAccessDenied.LocalPath"/>. Receives the return URL.
    /// </summary>
    public string? AccessDeniedPath { get; set; }

    /// <summary>Lets the callback endpoints answer over plain http. Development only.</summary>
    public bool AllowInsecureHttp { get; set; }

    /// <summary>How the session cookie and its tokens behave.</summary>
    public CapSessionOptions Session { get; } = new();

    /// <summary>How long what is read from CAP's API is kept in memory.</summary>
    public CapCacheOptions Cache { get; } = new();
}

/// <summary>How long JC.CAP keeps what it reads from CAP's API.</summary>
public class CapCacheOptions
{
    /// <summary>Whether members read from CAP are cached at all. Defaults to <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How long the set of members is held before the next read refreshes it. Defaults to five minutes.</summary>
    public TimeSpan UserLifetime { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>The session JC.CAP keeps once CAP has signed a user in.</summary>
public class CapSessionOptions
{
    /// <summary>
    /// How long the cookie lives, sliding. Defaults to 14 days, CAP's refresh token lifetime; a session
    /// whose refresh CAP refuses ends sooner regardless.
    /// </summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(14);

    /// <summary>Whether the cookie survives the browser closing. Defaults to <c>false</c>.</summary>
    public bool Persistent { get; set; }

    /// <summary>How far ahead of access-token expiry a refresh is attempted. Defaults to one minute.</summary>
    public TimeSpan RefreshSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long past expiry a session survives when CAP cannot be reached. A refusal from CAP ends the
    /// session at once whatever this says. Defaults to five minutes.
    /// </summary>
    public TimeSpan RefreshFailureGrace { get; set; } = TimeSpan.FromMinutes(5);
}
