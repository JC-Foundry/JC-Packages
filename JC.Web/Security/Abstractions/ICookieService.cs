using JC.Web.Security.Models;
using JC.Web.Security.Services;
using Microsoft.AspNetCore.Http;
// ReSharper disable InconsistentNaming

namespace JC.Web.Security.Abstractions;

/// <summary>
/// Provides methods for creating, reading, and validating HTTP cookies.
/// Registered as a keyed service with two implementations:
/// <see cref="CookieService"/> (unencrypted) and <see cref="EncryptedCookieService"/> (Data Protection encrypted).
/// </summary>
public interface ICookieService
{
    /// <summary>
    /// Keyed service key for the standard (unencrypted) cookie service implementation.
    /// </summary>
    const string StandardCookieDIKey = nameof(CookieService);

    /// <summary>
    /// Keyed service key for the encrypted (Data Protection) cookie service implementation.
    /// </summary>
    const string EncryptedCookieDIKey = nameof(EncryptedCookieService);

    /// <summary>
    /// Creates a cookie with the specified content and settings.
    /// Uses <see cref="Models.Options.CookieDefaultOptions"/> as the baseline unless <paramref name="overrideOptions"/> is provided.
    /// </summary>
    /// <param name="content">The cookie value to store.</param>
    /// <param name="settings">The cookie name and optional encryption settings.</param>
    /// <param name="overrideOptions">Optional <see cref="CookieOptions"/> that fully replace the configured defaults.</param>
    void CreateCookie(
        string content,
        CookieSettings settings,
        CookieOptions? overrideOptions = null);

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
    /// Deletes a cookie from the response by setting an expired value.
    /// </summary>
    /// <param name="settings">The cookie name identifying which cookie to delete.</param>
    void DeleteCookie(CookieSettings settings);

    /// <summary>
    /// Checks whether a cookie exists in the current HTTP request.
    /// </summary>
    /// <param name="settings">The cookie name to check for.</param>
    /// <returns><c>true</c> if the cookie exists; otherwise <c>false</c>.</returns>
    bool CookieExists(CookieSettings settings);
}
