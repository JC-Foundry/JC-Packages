using JC.Identity.Authentication;
using JC.Identity.Data;
using JC.Identity.Middleware;
using JC.Identity.Models;
using JC.Identity.Models.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JC.Identity.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseUserInfo(this IApplicationBuilder app)
        => app.UseMiddleware<UserInfoMiddleware>();

    public static IApplicationBuilder UseIdentityMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<IdentityMiddleware>();

    public static IApplicationBuilder UseIdentity(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseUserInfo();
        app.UseAuthorization();
        app.UseIdentityMiddleware();

        return app;
    }


    /// <summary>
    /// Configures the system by setting up the default administrator account and seeding system roles if they do not already exist.
    /// </summary>
    /// <param name="app">The application builder instance used to configure the application services.</param>
    /// <param name="setupTenancy">Specifies whether tenancy should be configured for the admin user. When <c>true</c>, a default tenant is found or created.</param>
    /// <param name="usernameConfigKey">The configuration key for the admin username.</param>
    /// <param name="emailConfigKey">The configuration key for the admin email.</param>
    /// <param name="passwordConfigKey">The configuration key for the admin password.</param>
    /// <param name="displayNameConfigKey">The configuration key for the admin display name.</param>
    /// <param name="defaultTenantConfigKey">The configuration key for the default tenant name. Falls back to "Default Tenant" if not configured.</param>
    /// <param name="additionalRoles">A collection of additional roles to be seeded into the system.</param>
    /// <typeparam name="TUser">The user entity type representing the administrator, inheriting from BaseUser.</typeparam>
    /// <typeparam name="TRoles">The type representing the system roles, inheriting from SystemRoles.</typeparam>
    /// <typeparam name="TRole">The type representing a role entity, inheriting from BaseRole.</typeparam>
    /// <returns>The configured application builder instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required services such as RoleManager or UserManager are not available or if roles or the admin user cannot be created.
    /// </exception>
    public static async Task<IApplicationBuilder> ConfigureAdminAndRolesAsync<TUser, TRole, TContext, TRoles>(
        this IApplicationBuilder app,
        bool setupTenancy = false,
        string usernameConfigKey = "Admin:Username",
        string emailConfigKey = "Admin:Email",
        string passwordConfigKey = "Admin:Password",
        string displayNameConfigKey = "Admin:DisplayName",
        string defaultTenantConfigKey = "Admin:DefaultTenantName",
        IEnumerable<string>? additionalRoles = null)
        where TUser : BaseUser, new()
        where TRole : BaseRole, new()
        where TContext : IdentityDataDbContext<TUser, TRole>
        where TRoles : SystemRoles
    {
        await app.SeedRolesAsync<TRoles, TRole>();
        await app.SeedDefaultAdminAsync<TUser, TRole, TContext>
            (setupTenancy, usernameConfigKey, emailConfigKey, passwordConfigKey, displayNameConfigKey, defaultTenantConfigKey, additionalRoles);

        return app;
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
    /// Seeds a default administrator account to the database with specified configuration settings.
    /// When <paramref name="setupTenancy"/> is <c>true</c>, finds or creates a default tenant (configurable via <paramref name="defaultTenantConfigKey"/>) and assigns it to the admin user.
    /// </summary>
    /// <param name="app">The application builder instance used to access services.</param>
    /// <param name="setupTenancy">Indicates whether a default tenant should be found or created for the admin user.</param>
    /// <param name="usernameConfigKey">The configuration key for the administrator's username.</param>
    /// <param name="emailConfigKey">The configuration key for the administrator's email address.</param>
    /// <param name="passwordConfigKey">The configuration key for the administrator's password.</param>
    /// <param name="displayNameConfigKey">The configuration key for the administrator's display name.</param>
    /// <param name="defaultTenantConfigKey">The configuration key for the default tenant name. Falls back to "Default Tenant" if not configured.</param>
    /// <param name="additionalRoles">A collection of additional roles to assign to the administrator.</param>
    /// <typeparam name="TUser">The type representing the user entity inheriting from BaseUser.</typeparam>
    /// <typeparam name="TRole">The type representing the role entity inheriting from BaseRole.</typeparam>
    /// <typeparam name="TContext">The database context type inheriting from IdentityDataDbContext.</typeparam>
    /// <returns>The configured application builder instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required configuration values are not found or invalid.
    /// </exception>
    public static async Task<IApplicationBuilder> SeedDefaultAdminAsync<TUser, TRole, TContext>(
        this IApplicationBuilder app,
        bool setupTenancy = false,
        string usernameConfigKey = "Admin:Username",
        string emailConfigKey = "Admin:Email",
        string passwordConfigKey = "Admin:Password",
        string displayNameConfigKey = "Admin:DisplayName",
        string defaultTenantConfigKey = "Admin:DefaultTenantName",
        IEnumerable<string>? additionalRoles = null)
        where TUser : BaseUser, new()
        where TRole : BaseRole
        where TContext : IdentityDataDbContext<TUser, TRole>
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
            return app;

        existingAdmin = await userManager.FindByNameAsync(username);
        if (existingAdmin != null)
            return app;

        Tenant? tenant = null;
        if (setupTenancy)
        {
            var tenantName = config[defaultTenantConfigKey] ?? "Default Tenant";
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == tenantName);

            if (tenant == null)
            {
                tenant = new Tenant
                {
                    Name = tenantName,
                    Description = "Default system tenant"
                };
                await context.Tenants.AddAsync(tenant);
                await context.SaveChangesAsync();
            }
        }

        var admin = new TUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName ?? "System Administrator",
            IsEnabled = true,
            TenantId = tenant?.Id
        };

        var result = await userManager.CreateAsync(admin, password);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TUser>>();
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create default admin user '{Username}': {Errors}", username, errors);
            return app;
        }

        await AssignRoleAsync(userManager, logger, admin, SystemRoles.SystemAdmin);
        if (!setupTenancy) await AssignRoleAsync(userManager, logger, admin, SystemRoles.Admin);

        if (additionalRoles == null) return app;

        foreach (var role in additionalRoles)
            await AssignRoleAsync(userManager, logger, admin, role);

        return app;
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