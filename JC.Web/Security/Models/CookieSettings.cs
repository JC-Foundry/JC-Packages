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
    /// <param name="name">The cookie name.</param>
    public CookieSettings(string name)
    {
        CookieName = name;
    }

    /// <summary>
    /// Creates cookie settings for an encrypted cookie with the specified Data Protection protector purpose.
    /// </summary>
    /// <param name="name">The cookie name.</param>
    /// <param name="protectorPurpose">The Data Protection protector purpose string. Must not be null or empty.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protectorPurpose"/> is null or empty.</exception>
    public CookieSettings(string name, string protectorPurpose)
        : this (name)
    {
        if (string.IsNullOrEmpty(protectorPurpose))
            throw new ArgumentNullException(nameof(protectorPurpose), $"You must provide a value for argument '{nameof(protectorPurpose)}'");

        ProtectorPurpose = protectorPurpose;
    }
}
