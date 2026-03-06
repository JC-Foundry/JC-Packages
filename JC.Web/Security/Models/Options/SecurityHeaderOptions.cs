using JC.Web.Security.Helpers;

namespace JC.Web.Security.Models.Options;

/// <summary>
/// Configuration options for the security headers middleware.
/// Controls which HTTP security headers are added to responses and their values.
/// </summary>
public class SecurityHeaderOptions
{
    /// <summary>
    /// Whether to add the <c>X-Content-Type-Options: nosniff</c> header. Defaults to <c>true</c>.
    /// Prevents browsers from MIME-sniffing the content type.
    /// </summary>
    public bool EnableXContentTypeOptions { get; set; } = true;

    /// <summary>
    /// The <c>X-Frame-Options</c> header value. Defaults to <see cref="XFrameOptionsMode.SameOrigin"/>.
    /// Set to <c>null</c> to omit the header.
    /// </summary>
    public XFrameOptionsMode? XFrameOptions { get; set; } = XFrameOptionsMode.SameOrigin;

    /// <summary>
    /// The <c>Referrer-Policy</c> header value. Defaults to <see cref="ReferrerPolicyMode.StrictOriginWhenCrossOrigin"/>.
    /// Set to <c>null</c> to omit the header.
    /// </summary>
    public ReferrerPolicyMode? ReferrerPolicy { get; set; } = ReferrerPolicyMode.StrictOriginWhenCrossOrigin;

    /// <summary>
    /// The <c>Permissions-Policy</c> header value as a raw policy string.
    /// Defaults to disabling geolocation, microphone, and camera. Set to <c>null</c> to omit the header.
    /// </summary>
    public string? PermissionsPolicy { get; set; } = "geolocation=(), microphone=(), camera=()";

    /// <summary>
    /// The <c>Cross-Origin-Opener-Policy</c> header value. Defaults to <c>null</c> (not sent).
    /// </summary>
    public CrossOriginOpenerPolicyMode? CrossOriginOpenerPolicy { get; set; }

    /// <summary>
    /// The <c>Cross-Origin-Resource-Policy</c> header value. Defaults to <c>null</c> (not sent).
    /// </summary>
    public CrossOriginResourcePolicyMode? CrossOriginResourcePolicy { get; set; }

    /// <summary>
    /// The <c>Cross-Origin-Embedder-Policy</c> header value. Defaults to <c>null</c> (not sent).
    /// </summary>
    public CrossOriginEmbedderPolicyMode? CrossOriginEmbedderPolicy { get; set; }


    /// <summary>
    /// Whether to add the <c>Strict-Transport-Security</c> (HSTS) header on HTTPS responses. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// The HSTS <c>max-age</c> duration. Defaults to 180 days.
    /// </summary>
    public TimeSpan HstsMaxAge { get; set; } = TimeSpan.FromDays(180);

    /// <summary>
    /// Whether to include the <c>includeSubDomains</c> directive in the HSTS header. Defaults to <c>false</c>.
    /// </summary>
    public bool HstsIncludeSubDomains { get; set; }

    /// <summary>
    /// Whether HSTS is only applied in production environments. Defaults to <c>true</c>.
    /// </summary>
    public bool HstsProductionOnly { get; set; } = true;

    /// <summary>
    /// Whether to remove the <c>Server</c> response header to prevent server software disclosure. Defaults to <c>true</c>.
    /// </summary>
    public bool RemoveServerHeader { get; set; } = true;

    /// <summary>
    /// Whether to remove the <c>X-Powered-By</c> response header. Defaults to <c>true</c>.
    /// </summary>
    public bool RemoveXPoweredByHeader { get; set; } = true;

    /// <summary>
    /// Optional callback to configure the <c>Content-Security-Policy</c> header via <see cref="ContentSecurityPolicyBuilder"/>.
    /// When <c>null</c>, no CSP header is added.
    /// </summary>
    public Action<ContentSecurityPolicyBuilder>? ContentSecurityPolicy { get; set; }
}
