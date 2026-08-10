using JC.Core.Models;
using JC.Identity.Shared.Models;
using JC.Identity.Shared.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JC.Identity.Shared.Extensions;

/// <summary>
/// Projects an authoritative user record onto an <see cref="IUserInfo"/>, and establishes that
/// identity as the ambient one for work happening outside an HTTP request.
/// </summary>
/// <remarks>
/// <see cref="IUserInfo"/> is registered scoped and populated in place, so constructing one and
/// handing it around does not make it ambient — nothing that injects <see cref="IUserInfo"/> would
/// see it. Everything here therefore resolves the scope's own instance and fills that in.
/// </remarks>
public static class UserInfoExtensions
{
    /// <summary>
    /// Projects an authoritative user record onto an existing <see cref="IUserInfo"/> and marks it
    /// populated.
    /// </summary>
    /// <typeparam name="T">The <see cref="IUserInfo"/> implementation.</typeparam>
    /// <param name="userInfo">The instance to populate.</param>
    /// <param name="user">The user record to project.</param>
    /// <param name="roles">The user's role names. Null and empty entries are discarded.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// Deliberately does not set <see cref="IUserInfo.TenantId"/>, as
    /// <see cref="JC.Core.Models.IApplicationUser.IdentityTenantId"/> does not inherently mean the user's
    /// application tenant. Nor does it set <see cref="IUserInfo.Authority"/>, which only the
    /// registering package knows.
    /// <para>
    /// Sets <see cref="IUserInfo.IsSetup"/>, so an instance populated this way is left alone by
    /// <see cref="Middleware.UserInfoMiddleware"/> should the scope later run through it.
    /// </para>
    /// </remarks>
    public static T PopulateFrom<T>(this T userInfo, IApplicationUser user, IEnumerable<string?> roles)
        where T : class, IUserInfo
    {
        userInfo.UserId = user.Id;
        userInfo.Username = user.UserName ?? IUserInfo.UNKNOWN_USER_NAME;
        userInfo.Email = user.Email ?? IUserInfo.UNKNOWN_USER_EMAIL;
        userInfo.EmailConfirmed = user.EmailConfirmed;
        userInfo.PhoneNumber = user.PhoneNumber;
        userInfo.PhoneNumberConfirmed = user.PhoneNumberConfirmed;

        userInfo.TwoFactorEnabled = user.TwoFactorEnabled;
        userInfo.LockoutEnabled = user.LockoutEnabled;
        userInfo.LockoutEnd = user.LockoutEnd?.DateTime;
        userInfo.AccessFailedCount = user.AccessFailedCount;

        userInfo.DisplayName = user.DisplayName;
        userInfo.LastLoginUtc = user.LastLoginUtc;
        userInfo.RegistrationUtc = user.RegistrationUtc;
        userInfo.IsEnabled = user.IsEnabled;

        userInfo.RequiresPasswordChange = user.RequirePasswordChange;

        userInfo.Roles = roles.Where(r => !string.IsNullOrEmpty(r)).ToList()!;
        userInfo.IsSetup = true;

        return userInfo;
    }

    /// <summary>
    /// Establishes a user as the ambient identity for a scope, by populating that scope's
    /// <see cref="IUserInfo"/> in place.
    /// </summary>
    /// <param name="scopedServices">The scope's service provider.</param>
    /// <param name="user">The user record to project.</param>
    /// <param name="roles">The user's role names. Null and empty entries are discarded.</param>
    /// <param name="tenantId">
    /// The user's tenant within this application, or <c>null</c> for the null tenant partition.
    /// Passed separately rather than taken from <see cref="JC.Core.Models.IApplicationUser.IdentityTenantId"/>,
    /// because the tenant owning an identity record and the user's application tenant are different
    /// concepts. Where they coincide, pass <c>user.IdentityTenantId</c> and say so at the call site.
    /// </param>
    /// <returns>The populated <see cref="IUserInfo"/>.</returns>
    /// <remarks>
    /// Intended for background jobs and other non-HTTP work that needs attribution. Calling it
    /// inside a live request scope replaces the authenticated user for the rest of that request —
    /// which is impersonation, and should be a deliberate choice rather than a convenience.
    /// </remarks>
    public static IUserInfo SetUserInfoForUser(this IServiceProvider scopedServices,
        IApplicationUser user, IEnumerable<string?> roles, string? tenantId = null)
    {
        var userInfo = scopedServices.GetRequiredService<IUserInfo>();

        userInfo.PopulateFrom(user, roles);

        userInfo.TenantId = tenantId;
        userInfo.MultiTenancyEnabled = !string.IsNullOrEmpty(tenantId);

        // Same source the claims middleware uses, so the authority is stated in exactly one place.
        userInfo.Authority = scopedServices
            .GetRequiredService<IOptions<IdentityProjectionOptions>>().Value.Authority;

        return userInfo;
    }

    /// <summary>
    /// Creates a service scope with the ambient identity already established.
    /// </summary>
    /// <param name="services">The root or parent service provider.</param>
    /// <param name="user">The user record to project.</param>
    /// <param name="roles">The user's role names. Null and empty entries are discarded.</param>
    /// <param name="tenantId">
    /// The user's tenant within this application, or <c>null</c> for the null tenant partition.
    /// See <see cref="SetUserInfoForUser"/> for why this is separate from the user record.
    /// </param>
    /// <returns>The scope. Dispose it to release the scoped services.</returns>
    /// <example>
    /// <code>
    /// using var scope = _services.CreateScopeForUser(user, roles, user.IdentityTenantId);
    /// var repo = scope.ServiceProvider.GetRequiredService&lt;IOrderRepository&gt;();
    /// await repo.SaveAsync(order);   // audited against that user
    /// </code>
    /// </example>
    public static IServiceScope CreateScopeForUser(this IServiceProvider services,
        IApplicationUser user, IEnumerable<string?> roles, string? tenantId = null)
    {
        var scope = services.CreateScope();
        scope.ServiceProvider.SetUserInfoForUser(user, roles, tenantId);

        return scope;
    }

    /// <summary>
    /// Creates an asynchronously disposable service scope with the ambient identity already
    /// established, for work whose scoped services implement <see cref="IAsyncDisposable"/>.
    /// </summary>
    /// <param name="services">The root or parent service provider.</param>
    /// <param name="user">The user record to project.</param>
    /// <param name="roles">The user's role names. Null and empty entries are discarded.</param>
    /// <param name="tenantId">
    /// The user's tenant within this application, or <c>null</c> for the null tenant partition.
    /// See <see cref="SetUserInfoForUser"/> for why this is separate from the user record.
    /// </param>
    /// <returns>The scope. Dispose it with <c>await using</c>.</returns>
    /// <example>
    /// <code>
    /// await using var scope = _services.CreateAsyncScopeForUser(user, roles, user.IdentityTenantId);
    /// var repo = scope.ServiceProvider.GetRequiredService&lt;IOrderRepository&gt;();
    /// await repo.SaveAsync(order);   // audited against that user
    /// </code>
    /// </example>
    public static AsyncServiceScope CreateAsyncScopeForUser(this IServiceProvider services,
        IApplicationUser user, IEnumerable<string?> roles, string? tenantId = null)
    {
        var scope = services.CreateAsyncScope();
        scope.ServiceProvider.SetUserInfoForUser(user, roles, tenantId);

        return scope;
    }
}
