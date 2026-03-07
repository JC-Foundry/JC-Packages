using JC.Web.Security.Models;
using JC.Web.Security.Services;
// ReSharper disable InconsistentNaming

namespace JC.Web.Security.Services;

/// <summary>
/// Provides methods for creating, reading, deleting, and validating HTTP cookies.
/// All operations reference cookies by name, which must first be registered as a <see cref="CookieProfile"/>
/// in the <see cref="CookieProfileDictionary"/>. Operations against unregistered cookie names return
/// <c>false</c>, <c>null</c>, or an invalid <see cref="CookieValidationResponse"/>.
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
///         => cookies.TryCreateCookie("user-pref", value);
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
///         => encryptedCookies.TryCreateCookie("auth-token", token);
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
    /// Attempts to create a cookie with the specified name and content.
    /// The cookie must have a registered <see cref="CookieProfile"/> in the <see cref="CookieProfileDictionary"/>.
    /// Cookie options are resolved from the global <see cref="Models.Options.CookieDefaultOptions"/>,
    /// merged with any <see cref="CookieDefaultOverride"/> defined on the profile.
    /// </summary>
    /// <param name="cookieName">The name of the cookie, matching a registered <see cref="CookieProfile"/>.</param>
    /// <param name="content">The content to be stored in the cookie.</param>
    /// <returns><c>true</c> if the profile was found and the cookie was written; <c>false</c> if no profile is registered for the name.</returns>
    bool TryCreateCookie(
        string cookieName,
        string content);


    /// <summary>
    /// Retrieves the value of the cookie with the specified name.
    /// The cookie must have a registered <see cref="CookieProfile"/> in the <see cref="CookieProfileDictionary"/>.
    /// For encrypted cookies, the value is decrypted before being returned.
    /// </summary>
    /// <param name="cookieName">The name of the cookie, matching a registered <see cref="CookieProfile"/>.</param>
    /// <returns>The cookie value if the profile exists and the cookie is found; <c>null</c> if no profile is registered, the cookie does not exist, or decryption failed.</returns>
    string? GetCookie(string cookieName);


    /// <summary>
    /// Validates a cookie by comparing its value against the expected value.
    /// The cookie must have a registered <see cref="CookieProfile"/> in the <see cref="CookieProfileDictionary"/>.
    /// For encrypted cookies, the stored value is decrypted before comparison.
    /// </summary>
    /// <param name="cookieName">The name of the cookie, matching a registered <see cref="CookieProfile"/>.</param>
    /// <param name="expectedValue">The value expected to be found in the cookie.</param>
    /// <param name="comparison">The type of string comparison to use. Defaults to <see cref="StringComparison.Ordinal"/>.</param>
    /// <returns>A <see cref="CookieValidationResponse"/> with <c>IsValid = false</c> and <c>ActualValue = null</c> if no profile is registered; otherwise the comparison result and actual value.</returns>
    CookieValidationResponse ValidateCookie(
        string cookieName,
        string expectedValue,
        StringComparison comparison = StringComparison.Ordinal);


    /// <summary>
    /// Attempts to delete a cookie with the specified name.
    /// The cookie must have a registered <see cref="CookieProfile"/> in the <see cref="CookieProfileDictionary"/>.
    /// Uses the profile's <see cref="CookieDefaultOverride"/> (merged with global defaults) for path/domain matching.
    /// </summary>
    /// <param name="cookieName">The name of the cookie, matching a registered <see cref="CookieProfile"/>.</param>
    /// <returns><c>true</c> if the profile was found and the delete was issued; <c>false</c> if no profile is registered for the name.</returns>
    bool TryDeleteCookie(string cookieName);


    /// <summary>
    /// Checks whether a cookie with the specified name exists in the current request.
    /// The cookie must have a registered <see cref="CookieProfile"/> in the <see cref="CookieProfileDictionary"/>.
    /// </summary>
    /// <param name="cookieName">The name of the cookie, matching a registered <see cref="CookieProfile"/>.</param>
    /// <returns><c>true</c> if the profile exists and the cookie is present in the request; <c>false</c> if no profile is registered or the cookie is not found.</returns>
    bool CookieExists(string cookieName);
}
