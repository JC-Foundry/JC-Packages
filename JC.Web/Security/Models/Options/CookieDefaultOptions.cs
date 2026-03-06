using Microsoft.AspNetCore.Http;

namespace JC.Web.Security.Models.Options;

/// <summary>
/// Global default options applied to all cookies created by <see cref="Abstractions.ICookieService"/> implementations.
/// These defaults are used unless explicitly overridden via the <c>overrideOptions</c> parameter on individual cookie operations.
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
    /// Converts these defaults into an ASP.NET Core <see cref="CookieOptions"/> instance.
    /// </summary>
    internal CookieOptions ToCookieOptions()
    {
        var options = new CookieOptions
        {
            HttpOnly = HttpOnly,
            Secure = Secure,
            SameSite = SameSite,
            Path = Path,
            IsEssential = IsEssential
        };

        if (MaxAge.HasValue)
            options.MaxAge = MaxAge.Value;

        if (Expires.HasValue)
            options.Expires = Expires.Value;

        if (!string.IsNullOrEmpty(Domain))
            options.Domain = Domain;

        return options;
    }
}
