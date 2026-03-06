using JC.Web.Security.Abstractions;
using JC.Web.Security.Models;
using JC.Web.Security.Models.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Web.Security.Services;

/// <summary>
/// Encrypted implementation of <see cref="ICookieService"/> using ASP.NET Core Data Protection.
/// Cookie values are encrypted on write and decrypted on read using the <see cref="CookieSettings.ProtectorPurpose"/>
/// to create a scoped <see cref="IDataProtector"/> per operation.
/// Throws <see cref="ArgumentException"/> if <see cref="CookieSettings.IsEncrypted"/> is <c>false</c>.
/// </summary>
public class EncryptedCookieService(
    IHttpContextAccessor httpContextAccessor,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<CookieDefaultOptions> defaults,
    ILogger<EncryptedCookieService> logger) : ICookieService
{
    internal const string DataProtectionConfigKey = "Cookies:DataProtection-Path";
    private readonly CookieDefaultOptions _defaults = defaults.Value;
    
    /// <inheritdoc />
    public void CreateCookie(string content, CookieSettings settings, CookieDefaultOverride? overrides = null)
    {
        EnsureEncrypted(settings);

        var protector = dataProtectionProvider.CreateProtector(settings.ProtectorPurpose!);
        var encryptedContent = protector.Protect(content);

        var options = _defaults.ToCookieOptions(overrides);
        var context = GetHttpContext();

        context.Response.Cookies.Append(settings.CookieName, encryptedContent, options);
    }

    /// <inheritdoc />
    public string? GetCookie(CookieSettings settings)
    {
        EnsureEncrypted(settings);

        var context = GetHttpContext();
        if (!context.Request.Cookies.TryGetValue(settings.CookieName, out var encryptedValue))
            return null;

        try
        {
            var protector = dataProtectionProvider.CreateProtector(settings.ProtectorPurpose!);
            return protector.Unprotect(encryptedValue);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to decrypt cookie '{CookieName}'. The cookie may have been tampered with or the data protection key has changed.",
                settings.CookieName);
            return null;
        }
    }

    /// <inheritdoc />
    public CookieValidationResponse ValidateCookie(string expectedValue, CookieSettings settings, StringComparison comparison = StringComparison.Ordinal)
    {
        var actualValue = GetCookie(settings);
        var isValid = actualValue is not null
                      && string.Equals(actualValue, expectedValue, comparison);

        return new CookieValidationResponse(isValid, actualValue);
    }

    /// <inheritdoc />
    public void DeleteCookie(CookieSettings settings, CookieDefaultOverride? overrides = null)
    {
        var options = _defaults.ToCookieOptions(overrides);
        var context = GetHttpContext();
        context.Response.Cookies.Delete(settings.CookieName, options);
    }

    /// <inheritdoc />
    public bool CookieExists(CookieSettings settings)
    {
        var context = GetHttpContext();
        return context.Request.Cookies.ContainsKey(settings.CookieName);
    }

    private HttpContext GetHttpContext() =>
        httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("HttpContext is not available.");

    private static void EnsureEncrypted(CookieSettings settings)
    {
        if (!settings.IsEncrypted)
            throw new ArgumentException(
                $"CookieSettings for '{settings.CookieName}' must have a ProtectorPurpose when used with EncryptedCookieService.",
                nameof(settings));
    }
}
