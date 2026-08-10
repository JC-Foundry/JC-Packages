using JC.Identity.Shared.Middleware;
using Microsoft.AspNetCore.Builder;

namespace JC.Identity.Shared.Extensions;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> registering the shared identity
/// middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="UserInfoMiddleware"/>, which projects the current principal's claims onto
    /// the scoped <see cref="JC.Core.Models.IUserInfo"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>Must run after authentication, and before anything that reads <c>IUserInfo</c>.</remarks>
    public static IApplicationBuilder UseUserInfo(this IApplicationBuilder app)
        => app.UseMiddleware<UserInfoMiddleware>();

    /// <summary>
    /// Adds <see cref="IdentityMiddleware"/>, which enforces disabled accounts, required password
    /// changes and optional two-factor setup.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>Must run after <see cref="UseUserInfo"/>, whose output it reads.</remarks>
    public static IApplicationBuilder UseIdentityMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<IdentityMiddleware>();
}
