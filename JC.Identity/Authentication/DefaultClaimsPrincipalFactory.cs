using System.Security.Claims;
using JC.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace JC.Identity.Authentication;

/// <summary>
/// Custom claims principal factory that extends the default identity claims with all 12
/// <see cref="DefaultClaims"/> from the user entity.
/// </summary>
/// <typeparam name="TUser">The user type, extending <see cref="BaseUser"/>.</typeparam>
/// <typeparam name="TRole">The role type, extending <see cref="BaseRole"/>.</typeparam>
public class DefaultClaimsPrincipalFactory<TUser, TRole>(
    UserManager<TUser> userManager,
    RoleManager<TRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<TUser, TRole>(userManager, roleManager, options)
    where TUser : BaseUser
    where TRole : BaseRole
{
    /// <inheritdoc />
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(TUser user)
    {
        var defaultClaims = await base.GenerateClaimsAsync(user);
        
        defaultClaims.AddClaim(new Claim(DefaultClaims.EmailConfirmed, user.EmailConfirmed.ToString()));
        defaultClaims.AddClaim(new Claim(DefaultClaims.PhoneNumber, user.PhoneNumber ?? ""));
        defaultClaims.AddClaim(new Claim(DefaultClaims.PhoneNumberConfirmed, user.PhoneNumberConfirmed.ToString()));
        defaultClaims.AddClaim(new Claim(DefaultClaims.TwoFactorEnabled, user.TwoFactorEnabled.ToString()));
        defaultClaims.AddClaim(new Claim(DefaultClaims.LockoutEnabled, user.LockoutEnabled.ToString()));
        defaultClaims.AddClaim(new Claim(DefaultClaims.LockoutEnd, user.LockoutEnd?.ToString("O") ?? ""));
        defaultClaims.AddClaim(new Claim(DefaultClaims.AccessFailedCount, user.AccessFailedCount.ToString()));
        
        defaultClaims.AddClaim(new Claim(DefaultClaims.TenantId, user.TenantId ?? ""));
        defaultClaims.AddClaim(new Claim(DefaultClaims.DisplayName, user.DisplayName ?? ""));
        defaultClaims.AddClaim(new Claim(DefaultClaims.LastLoginUtc, user.LastLoginUtc?.ToString("O") ?? ""));
        defaultClaims.AddClaim(new Claim(DefaultClaims.IsEnabled, user.IsEnabled.ToString()));
        
        defaultClaims.AddClaim(new Claim(DefaultClaims.RequirePasswordChange, user.RequirePasswordChange.ToString()));
        
        return defaultClaims;
    }
}