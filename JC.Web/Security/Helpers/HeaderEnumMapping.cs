using JC.Web.Security.Models;

namespace JC.Web.Security.Helpers;

internal static class HeaderEnumMapping
{
    #region Constants

    //XFrameOptions:
    private const string XFrame_Deny = "DENY";
    private const string XFrame_SameOrigin = "SAMEORIGIN";
    
    
    //ReferrerPolicy:
    private const string ReferrerPolicy_NoReferrer = "no-referrer";
    private const string ReferrerPolicy_NoReferrerWhenDowngrade = "no-referrer-when-downgrade";
    private const string ReferrerPolicy_Origin = "origin";
    private const string ReferrerPolicy_OriginWhenCrossOrigin = "origin-when-cross-origin";
    private const string ReferrerPolicy_SameOrigin = "same-origin";
    private const string ReferrerPolicy_StrictOrigin = "strict-origin";
    private const string ReferrerPolicy_StrictOriginWhenCrossOrigin = "strict-origin-when-cross-origin";
    private const string ReferrerPolicy_UnsafeUrl = "unsafe-url";

    
    //CrossOriginOpenerPolicy:
    private const string CrossOriginOpenerPolicy_UnsafeNone = "unsafe-none";
    private const string CrossOriginOpenerPolicy_SameOriginAllowPopups = "same-origin-allow-popups";
    private const string CrossOriginOpenerPolicy_SameOrigin = "same-origin";
    private const string CrossOriginOpenerPolicy_NoOpenerAllowPopups = "no-opener-allow-popups";
    
    
    //CrossOriginResourcePolicy:
    private const string CrossOriginResourcePolicy_SameSite = "same-site";
    private const string CrossOriginResourcePolicy_SameOrigin = "same-origin";
    private const string CrossOriginResourcePolicy_CrossOrigin = "cross-origin";
    
    
    //CrossOriginEmbedderPolicy:
    private const string CrossOriginEmbedderPolicy_UnsafeNone = "unsafe-none";
    private const string CrossOriginEmbedderPolicy_RequireCorp = "require-corp";
    private const string CrossOriginEmbedderPolicy_Credentialless = "credentialless";
    
    #endregion

    internal static string? GetXFrameOptions(XFrameOptionsMode? mode)
        => mode switch
        {
            XFrameOptionsMode.Deny => XFrame_Deny,
            XFrameOptionsMode.SameOrigin => XFrame_SameOrigin,
            _ => null
        };

    internal static string? GetReferrerPolicy(ReferrerPolicyMode? mode)
        => mode switch
        {
            ReferrerPolicyMode.NoReferrer => ReferrerPolicy_NoReferrer,
            ReferrerPolicyMode.NoReferrerWhenDowngrade => ReferrerPolicy_NoReferrerWhenDowngrade,
            ReferrerPolicyMode.Origin => ReferrerPolicy_Origin,
            ReferrerPolicyMode.OriginWhenCrossOrigin => ReferrerPolicy_OriginWhenCrossOrigin,
            ReferrerPolicyMode.SameOrigin => ReferrerPolicy_SameOrigin,
            ReferrerPolicyMode.StrictOrigin => ReferrerPolicy_StrictOrigin,
            ReferrerPolicyMode.StrictOriginWhenCrossOrigin => ReferrerPolicy_StrictOriginWhenCrossOrigin,
            ReferrerPolicyMode.UnsafeUrl => ReferrerPolicy_UnsafeUrl,
            _ => null
        };
    
    internal static string? GetCrossOriginOpenerPolicy(CrossOriginOpenerPolicyMode? mode)
        => mode switch
        {
            CrossOriginOpenerPolicyMode.UnsafeNone => CrossOriginOpenerPolicy_UnsafeNone,
            CrossOriginOpenerPolicyMode.SameOriginAllowPopups => CrossOriginOpenerPolicy_SameOriginAllowPopups,
            CrossOriginOpenerPolicyMode.SameOrigin => CrossOriginOpenerPolicy_SameOrigin,
            CrossOriginOpenerPolicyMode.NoOpenerAllowPopups => CrossOriginOpenerPolicy_NoOpenerAllowPopups,
            _ => null
        };

    internal static string? GetCrossOriginResourcePolicy(CrossOriginResourcePolicyMode? mode)
        => mode switch
        {
            CrossOriginResourcePolicyMode.SameSite => CrossOriginResourcePolicy_SameSite,
            CrossOriginResourcePolicyMode.SameOrigin => CrossOriginResourcePolicy_SameOrigin,
            CrossOriginResourcePolicyMode.CrossOrigin => CrossOriginResourcePolicy_CrossOrigin,
            _ => null
        };
    
    internal static string? GetCrossOriginEmbedderPolicy(CrossOriginEmbedderPolicyMode? mode)
        => mode switch
        {
            CrossOriginEmbedderPolicyMode.UnsafeNone => CrossOriginEmbedderPolicy_UnsafeNone,
            CrossOriginEmbedderPolicyMode.RequireCorp => CrossOriginEmbedderPolicy_RequireCorp,
            CrossOriginEmbedderPolicyMode.Credentialless => CrossOriginEmbedderPolicy_Credentialless,
            _ => null
        };
}