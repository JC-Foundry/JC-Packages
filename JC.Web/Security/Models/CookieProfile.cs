using Microsoft.AspNetCore.Http;

namespace JC.Web.Security.Models;

/// <summary>
/// Defines a cookie's identity, optional encryption configuration, and optional default overrides.
/// Registered in a <see cref="Services.CookieProfileDictionary"/> and resolved by cookie name
/// when <see cref="Abstractions.ICookieService"/> operations are performed.
/// </summary>
public class CookieProfile
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
    /// Represents optional overrides for the default cookie settings.
    /// These overrides are applied selectively to modify or replace global defaults.
    /// </summary>
    public CookieDefaultOverride? DefaultOverride { get; }

    /// <summary>
    /// Creates a profile for an unencrypted cookie with optional default overrides.
    /// </summary>
    /// <param name="cookieName">The cookie name. Must not be null, empty, or whitespace.</param>
    /// <param name="override">Optional overrides merged on top of the global <see cref="Options.CookieDefaultOptions"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cookieName"/> is null, empty, or whitespace.</exception>
    public CookieProfile(string cookieName, CookieDefaultOverride? @override = null)
    {
        if (string.IsNullOrWhiteSpace(cookieName))
            throw new ArgumentException("Cookie name must not be null, empty, or whitespace.", nameof(cookieName));

        CookieName = cookieName;
        DefaultOverride = @override;
    }

    /// <summary>
    /// Creates a profile for an encrypted cookie with the specified Data Protection protector purpose and optional default overrides.
    /// </summary>
    /// <param name="cookieName">The cookie name. Must not be null, empty, or whitespace.</param>
    /// <param name="protectorPurpose">The Data Protection protector purpose string. Must not be null or empty.</param>
    /// <param name="override">Optional overrides merged on top of the global <see cref="Options.CookieDefaultOptions"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cookieName"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protectorPurpose"/> is null or empty.</exception>
    public CookieProfile(string cookieName, string protectorPurpose, CookieDefaultOverride? @override = null)
        : this(cookieName, @override)
    {
        if (string.IsNullOrEmpty(protectorPurpose))
            throw new ArgumentNullException(nameof(protectorPurpose), $"You must provide a value for argument '{nameof(protectorPurpose)}'");

        ProtectorPurpose = protectorPurpose;
    }

    /// <summary>
    /// Creates a copy of an existing profile with a replacement <see cref="CookieDefaultOverride"/>.
    /// Used by <see cref="Services.CookieProfileDictionary.TryUpdateProfileOverride"/> to atomically swap overrides.
    /// </summary>
    /// <param name="profile">The existing profile to copy identity and encryption settings from.</param>
    /// <param name="override">The new override to apply.</param>
    public CookieProfile(CookieProfile profile, CookieDefaultOverride @override)
    {
        CookieName = profile.CookieName;
        ProtectorPurpose = profile.ProtectorPurpose;
        DefaultOverride = @override;
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
