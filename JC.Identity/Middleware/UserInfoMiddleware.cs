using JC.Core.Models;
using JC.Identity.Authentication;
using JC.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JC.Identity.Middleware;

public class UserInfoMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userInfo = (IUserInfo)context.RequestServices.GetRequiredService(typeof(IUserInfo));
        var io = context.RequestServices.GetRequiredService<IOptions<IdentityOptions>>();

        if (!userInfo.IsSetup)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                userInfo.UserId = UserInfo.SYSTEM_USER_ID;
                userInfo.Username = UserInfo.SYSTEM_USER_NAME;
                userInfo.Email = UserInfo.SYSTEM_USER_EMAIL;
            }
            else
            {
                userInfo.Username = context.User.Identity?.Name ?? UserInfo.UNKNOWN_USER_NAME;
                userInfo.Email = context.User.FindFirst(io.Value.ClaimsIdentity.EmailClaimType)?.Value ?? UserInfo.UNKNOWN_USER_EMAIL;
                userInfo.UserId = context.User.FindFirst(io.Value.ClaimsIdentity.UserIdClaimType)?.Value ?? UserInfo.UNKNOWN_USER_ID;
                
                userInfo.EmailConfirmed = string.Equals(context.User.FindFirst(DefaultClaims.EmailConfirmed)?.Value, "true", StringComparison.OrdinalIgnoreCase);
                userInfo.PhoneNumber = context.User.FindFirst(DefaultClaims.PhoneNumber)?.Value;
                userInfo.PhoneNumberConfirmed = string.Equals(context.User.FindFirst(DefaultClaims.PhoneNumberConfirmed)?.Value, "true", StringComparison.OrdinalIgnoreCase);
                userInfo.TwoFactorEnabled = string.Equals(context.User.FindFirst(DefaultClaims.TwoFactorEnabled)?.Value, "true", StringComparison.OrdinalIgnoreCase);
                
                userInfo.LockoutEnabled = string.Equals(context.User.FindFirst(DefaultClaims.LockoutEnabled)?.Value, "true", StringComparison.OrdinalIgnoreCase);
                userInfo.LockoutEnd = DateTime.TryParse(context.User.FindFirst(DefaultClaims.LockoutEnd)?.Value, out var lockoutEnd) ? lockoutEnd : null;
                
                userInfo.AccessFailedCount = int.TryParse(context.User.FindFirst(DefaultClaims.AccessFailedCount)?.Value, out var accessFailedCount) ? accessFailedCount : 0;
                userInfo.IsEnabled = string.Equals(context.User.FindFirst(DefaultClaims.IsEnabled)?.Value, "true", StringComparison.OrdinalIgnoreCase);
                
                userInfo.TenantId = context.User.FindFirst(DefaultClaims.TenantId)?.Value;
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
            }
            
            userInfo.IsSetup = true;
        }
        
        await next(context);
    }
}