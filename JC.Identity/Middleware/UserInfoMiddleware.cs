using JC.Core.Models;
using JC.Identity.Authentication;
using JC.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Identity.Middleware;

/// <summary>
/// Middleware that populates <see cref="IUserInfo"/> from the current <see cref="System.Security.Claims.ClaimsPrincipal"/>
/// on first request per scope. Assigns system user constants for unauthenticated requests.
/// </summary>
public class UserInfoMiddleware(RequestDelegate next, ILogger<UserInfoMiddleware> logger)
{
    /// <summary>
    /// Populates the scoped <see cref="IUserInfo"/> instance and invokes the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var userInfo = (IUserInfo)context.RequestServices.GetRequiredService(typeof(IUserInfo));
        var io = context.RequestServices.GetRequiredService<IOptions<IdentityOptions>>();

        if (!userInfo.IsSetup)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                logger.LogDebug("Unauthenticated request — assigning system user identity.");
                userInfo.UserId = IUserInfo.SYSTEM_USER_ID;
                userInfo.Username = IUserInfo.SYSTEM_USER_NAME;
                userInfo.Email = IUserInfo.SYSTEM_USER_EMAIL;
            }
            else
            {
                userInfo.Username = context.User.Identity?.Name ?? IUserInfo.UNKNOWN_USER_NAME;
                userInfo.Email = context.User.FindFirst(io.Value.ClaimsIdentity.EmailClaimType)?.Value ?? IUserInfo.UNKNOWN_USER_EMAIL;
                userInfo.UserId = context.User.FindFirst(io.Value.ClaimsIdentity.UserIdClaimType)?.Value ?? IUserInfo.UNKNOWN_USER_ID;

                userInfo.EmailConfirmed = string.Equals(context.User.FindFirst(DefaultClaims.EmailConfirmed)?.Value, "true", StringComparison.OrdinalIgnoreCase);
                userInfo.PhoneNumber = context.User.FindFirst(DefaultClaims.PhoneNumber)?.Value;
                userInfo.PhoneNumberConfirmed = string.Equals(context.User.FindFirst(DefaultClaims.PhoneNumberConfirmed)?.Value, "true", StringComparison.OrdinalIgnoreCase);
                userInfo.TwoFactorEnabled = string.Equals(context.User.FindFirst(DefaultClaims.TwoFactorEnabled)?.Value, "true", StringComparison.OrdinalIgnoreCase);

                userInfo.LockoutEnabled = string.Equals(context.User.FindFirst(DefaultClaims.LockoutEnabled)?.Value, "true", StringComparison.OrdinalIgnoreCase);
                userInfo.LockoutEnd = DateTime.TryParse(context.User.FindFirst(DefaultClaims.LockoutEnd)?.Value, out var lockoutEnd) ? lockoutEnd : null;

                userInfo.AccessFailedCount = int.TryParse(context.User.FindFirst(DefaultClaims.AccessFailedCount)?.Value, out var accessFailedCount) ? accessFailedCount : 0;
                userInfo.IsEnabled = string.Equals(context.User.FindFirst(DefaultClaims.IsEnabled)?.Value, "true", StringComparison.OrdinalIgnoreCase);

                var tenantId = context.User.FindFirst(DefaultClaims.TenantId)?.Value;
                if (!string.IsNullOrEmpty(tenantId)) userInfo.TenantId = tenantId;
                
                userInfo.MultiTenancyEnabled = !string.IsNullOrEmpty(userInfo.TenantId);
                userInfo.DisplayName = context.User.FindFirst(DefaultClaims.DisplayName)?.Value;
                userInfo.LastLoginUtc = DateTime.TryParse(context.User.FindFirst(DefaultClaims.LastLoginUtc)?.Value, out var lastLoginUtc) ? lastLoginUtc : null;

                userInfo.RequiresPasswordChange = string.Equals(context.User.FindFirst(DefaultClaims.RequirePasswordChange)?.Value, "true", StringComparison.OrdinalIgnoreCase);

                userInfo.Claims = context.User.Claims.ToList().AsReadOnly();
                userInfo.Roles = userInfo.Claims
                    .Where(c => c.Type == io.Value.ClaimsIdentity.RoleClaimType)
                    .Select(c => c.Value)
                    .ToList()
                    .AsReadOnly();

                logger.LogDebug("UserInfo populated for {UserId} ({Username}), tenant: {TenantId}, enabled: {IsEnabled}.",
                    userInfo.UserId, userInfo.Username, userInfo.TenantId ?? "none", userInfo.IsEnabled);
            }

            userInfo.IsSetup = true;
        }

        await next(context);
    }
}