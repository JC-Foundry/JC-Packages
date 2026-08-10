using JC.Identity.Models;
using JC.Identity.Shared.Authentication;
using JC.Identity.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JC.Identity.Extensions;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> wiring up local ASP.NET Identity and
/// seeding its roles and default administrator.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the full local Identity pipeline in the required order: authentication, then the user
    /// info projection, then authorisation, then the identity business rules.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    /// The order matters in both directions. <c>UseUserInfo</c> must follow authentication because
    /// it reads the principal's claims, and must precede <c>UseIdentityMiddleware</c> because that
    /// enforces rules against what it produced.
    /// </remarks>
    public static IApplicationBuilder UseIdentity(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseUserInfo();
        app.UseAuthorization();
        app.UseIdentityMiddleware();

        return app;
    }


    /// <summary>
    /// Seeds the system roles, then the default administrator account.
    /// </summary>
    /// <param name="app">The application builder instance used to configure the application services.</param>
    /// <param name="assignAdminRole">Whether the seeded administrator is given <see cref="SystemRoles.Admin"/> in addition to <see cref="SystemRoles.SystemAdmin"/>. Defaults to <c>true</c>.</param>
    /// <param name="usernameConfigKey">The configuration key for the admin username.</param>
    /// <param name="emailConfigKey">The configuration key for the admin email.</param>
    /// <param name="passwordConfigKey">The configuration key for the admin password.</param>
    /// <param name="displayNameConfigKey">The configuration key for the admin display name.</param>
    /// <param name="additionalRoles">A collection of additional roles to assign to the administrator.</param>
    /// <typeparam name="TUser">The user entity type representing the administrator, inheriting from BaseUser.</typeparam>
    /// <typeparam name="TRole">The type representing a role entity, inheriting from BaseRole.</typeparam>
    /// <typeparam name="TRoles">The type representing the system roles, inheriting from SystemRoles.</typeparam>
    /// <returns>
    /// The administrator account — newly created, or the existing one where a matching account was
    /// already present. <c>null</c> only where creation was attempted and failed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required services such as RoleManager or UserManager are not available, or if a required configuration value is missing.
    /// </exception>
    /// <remarks>
    /// Tenants are not this package's concern. A tenant-scoped application passes the returned user
    /// to JC.Tenancy's <c>SeedDefaultTenantAsync</c>:
    /// <code>
    /// var admin = await app.ConfigureAdminAndRolesAsync&lt;AppUser, AppRole, AppRoles&gt;();
    /// if (admin is not null)
    ///     await app.ApplicationServices.SeedDefaultTenantAsync(admin);
    /// </code>
    /// </remarks>
    public static async Task<TUser?> ConfigureAdminAndRolesAsync<TUser, TRole, TRoles>(
        this IApplicationBuilder app,
        bool assignAdminRole = true,
        string usernameConfigKey = "Admin:Username",
        string emailConfigKey = "Admin:Email",
        string passwordConfigKey = "Admin:Password",
        string displayNameConfigKey = "Admin:DisplayName",
        IEnumerable<string>? additionalRoles = null)
        where TUser : BaseUser, new()
        where TRole : BaseRole, new()
        where TRoles : SystemRoles
    {
        await app.SeedRolesAsync<TRoles, TRole>();

        return await app.SeedDefaultAdminAsync<TUser>
            (assignAdminRole, usernameConfigKey, emailConfigKey, passwordConfigKey, displayNameConfigKey, additionalRoles);
    }

    /// <summary>
    /// Seeds the specified system roles into the database if they do not already exist.
    /// </summary>
    /// <param name="app">The application builder instance used to access services.</param>
    /// <typeparam name="TRoles">The type representing the system roles, inheriting from SystemRoles.</typeparam>
    /// <typeparam name="TRole">The type representing the role entity, inheriting from BaseRole.</typeparam>
    /// <returns>The configured application builder instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the required RoleManager service is not available or roles cannot be created.
    /// </exception>
    public static async Task<IApplicationBuilder> SeedRolesAsync<TRoles, TRole>(this IApplicationBuilder app)
        where TRoles : SystemRoles
        where TRole : BaseRole, new()
    {
        using var scope = app.ApplicationServices.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<TRole>>();

        var roles = SystemRoles.GetAllRoles<TRoles>();

        foreach (var (role, description) in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new TRole
                {
                    Name = role,
                    Description = description
                });
            }
        }

        return app;
    }

    /// <summary>
    /// Seeds a default administrator account from configuration.
    /// </summary>
    /// <param name="app">The application builder instance used to access services.</param>
    /// <param name="assignAdminRole">Whether the administrator is given <see cref="SystemRoles.Admin"/> in addition to <see cref="SystemRoles.SystemAdmin"/>. Defaults to <c>true</c>.</param>
    /// <param name="usernameConfigKey">The configuration key for the administrator's username.</param>
    /// <param name="emailConfigKey">The configuration key for the administrator's email address.</param>
    /// <param name="passwordConfigKey">The configuration key for the administrator's password.</param>
    /// <param name="displayNameConfigKey">The configuration key for the administrator's display name.</param>
    /// <param name="additionalRoles">A collection of additional roles to assign to the administrator.</param>
    /// <typeparam name="TUser">The type representing the user entity inheriting from BaseUser.</typeparam>
    /// <returns>
    /// The administrator account — newly created, or the existing one where an account already
    /// matched the configured email or username. <c>null</c> only where creation was attempted and
    /// failed, in which case the reason is logged.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a required configuration value is missing.
    /// </exception>
    /// <remarks>
    /// Returning the existing account rather than nothing keeps follow-on setup idempotent: a run
    /// that created the user but failed a later step can correct itself on the next start.
    /// </remarks>
    public static async Task<TUser?> SeedDefaultAdminAsync<TUser>(
        this IApplicationBuilder app,
        bool assignAdminRole = true,
        string usernameConfigKey = "Admin:Username",
        string emailConfigKey = "Admin:Email",
        string passwordConfigKey = "Admin:Password",
        string displayNameConfigKey = "Admin:DisplayName",
        IEnumerable<string>? additionalRoles = null)
        where TUser : BaseUser, new()
    {
        using var scope = app.ApplicationServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var username = config[usernameConfigKey] ?? throw new InvalidOperationException($"Configuration value '{usernameConfigKey}' not found.");
        var email = config[emailConfigKey] ?? throw new InvalidOperationException($"Configuration value '{emailConfigKey}' not found.");
        var password = config[passwordConfigKey] ?? throw new InvalidOperationException($"Configuration value '{passwordConfigKey}' not found.");
        var displayName = config[displayNameConfigKey];

        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin != null)
            return existingAdmin;

        existingAdmin = await userManager.FindByNameAsync(username);
        if (existingAdmin != null)
            return existingAdmin;

        var admin = new TUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName ?? "System Administrator",
            IsEnabled = true,
            RegistrationUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TUser>>();
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create default admin user '{Username}': {Errors}", username, errors);
            return null;
        }

        await AssignRoleAsync(userManager, logger, admin, SystemRoles.SystemAdmin);
        if (assignAdminRole) await AssignRoleAsync(userManager, logger, admin, SystemRoles.Admin);

        if (additionalRoles == null) return admin;

        foreach (var role in additionalRoles)
            await AssignRoleAsync(userManager, logger, admin, role);

        return admin;
    }

    private static async Task AssignRoleAsync<TUser>(UserManager<TUser> userManager, ILogger logger, TUser user, string role)
        where TUser : BaseUser
    {
        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to assign role '{Role}' to user '{Username}': {Errors}", role, user.UserName, errors);
        }
    }
}
