using JC.Web.Security.Models;
using JC.Web.Security.Services;
using Microsoft.AspNetCore.Http;
// ReSharper disable InconsistentNaming

namespace JC.Web.Security.Abstractions;

/// <summary>
/// Provides methods for creating, reading, deleting, and validating HTTP cookies.
/// <para>
/// <b>Registration:</b> Use <c>AddCookieServices</c> to register cookie services.
/// The registration behaviour depends on whether encrypted cookies are enabled:
/// </para>
/// <para>
/// <b>Unencrypted only</b> (<c>useEncryptedCookies: false</c>):<br/>
/// The standard <see cref="CookieService"/> is registered as a plain (non-keyed) service.
/// Inject directly via <see cref="ICookieService"/>:
/// </para>
/// <example>
/// <code>
/// // Registration
/// builder.Services.AddCookieServices(builder.Configuration, useEncryptedCookies: false);
///
/// // Injection
/// public class MyService(ICookieService cookies)
/// {
///     public void SetPreference(string value)
///         => cookies.CreateCookie(value, new CookieSettings("user-pref"));
/// }
/// </code>
/// </example>
/// <para>
/// <b>With encryption</b> (default, <c>useEncryptedCookies: true</c>):<br/>
/// Both implementations are registered as <b>keyed services</b>. Use <see cref="StandardCookieDIKey"/>
/// and <see cref="EncryptedCookieDIKey"/> with the <c>[FromKeyedServices]</c> attribute to inject the desired implementation.
/// Requires the <c>Cookies:DataProtection_Path</c> configuration key.
/// </para>
/// <example>
/// <code>
/// // Registration
/// builder.Services.AddCookieServices(builder.Configuration);
///
/// // appsettings.json
/// // { "Cookies": { "DataProtection_Path": "/app/keys" } }
///
/// // Injection — unencrypted
/// public class PreferenceService(
///     [FromKeyedServices(ICookieService.StandardCookieDIKey)] ICookieService cookies)
///
/// // Injection — encrypted
/// public class AuthTokenService(
///     [FromKeyedServices(ICookieService.EncryptedCookieDIKey)] ICookieService encryptedCookies)
/// {
///     public void SetToken(string token)
///         => encryptedCookies.CreateCookie(token, new CookieSettings("auth-token", "AuthPurpose"));
/// }
/// </code>
/// </example>
/// </summary>
public interface ICookieService
{
    /// <summary>
    /// Keyed service key for the standard (unencrypted) cookie service implementation.
    /// Used with <c>[FromKeyedServices(ICookieService.StandardCookieDIKey)]</c> when both
    /// encrypted and unencrypted services are registered.
    /// </summary>
    const string StandardCookieDIKey = nameof(CookieService);

    /// <summary>
    /// Keyed service key for the encrypted (Data Protection) cookie service implementation.
    /// Used with <c>[FromKeyedServices(ICookieService.EncryptedCookieDIKey)]</c> to inject
    /// the <see cref="EncryptedCookieService"/>.
    /// </summary>
    const string EncryptedCookieDIKey = nameof(EncryptedCookieService);

    /// <summary>
    /// Creates a cookie with the specified content and settings.
    /// Uses <see cref="Models.Options.CookieDefaultOptions"/> as the baseline.
    /// Any non-null properties in <paramref name="overrides"/> are merged on top of the defaults.
    /// </summary>
    /// <param name="content">The cookie value to store.</param>
    /// <param name="settings">The cookie name and optional encryption settings.</param>
    /// <param name="overrides">Optional overrides merged on top of the configured defaults. Only non-null properties are applied.</param>
    void CreateCookie(
        string content,
        CookieSettings settings,
        CookieDefaultOverride? overrides = null);

    /// <summary>
    /// Reads a cookie value by name from the current HTTP request.
    /// For the encrypted implementation, the value is decrypted before being returned.
    /// </summary>
    /// <param name="settings">The cookie name and optional encryption settings.</param>
    /// <returns>The cookie value, or <c>null</c> if the cookie does not exist or decryption fails.</returns>
    string? GetCookie(CookieSettings settings);

    /// <summary>
    /// Reads a cookie and validates its value against an expected string.
    /// </summary>
    /// <param name="expectedValue">The value to compare the cookie against.</param>
    /// <param name="settings">The cookie name and optional encryption settings.</param>
    /// <param name="comparison">The string comparison type. Defaults to <see cref="StringComparison.Ordinal"/>.</param>
    /// <returns>A <see cref="CookieValidationResponse"/> indicating whether the values match and the actual cookie value.</returns>
    CookieValidationResponse ValidateCookie(
        string expectedValue,
        CookieSettings settings,
        StringComparison comparison = StringComparison.Ordinal);

    /// <summary>
    /// Deletes a cookie from the response. Uses the configured default path and domain to ensure
    /// the deletion targets the correct cookie. Optional overrides can be provided to match
    /// non-default path/domain values.
    /// </summary>
    /// <param name="settings">The cookie name identifying which cookie to delete.</param>
    /// <param name="overrides">Optional overrides to match the path/domain the cookie was created with.</param>
    void DeleteCookie(CookieSettings settings, CookieDefaultOverride? overrides = null);

    /// <summary>
    /// Checks whether a cookie exists in the current HTTP request.
    /// </summary>
    /// <param name="settings">The cookie name to check for.</param>
    /// <returns><c>true</c> if the cookie exists; otherwise <c>false</c>.</returns>
    bool CookieExists(CookieSettings settings);
}
