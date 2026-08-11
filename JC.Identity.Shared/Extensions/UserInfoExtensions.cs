using System.Security.Claims;
using JC.Core.Models;
using JC.Identity.Shared.Authentication;
using JC.Identity.Shared.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// Sets <see cref="IUserInfo.IsSetup"/>, so an instance populated this way is left alone by the
    /// claims middleware should the scope later run through it.
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
    /// Projects a claims principal onto an existing <see cref="IUserInfo"/> and marks it populated.
    /// </summary>
    /// <typeparam name="T">The <see cref="IUserInfo"/> implementation.</typeparam>
    /// <param name="userInfo">The instance to populate.</param>
    /// <param name="principal">The principal to project, or <c>null</c> where none was established.</param>
    /// <param name="options">The claim types to read and the authority to stamp.</param>
    /// <param name="logger">Optional logger recording the projection outcome.</param>
    /// <returns>The same instance, for chaining.</returns>
    public static T PopulateFrom<T>(this T userInfo, ClaimsPrincipal? principal,
        IdentityProjectionOptions options, ILogger? logger = null)
        where T : class, IUserInfo
    {
        if (principal?.Identity is null)
        {
            logger?.LogDebug("No identity present — assigning system user identity.");
            userInfo.UserId = IUserInfo.SYSTEM_USER_ID;
            userInfo.Username = IUserInfo.SYSTEM_USER_NAME;
            userInfo.Email = IUserInfo.SYSTEM_USER_EMAIL;
        }
        else if (!principal.Identity.IsAuthenticated)
        {
            logger?.LogDebug("Unauthenticated request — assigning unknown user identity.");
            userInfo.UserId = IUserInfo.UNKNOWN_USER_ID;
            userInfo.Username = IUserInfo.UNKNOWN_USER_NAME;
            userInfo.Email = IUserInfo.UNKNOWN_USER_EMAIL;
        }
        else
        {
            const string trueValue = "true";
            userInfo.Authority = options.Authority;

            userInfo.Username = principal.Identity.Name ?? IUserInfo.UNKNOWN_USER_NAME;
            userInfo.Email = principal.FindFirst(options.EmailClaimType)?.Value ?? IUserInfo.UNKNOWN_USER_EMAIL;
            userInfo.UserId = principal.FindFirst(options.UserIdClaimType)?.Value ?? IUserInfo.UNKNOWN_USER_ID;

            userInfo.EmailConfirmed = string.Equals(principal.FindFirst(DefaultClaims.EmailConfirmed)?.Value, trueValue, StringComparison.OrdinalIgnoreCase);
            userInfo.PhoneNumber = principal.FindFirst(DefaultClaims.PhoneNumber)?.Value;
            userInfo.PhoneNumberConfirmed = string.Equals(principal.FindFirst(DefaultClaims.PhoneNumberConfirmed)?.Value, trueValue, StringComparison.OrdinalIgnoreCase);
            userInfo.TwoFactorEnabled = string.Equals(principal.FindFirst(DefaultClaims.TwoFactorEnabled)?.Value, trueValue, StringComparison.OrdinalIgnoreCase);

            userInfo.LockoutEnabled = string.Equals(principal.FindFirst(DefaultClaims.LockoutEnabled)?.Value, trueValue, StringComparison.OrdinalIgnoreCase);
            userInfo.LockoutEnd = DateTime.TryParse(principal.FindFirst(DefaultClaims.LockoutEnd)?.Value, out var lockoutEnd) ? lockoutEnd : null;

            userInfo.AccessFailedCount = int.TryParse(principal.FindFirst(DefaultClaims.AccessFailedCount)?.Value, out var accessFailedCount) ? accessFailedCount : 0;
            userInfo.IsEnabled = string.Equals(principal.FindFirst(DefaultClaims.IsEnabled)?.Value, trueValue, StringComparison.OrdinalIgnoreCase);

            var tenantId = principal.FindFirst(DefaultClaims.TenantId)?.Value;
            if (!string.IsNullOrEmpty(tenantId)) userInfo.TenantId = tenantId;

            userInfo.DisplayName = principal.FindFirst(DefaultClaims.DisplayName)?.Value;
            userInfo.LastLoginUtc = DateTime.TryParse(principal.FindFirst(DefaultClaims.LastLoginUtc)?.Value, out var lastLoginUtc) ? lastLoginUtc : null;
            userInfo.RegistrationUtc = DateTime.TryParse(principal.FindFirst(DefaultClaims.RegistrationUtc)?.Value, out var registrationUtc) ? registrationUtc : null;

            userInfo.RequiresPasswordChange = string.Equals(principal.FindFirst(DefaultClaims.RequirePasswordChange)?.Value, trueValue, StringComparison.OrdinalIgnoreCase);

            userInfo.Claims = principal.Claims.ToList().AsReadOnly();
            userInfo.Roles = userInfo.Claims
                .Where(c => c.Type == options.RoleClaimType)
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly();

            logger?.LogDebug("UserInfo populated for {UserId} ({Username}), tenant: {TenantId}, enabled: {IsEnabled}.",
                userInfo.UserId, userInfo.Username, userInfo.TenantId ?? "none", userInfo.IsEnabled);
        }

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
