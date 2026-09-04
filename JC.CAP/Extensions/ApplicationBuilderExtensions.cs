using CAP.SSO.Models;
using JC.CAP.Authentication;
using JC.CAP.Services;
using JC.Identity.Shared.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JC.CAP.Extensions;

/// <summary>Extension methods for <see cref="IApplicationBuilder"/> composing the JC.CAP pipeline and publishing roles.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the CAP pipeline in the order it has to run: authentication, the user info projection,
    /// authorisation, then the identity rules. The same order as JC.Identity's <c>UseIdentity</c>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>Call <c>MapCap</c> as well: the endpoints CAP returns to are mapped, not middleware.</remarks>
    public static IApplicationBuilder UseCap(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseUserInfo();
        app.UseAuthorization();
        app.UseIdentityMiddleware();

        return app;
    }

    /// <summary>
    /// Publishes the roles declared on <typeparamref name="TRoles"/> to CAP once, waiting for the answer. The
    /// counterpart of JC.Identity's <c>SeedRolesAsync</c>. Not needed where <see cref="CapRoleSyncJob{TRoles}"/>
    /// runs as a background job, though calling both is harmless.
    /// </summary>
    /// <typeparam name="TRoles">The application's roles class, extending <see cref="SystemRoles"/>.</typeparam>
    /// <param name="app">The application builder.</param>
    /// <param name="throwOnFail">
    /// Whether a failed publish stops the application starting. Defaults to <c>true</c>, so a wrong secret or
    /// an unreachable CAP is found at startup rather than at the first sign-in.
    /// </param>
    /// <returns>
    /// What CAP did with the catalogue, or <c>null</c> where the publish failed and
    /// <paramref name="throwOnFail"/> is <c>false</c>.
    /// </returns>
    public static async Task<CatalogueSync?> SyncCapRolesAsync<TRoles>(this IApplicationBuilder app, bool throwOnFail = true)
        where TRoles : SystemRoles
    {
        using var scope = app.ApplicationServices.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<CapRoleSyncJob<TRoles>>();

        try
        {
            return await job.SyncAsync();
        }
        catch (Exception ex) when (!throwOnFail)
        {
            scope.ServiceProvider.GetRequiredService<ILogger<CapRoleSyncJob<TRoles>>>()
                 .LogError(ex, "Publishing the role catalogue to CAP failed. The application continues without it.");

            return null;
        }
    }
}
