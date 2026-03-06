using Microsoft.AspNetCore.Http;

namespace JC.Web.Security.Models.Options;

/// <summary>
/// Global default options applied to all cookies created by <see cref="Abstractions.ICookieService"/> implementations.
/// These defaults are used as the baseline, with any <see cref="CookieDefaultOverride"/> properties merged on top.
/// </summary>
public class CookieDefaultOptions
{
    /// <summary>
    /// Whether cookies are inaccessible to client-side JavaScript. Defaults to <c>true</c>.
    /// </summary>
    public bool HttpOnly { get; set; } = true;

    /// <summary>
    /// Whether cookies are only sent over HTTPS. Defaults to <c>true</c>.
    /// </summary>
    public bool Secure { get; set; } = true;

    /// <summary>
    /// The SameSite attribute for cookies. Defaults to <see cref="SameSiteMode.Lax"/>.
    /// </summary>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;

    /// <summary>
    /// The maximum age of the cookie. When <c>null</c>, the cookie is a session cookie.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// The URL path the cookie is valid for. Defaults to <c>"/"</c>.
    /// </summary>
    public string? Path { get; set; } = "/";

    /// <summary>
    /// The domain the cookie is valid for. When <c>null</c>, defaults to the current request host.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// The absolute expiration date for the cookie. When <c>null</c>, <see cref="MaxAge"/> is used instead.
    /// If both are set, <see cref="MaxAge"/> takes precedence per the HTTP specification.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Whether the cookie is essential for the application to function and should bypass consent checks. Defaults to <c>false</c>.
    /// </summary>
    public bool IsEssential { get; set; }

    /// <summary>
    /// Builds a <see cref="CookieOptions"/> from these defaults, merging any non-null properties from the override on top.
    /// </summary>
    /// <param name="overrides">Optional overrides. Only non-null properties replace the defaults.</param>
    /// <returns>A fully populated <see cref="CookieOptions"/> instance.</returns>
    internal CookieOptions ToCookieOptions(CookieDefaultOverride? overrides = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = overrides?.HttpOnly ?? HttpOnly,
            Secure = overrides?.Secure ?? Secure,
            SameSite = overrides?.SameSite ?? SameSite,
            Path = overrides?.Path ?? Path,
            IsEssential = overrides?.IsEssential ?? IsEssential
        };

        var maxAge = overrides?.MaxAge ?? MaxAge;
        if (maxAge.HasValue)
            options.MaxAge = maxAge.Value;

        var expires = overrides?.Expires ?? Expires;
        if (expires.HasValue)
            options.Expires = expires.Value;

        var domain = overrides?.Domain ?? Domain;
        if (!string.IsNullOrEmpty(domain))
            options.Domain = domain;

        return options;
    }
}
