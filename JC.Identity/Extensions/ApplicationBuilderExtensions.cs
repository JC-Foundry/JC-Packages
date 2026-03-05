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
    /// <param name="additionalRoles">A collection of additional roles to be seeded into the system.</param>
    /// <typeparam name="TUser">The user entity type representing the administrator, inheriting from BaseUser.</typeparam>
    /// <typeparam name="TRoles">The type representing the system roles, inheriting from SystemRoles.</typeparam>
    /// <typeparam name="TRole">The type representing a role entity, inheriting from BaseRole.</typeparam>
    /// <returns>The configured application builder instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required services such as RoleManager or UserManager are not available or if roles or the admin user cannot be created.
    /// </exception>
    public static async Task<IApplicationBuilder> ConfigureAdminAndRolesAsync<TUser, TRoles, TRole>(
        this IApplicationBuilder app,
        bool setupTenancy = false,
        string usernameConfigKey = "Admin:Username",
        string emailConfigKey = "Admin:Email",
        string passwordConfigKey = "Admin:Password",
        string displayNameConfigKey = "Admin:DisplayName",
        IEnumerable<string>? additionalRoles = null)
        where TUser : BaseUser, new()
        where TRoles : SystemRoles
        where TRole : BaseRole, new()
    {
        await app.SeedRolesAsync<TRoles, TRole>();
        await app.SeedDefaultAdminAsync<TUser, TRole>
            (setupTenancy, usernameConfigKey, emailConfigKey, passwordConfigKey, displayNameConfigKey, additionalRoles);

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
    /// When <paramref name="setupTenancy"/> is <c>true</c>, finds or creates a "Default Tenant" and assigns it to the admin user.
    /// </summary>
    /// <param name="app">The application builder instance used to access services.</param>
    /// <param name="setupTenancy">Indicates whether a default tenant should be found or created for the admin user.</param>
    /// <param name="usernameConfigKey">The configuration key for the administrator's username.</param>
    /// <param name="emailConfigKey">The configuration key for the administrator's email address.</param>
    /// <param name="passwordConfigKey">The configuration key for the administrator's password.</param>
    /// <param name="displayNameConfigKey">The configuration key for the administrator's display name.</param>
    /// <param name="additionalRoles">A collection of additional roles to assign to the administrator.</param>
    /// <typeparam name="TUser">The type representing the user entity inheriting from BaseUser.</typeparam>
    /// <typeparam name="TRole">The type representing the role entity inheriting from BaseRole.</typeparam>
    /// <returns>The configured application builder instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required configuration values are not found or invalid.
    /// </exception>
    public static async Task<IApplicationBuilder> SeedDefaultAdminAsync<TUser, TRole>(
        this IApplicationBuilder app,
        bool setupTenancy = false,
        string usernameConfigKey = "Admin:Username",
        string emailConfigKey = "Admin:Email",
        string passwordConfigKey = "Admin:Password",
        string displayNameConfigKey = "Admin:DisplayName",
        IEnumerable<string>? additionalRoles = null)
        where TUser : BaseUser, new()
        where TRole : BaseRole
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
            var context = scope.ServiceProvider.GetRequiredService<IdentityDataDbContext<TUser, TRole>>();
            tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == "Default Tenant");

            if (tenant == null)
            {
                tenant = new Tenant
                {
                    Name = "Default Tenant",
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

        if (!result.Succeeded) return app;

        await userManager.AddToRoleAsync(admin, SystemRoles.SystemAdmin);
        if (!setupTenancy) await userManager.AddToRoleAsync(admin, SystemRoles.Admin);

        if (additionalRoles == null) return app;

        var roles = additionalRoles.ToList();
        foreach (var role in roles)
            await userManager.AddToRoleAsync(admin, role);

        return app;
    }
}