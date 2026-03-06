using Microsoft.AspNetCore.Http;

namespace JC.Web.Security.Models;

/// <summary>
/// Identifies a cookie by name and optionally associates it with a Data Protection protector purpose.
/// Used by <see cref="Abstractions.ICookieService"/> implementations to read, write, and validate cookies.
/// </summary>
public class CookieSettings
{
    /// <summary>
    /// The name of the cookie.
    /// </summary>
    public string CookieName { get; }

    /// <summary>
    /// The Data Protection protector purpose string used for encryption/decryption.
    /// When set, the cookie is treated as encrypted. Must be non-empty for use with the encrypted cookie service.
    /// </summary>
    public string? ProtectorPurpose { get; }

    /// <summary>
    /// Indicates whether this cookie is configured for encryption (i.e. <see cref="ProtectorPurpose"/> is set).
    /// </summary>
    public bool IsEncrypted => !string.IsNullOrEmpty(ProtectorPurpose);

    /// <summary>
    /// Creates cookie settings for an unencrypted cookie.
    /// </summary>
    /// <param name="name">The cookie name. Must not be null, empty, or whitespace.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
    public CookieSettings(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cookie name must not be null, empty, or whitespace.", nameof(name));

        CookieName = name;
    }

    /// <summary>
    /// Creates cookie settings for an encrypted cookie with the specified Data Protection protector purpose.
    /// </summary>
    /// <param name="name">The cookie name. Must not be null, empty, or whitespace.</param>
    /// <param name="protectorPurpose">The Data Protection protector purpose string. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protectorPurpose"/> is null or empty.</exception>
    public CookieSettings(string name, string protectorPurpose)
        : this(name)
    {
        if (string.IsNullOrEmpty(protectorPurpose))
            throw new ArgumentNullException(nameof(protectorPurpose), $"You must provide a value for argument '{nameof(protectorPurpose)}'");

        ProtectorPurpose = protectorPurpose;
    }
}

/// <summary>
/// Selective overrides that are merged on top of the global <see cref="Options.CookieDefaultOptions"/>.
/// Only non-null properties are applied — anything left <c>null</c> falls back to the configured defaults.
/// </summary>
public class CookieDefaultOverride
{
    /// <summary>
    /// Override for <see cref="CookieOptions.HttpOnly"/>. When <c>null</c>, the global default is used.
    /// </summary>
    public bool? HttpOnly { get; set; }

    /// <summary>
    /// Override for <see cref="CookieOptions.Secure"/>. When <c>null</c>, the global default is used.
    /// </summary>
    public bool? Secure { get; set; }

    /// <summary>
    /// Override for <see cref="CookieOptions.SameSite"/>. When <c>null</c>, the global default is used.
    /// </summary>
    public SameSiteMode? SameSite { get; set; }

    /// <summary>
    /// Override for <see cref="CookieOptions.MaxAge"/>. When <c>null</c>, the global default is used.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Override for <see cref="CookieOptions.Path"/>. When <c>null</c>, the global default is used.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Override for <see cref="CookieOptions.Domain"/>. When <c>null</c>, the global default is used.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Override for <see cref="CookieOptions.Expires"/>. When <c>null</c>, the global default is used.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Override for <see cref="CookieOptions.IsEssential"/>. When <c>null</c>, the global default is used.
    /// </summary>
    public bool? IsEssential { get; set; }

    /// <summary>
    /// Creates an empty override. All properties default to <c>null</c> (use global defaults).
    /// </summary>
    public CookieDefaultOverride()
    {
    }

    /// <summary>
    /// Creates an override with the most commonly adjusted properties.
    /// </summary>
    /// <param name="sameSite">The SameSite mode override.</param>
    /// <param name="httpOnly">Optional HttpOnly override.</param>
    /// <param name="secure">Optional Secure override.</param>
    /// <param name="maxAge">Optional MaxAge override.</param>
    public CookieDefaultOverride(
        SameSiteMode sameSite,
        bool? httpOnly = null,
        bool? secure = null,
        TimeSpan? maxAge = null)
    {
        SameSite = sameSite;
        HttpOnly = httpOnly;
        Secure = secure;
        MaxAge = maxAge;
    }

    /// <summary>
    /// Creates a fully specified override.
    /// </summary>
    /// <param name="sameSite">The SameSite mode override.</param>
    /// <param name="httpOnly">The HttpOnly override.</param>
    /// <param name="secure">The Secure override.</param>
    /// <param name="path">The path override.</param>
    /// <param name="domain">Optional domain override.</param>
    /// <param name="maxAge">Optional MaxAge override.</param>
    /// <param name="expires">Optional absolute expiration override.</param>
    /// <param name="isEssential">Optional IsEssential override.</param>
    public CookieDefaultOverride(
        SameSiteMode sameSite,
        bool httpOnly,
        bool secure,
        string path,
        string? domain = null,
        TimeSpan? maxAge = null,
        DateTimeOffset? expires = null,
        bool? isEssential = null)
        : this(sameSite, httpOnly, secure, maxAge)
    {
        Path = path;
        Domain = domain;
        Expires = expires;
        IsEssential = isEssential;
    }
}
