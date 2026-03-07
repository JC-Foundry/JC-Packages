using JC.Web.Security.Models;
using JC.Web.Security.Models.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Web.Security.Services;

/// <summary>
/// Encrypted implementation of <see cref="ICookieService"/> using ASP.NET Core Data Protection.
/// Resolves cookie configuration from the <see cref="CookieProfileDictionary"/> by cookie name.
/// Cookie values are encrypted on write and decrypted on read using the <see cref="CookieProfile.ProtectorPurpose"/>
/// to create a scoped <see cref="IDataProtector"/> per operation.
/// Throws <see cref="ArgumentException"/> if the resolved <see cref="CookieProfile"/> does not have
/// <see cref="CookieProfile.IsEncrypted"/> set to <c>true</c>.
/// </summary>
public class EncryptedCookieService(
    CookieProfileDictionary profileDictionary,
    IHttpContextAccessor httpContextAccessor,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<CookieDefaultOptions> defaults,
    ILogger<EncryptedCookieService> logger) : ICookieService
{
    internal const string DataProtectionConfigKey = "Cookies:DataProtection_Path";
    private readonly CookieDefaultOptions _defaults = defaults.Value;

    private HttpContext GetHttpContext() =>
        httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("HttpContext is not available.");

    private static void EnsureEncrypted(CookieProfile profile)
    {
        if (!profile.IsEncrypted)
            throw new ArgumentException(
                $"CookieSettings for '{profile.CookieName}' must have a ProtectorPurpose when used with EncryptedCookieService.",
                nameof(profile));
    }
    
    
    /// <inheritdoc />
    public bool TryCreateCookie(string cookieName, string content)
    {
        var profile = profileDictionary.GetProfile(cookieName);
        if (profile == null) return false;
        
        EnsureEncrypted(profile);

        var protector = dataProtectionProvider.CreateProtector(profile.ProtectorPurpose!);
        var encryptedContent = protector.Protect(content);

        var options = _defaults.ToCookieOptions(profile.DefaultOverride);
        var context = GetHttpContext();

        context.Response.Cookies.Append(profile.CookieName, encryptedContent, options);
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
        EnsureEncrypted(profile);

        var context = GetHttpContext();
        if (!context.Request.Cookies.TryGetValue(profile.CookieName, out var encryptedValue))
            return null;

        try
        {
            var protector = dataProtectionProvider.CreateProtector(profile.ProtectorPurpose!);
            return protector.Unprotect(encryptedValue);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to decrypt cookie '{CookieName}'. The cookie may have been tampered with or the data protection key has changed.",
                profile.CookieName);
            return null;
        }
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
