using JC.Web.Security.Helpers;

namespace JC.Web.Security.Models.Options;

public class SecurityHeaderOptions
{
    public bool EnableXContentTypeOptions { get; set; } = true;
    
    public XFrameOptionsMode? XFrameOptions { get; set; } = XFrameOptionsMode.SameOrigin;
    public ReferrerPolicyMode? ReferrerPolicy { get; set; } = ReferrerPolicyMode.StrictOriginWhenCrossOrigin;
    public string? PermissionsPolicy { get; set; } = "geolocation=(), microphone=(), camera=()";

    public CrossOriginOpenerPolicyMode? CrossOriginOpenerPolicy { get; set; } = CrossOriginOpenerPolicyMode.SameOrigin;
    public CrossOriginResourcePolicyMode? CrossOriginResourcePolicy { get; set; } = CrossOriginResourcePolicyMode.SameOrigin;
    public CrossOriginEmbedderPolicyMode? CrossOriginEmbedderPolicy { get; set; }
    
    
    // HSTS — wraps ASP.NET's UseHsts() into our pipeline
    public bool EnableHsts { get; set; } = true;
    public TimeSpan HstsMaxAge { get; set; } = TimeSpan.FromDays(180);
    public bool HstsIncludeSubDomains { get; set; } = false;
    public bool HstsProductionOnly { get; set; } = true;

    // Strip headers that leak server info
    public bool RemoveServerHeader { get; set; } = true;
    public bool RemoveXPoweredByHeader { get; set; } = true;

    // CSP — configured via the builder in Helpers/
    // null = no CSP header added
    public Action<ContentSecurityPolicyBuilder>? ContentSecurityPolicy { get; set; }
}