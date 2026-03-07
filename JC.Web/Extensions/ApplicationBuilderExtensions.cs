using JC.Web.Security.Middleware;
using JC.Web.Security.Models;
using JC.Web.Security.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace JC.Web.Extensions;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> providing JC.Web middleware registration.
/// </summary>
public static class ApplicationBuilderExtensions
{
    

    #region Security

    /// <summary>
    /// Adds the security headers middleware to the request pipeline.
    /// Must be called after <see cref="ServiceCollectionExtensions.AddSecurityHeaders"/>.
    /// Place early in the pipeline to ensure headers are applied to all responses.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeaderMiddleware>();
        return app;
    }

    /// <summary>
    /// Registers one or more unencrypted cookie profiles from tuples at startup.
    /// Each tuple is used to construct a <see cref="CookieProfile"/> — construction will throw
    /// if the cookie name is null, empty, or whitespace.
    /// Must be called after <see cref="ServiceCollectionExtensions.AddCookieServices"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="standardCookies">
    /// One or more tuples of cookie name and optional <see cref="CookieDefaultOverride"/>.
    /// </param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if any cookie name is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="CookieProfileDictionary"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a profile already exists for any cookie name.</exception>
    public static IApplicationBuilder PopulateStandardCookieProfiles(this IApplicationBuilder app,
        params IEnumerable<(string CookieName, CookieDefaultOverride? Override)> standardCookies)
    {
        app.PopulateCookieProfiles(standardCookies, []);
        return app;
    }

    /// <summary>
    /// Registers one or more encrypted cookie profiles from tuples at startup.
    /// Each tuple is used to construct a <see cref="CookieProfile"/> — construction will throw
    /// if the cookie name is null/empty/whitespace or the protector purpose is null/empty.
    /// Must be called after <see cref="ServiceCollectionExtensions.AddCookieServices"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="encryptedCookies">
    /// One or more tuples of cookie name, Data Protection protector purpose string,
    /// and optional <see cref="CookieDefaultOverride"/>.
    /// </param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if any cookie name is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if any protector purpose is null/empty, or if <see cref="CookieProfileDictionary"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a profile already exists for any cookie name.</exception>
    public static IApplicationBuilder PopulateEncryptedCookieProfiles(this IApplicationBuilder app,
        params IEnumerable<(string CookieName, string ProtectorPurpose, CookieDefaultOverride? Override)> encryptedCookies)
    {
        app.PopulateCookieProfiles([], encryptedCookies);
        return app;
    }

    /// <summary>
    /// Registers both unencrypted and encrypted cookie profiles from tuples at startup.
    /// Each tuple is used to construct a <see cref="CookieProfile"/> — construction will throw
    /// if required values are missing (null/empty cookie name, or null/empty protector purpose for encrypted cookies).
    /// Must be called after <see cref="ServiceCollectionExtensions.AddCookieServices"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="standardCookies">
    /// Tuples of cookie name and optional <see cref="CookieDefaultOverride"/> for unencrypted profiles.
    /// </param>
    /// <param name="encryptedCookies">
    /// Tuples of cookie name, Data Protection protector purpose string,
    /// and optional <see cref="CookieDefaultOverride"/> for encrypted profiles.
    /// </param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if any cookie name is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if any protector purpose is null/empty, or if <see cref="CookieProfileDictionary"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a profile already exists for any cookie name.</exception>
    public static IApplicationBuilder PopulateCookieProfiles(this IApplicationBuilder app,
        IEnumerable<(string CookieName, CookieDefaultOverride? Override)> standardCookies,
        IEnumerable<(string CookieName, string ProtectorPurpose, CookieDefaultOverride? Override)> encryptedCookies)
    {
        var standard = standardCookies
            .Select(c => new CookieProfile(c.CookieName, c.Override))
            .ToList();
        var encrypted = encryptedCookies
            .Select(c => new CookieProfile(c.CookieName, c.ProtectorPurpose, c.Override))
            .ToList();

        app.PopulateCookieProfiles(standard, encrypted);
        return app;
    }

    /// <summary>
    /// Registers one or more pre-built unencrypted <see cref="CookieProfile"/> instances at startup.
    /// Must be called after <see cref="ServiceCollectionExtensions.AddCookieServices"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="standardCookies">One or more <see cref="CookieProfile"/> instances to register as unencrypted profiles.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="CookieProfileDictionary"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a profile already exists for any cookie name.</exception>
    public static IApplicationBuilder PopulateStandardCookieProfiles(this IApplicationBuilder app,
        params IEnumerable<CookieProfile> standardCookies)
    {
        app.PopulateCookieProfiles(standardCookies, []);
        return app;
    }

    /// <summary>
    /// Registers one or more pre-built encrypted <see cref="CookieProfile"/> instances at startup.
    /// Must be called after <see cref="ServiceCollectionExtensions.AddCookieServices"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="encryptedCookies">One or more <see cref="CookieProfile"/> instances to register as encrypted profiles.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="CookieProfileDictionary"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a profile already exists for any cookie name.</exception>
    public static IApplicationBuilder PopulateEncryptedCookieProfiles(this IApplicationBuilder app,
        params IEnumerable<CookieProfile> encryptedCookies)
    {
        app.PopulateCookieProfiles([], encryptedCookies);
        return app;
    }

    /// <summary>
    /// Registers both unencrypted and encrypted pre-built <see cref="CookieProfile"/> instances
    /// in the <see cref="CookieProfileDictionary"/> at startup. Throws if any profile cannot be saved
    /// (e.g. duplicate cookie name).
    /// Must be called after <see cref="ServiceCollectionExtensions.AddCookieServices"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="standardCookies"><see cref="CookieProfile"/> instances to register as unencrypted profiles.</param>
    /// <param name="encryptedCookies"><see cref="CookieProfile"/> instances to register as encrypted profiles.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="CookieProfileDictionary"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a profile already exists for any cookie name.</exception>
    public static IApplicationBuilder PopulateCookieProfiles(this IApplicationBuilder app,
        IEnumerable<CookieProfile> standardCookies, IEnumerable<CookieProfile> encryptedCookies)
    {
        var profileDictionary = app.ApplicationServices.GetService<CookieProfileDictionary>();
        if (profileDictionary == null)
            throw new ArgumentNullException(nameof(profileDictionary), 
                "CookieProfileDictionary is not registered. Call AddCookieServices() before PopulateCookieProfiles().");

        foreach (var profile in standardCookies)
        {
            var result = profileDictionary.TryCreateProfile(profile);
            if (!result)
                throw new InvalidOperationException(
                    $"Unable to create a standard cookie profile for '{profile.CookieName}'.");
        }

        foreach (var profile in encryptedCookies)
        {
            var result = profileDictionary.TryCreateProfile(profile);
            if (!result)
                throw new InvalidOperationException(
                    $"Unable to create an encrypted cookie profile for '{profile.CookieName}' with purpose protector: '{profile.ProtectorPurpose}'.");
        }

        return app;
    }

    #endregion
}
