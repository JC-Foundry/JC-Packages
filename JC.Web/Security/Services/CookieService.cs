using JC.Web.Security.Models;
using JC.Web.Security.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Web.Security.Services;

/// <summary>
/// Standard (unencrypted) implementation of <see cref="ICookieService"/>.
/// Reads and writes plain-text cookie values using the configured <see cref="CookieDefaultOptions"/>.
/// Resolves cookie configuration from the <see cref="CookieProfileDictionary"/> by cookie name.
/// Logs a warning if the resolved <see cref="CookieProfile"/> has a <see cref="CookieProfile.ProtectorPurpose"/> set, as it will be ignored.
/// </summary>
public class CookieService(
    CookieProfileDictionary profileDictionary,
    IHttpContextAccessor httpContextAccessor,
    IOptions<CookieDefaultOptions> defaults,
    ILogger<CookieService> logger) : ICookieService
{
    private readonly CookieDefaultOptions _defaults = defaults.Value;
    
    private HttpContext GetHttpContext() =>
        httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("HttpContext is not available.");
    
    
    /// <inheritdoc />
    public bool TryCreateCookie(string cookieName, string content)
    {
        var profile = profileDictionary.GetProfile(cookieName);
        if (profile == null) return false;
        
        if (profile.IsEncrypted)
            logger.LogWarning(
                "CookieSettings for '{CookieName}' has a ProtectorPurpose set but is being used with the unencrypted CookieService. The ProtectorPurpose will be ignored.",
                profile.CookieName);

        var options = _defaults.ToCookieOptions(profile.DefaultOverride);
        var context = GetHttpContext();

        context.Response.Cookies.Append(profile.CookieName, content, options);
        return true;
    }

    /// <inheritdoc />
    public string? GetCookie(string cookieName)
    {
        var profile = profileDictionary.GetProfile(cookieName);
        return profile == null ? null : GetCookie(profile);
    }

    private string? GetCookie(CookieProfile profile)
    {
        var context = GetHttpContext();
        context.Request.Cookies.TryGetValue(profile.CookieName, out var value);
        return value;
    }

    /// <inheritdoc />
    public CookieValidationResponse ValidateCookie(string cookieName, string expectedValue,
        StringComparison comparison = StringComparison.Ordinal)
    {
        var profile = profileDictionary.GetProfile(cookieName);
        if (profile == null) 
            return new CookieValidationResponse(false, null);
        
        var actualValue = GetCookie(profile);
        var isValid = actualValue is not null
                      && string.Equals(actualValue, expectedValue, comparison);

        return new CookieValidationResponse(isValid, actualValue);
    }

    /// <inheritdoc />
    public bool TryDeleteCookie(string cookieName)
    {
        var profile = profileDictionary.GetProfile(cookieName);
        if (profile == null) return false;
        
        var options = _defaults.ToCookieOptions(profile.DefaultOverride);
        var context = GetHttpContext();
        
        context.Response.Cookies.Delete(profile.CookieName, options);
        return true;
    }

    /// <inheritdoc />
    public bool CookieExists(string cookieName)
    {
        var profile = profileDictionary.GetProfile(cookieName);
        if (profile == null) return false;
        
        var context = GetHttpContext();
        return context.Request.Cookies.ContainsKey(profile.CookieName);
    }
}
