using System.Collections.Concurrent;
using JC.Web.Security.Models;

namespace JC.Web.Security.Services;

/// <summary>
/// Thread-safe registry of <see cref="CookieProfile"/> instances, keyed by cookie name.
/// Registered as a singleton and used by <see cref="Abstractions.ICookieService"/> implementations
/// to resolve cookie configuration (identity, encryption, and default overrides) by name.
/// Profiles are typically registered at startup and looked up per-request.
/// </summary>
public class CookieProfileDictionary
{
    private readonly ConcurrentDictionary<string, CookieProfile> _profiles = new();

    public bool TryCreateProfile(CookieProfile profile)
        => _profiles.TryAdd(profile.CookieName, profile);

    /// <summary>
    /// Registers an unencrypted cookie profile with optional default overrides.
    /// </summary>
    /// <param name="cookieName">The cookie name. Must not already be registered.</param>
    /// <param name="override">Optional overrides merged on top of global <see cref="Models.Options.CookieDefaultOptions"/>.</param>
    /// <returns><c>true</c> if the profile was registered; <c>false</c> if a profile with the same name already exists.</returns>
    public bool TryCreateProfile(string cookieName, CookieDefaultOverride? @override = null)
    {
        var profile = new CookieProfile(cookieName, @override);
        return TryCreateProfile(profile);
    }

    /// <summary>
    /// Registers an encrypted cookie profile with a Data Protection protector purpose and optional default overrides.
    /// </summary>
    /// <param name="cookieName">The cookie name. Must not already be registered.</param>
    /// <param name="protectorPurpose">The Data Protection protector purpose string for encryption/decryption.</param>
    /// <param name="override">Optional overrides merged on top of global <see cref="Models.Options.CookieDefaultOptions"/>.</param>
    /// <returns><c>true</c> if the profile was registered; <c>false</c> if a profile with the same name already exists.</returns>
    public bool TryCreateProfile(string cookieName, string protectorPurpose, CookieDefaultOverride? @override = null)
    {
        var profile = new CookieProfile(cookieName, protectorPurpose, @override);
        return TryCreateProfile(profile);
    }

    /// <summary>
    /// Retrieves the <see cref="CookieProfile"/> registered for the specified cookie name.
    /// </summary>
    /// <param name="cookieName">The cookie name to look up.</param>
    /// <returns>The registered profile, or <c>null</c> if no profile exists for the name.</returns>
    public CookieProfile? GetProfile(string cookieName)
    {
        var result = _profiles.TryGetValue(cookieName, out var profile);
        return result ? profile : null;
    }

    /// <summary>
    /// Atomically replaces the <see cref="CookieDefaultOverride"/> on an existing profile,
    /// preserving the cookie's name and encryption settings.
    /// </summary>
    /// <param name="cookieName">The cookie name of the profile to update.</param>
    /// <param name="override">The new override to apply.</param>
    /// <returns><c>true</c> if the profile was found and updated; <c>false</c> if no profile exists or the update lost a race.</returns>
    public bool TryUpdateProfileOverride(string cookieName, CookieDefaultOverride @override)
    {
        var profile = GetProfile(cookieName);
        if (profile == null) return false;

        var newProfile = new CookieProfile(profile, @override);
        return _profiles.TryUpdate(cookieName, newProfile, profile);
    }

    /// <summary>
    /// Removes the profile registered for the specified cookie name.
    /// </summary>
    /// <param name="cookieName">The cookie name of the profile to remove.</param>
    /// <returns><c>true</c> if the profile was found and removed; <c>false</c> if no profile exists or the removal lost a race.</returns>
    public bool TryRemoveProfile(string cookieName)
    {
        var profile = GetProfile(cookieName);
        if (profile == null) return false;

        var pair = new KeyValuePair<string, CookieProfile>(cookieName, profile);
        return _profiles.TryRemove(pair);
    }

    /// <summary>
    /// Checks whether a profile is registered for the specified cookie name.
    /// </summary>
    /// <param name="cookieName">The cookie name to check.</param>
    /// <returns><c>true</c> if a profile is registered; otherwise <c>false</c>.</returns>
    public bool HasProfile(string cookieName)
        => _profiles.TryGetValue(cookieName, out _);
}