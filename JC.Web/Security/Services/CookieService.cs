using JC.Web.Security.Abstractions;
using JC.Web.Security.Models;
using JC.Web.Security.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Web.Security.Services;

/// <summary>
/// Standard (unencrypted) implementation of <see cref="ICookieService"/>.
/// Reads and writes plain-text cookie values using the configured <see cref="CookieDefaultOptions"/>.
/// Logs a warning if <see cref="CookieSettings"/> has a <see cref="CookieSettings.ProtectorPurpose"/> set, as it will be ignored.
/// </summary>
public class CookieService(
    IHttpContextAccessor httpContextAccessor,
    IOptions<CookieDefaultOptions> defaults,
    ILogger<CookieService> logger) : ICookieService
{
    private readonly CookieDefaultOptions _defaults = defaults.Value;

    /// <inheritdoc />
    public void CreateCookie(string content, CookieSettings settings, CookieOptions? overrideOptions = null)
    {
        if (settings.IsEncrypted)
            logger.LogWarning(
                "CookieSettings for '{CookieName}' has a ProtectorPurpose set but is being used with the unencrypted CookieService. The ProtectorPurpose will be ignored.",
                settings.CookieName);

        var options = overrideOptions ?? _defaults.ToCookieOptions();
        var context = GetHttpContext();

        context.Response.Cookies.Append(settings.CookieName, content, options);
    }

    /// <inheritdoc />
    public string? GetCookie(CookieSettings settings)
    {
        var context = GetHttpContext();
        context.Request.Cookies.TryGetValue(settings.CookieName, out var value);
        return value;
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
    public void DeleteCookie(CookieSettings settings)
    {
        var context = GetHttpContext();
        context.Response.Cookies.Delete(settings.CookieName);
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
}
