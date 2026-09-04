using System.Security.Claims;
using CAP.SSO.Models;
using JC.Identity.Shared.Authentication;
using Microsoft.Extensions.Logging;

namespace JC.CAP.Authentication;

/// <summary>
/// The default <see cref="ICapClaimsPrincipalFactory"/>: CAP's vocabulary in, ASP.NET Identity's out, so the
/// shared projection, <c>[Authorize(Roles = ...)]</c> and <c>User.IsInRole</c> all read the cookie unchanged.
/// </summary>
public class CapClaimsPrincipalFactory(
    IEnumerable<ICapClaimsEnricher> enrichers,
    ILogger<CapClaimsPrincipalFactory> logger) : ICapClaimsPrincipalFactory
{
    /// <inheritdoc />
    public virtual async Task<ClaimsPrincipal> CreateAsync(ClaimsPrincipal capPrincipal, bool isRefresh,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capPrincipal);

        var userId = capPrincipal.FindFirst(OIDC.Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("CAP returned a principal without a subject claim.");

        var identity = new ClaimsIdentity(CapDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

        // The username, not the full name: Identity.Name is what the projection reads as Username.
        var username = Value(capPrincipal, OIDC.Claims.PreferredUsername)
                       ?? Value(capPrincipal, OIDC.Claims.Email)
                       ?? userId;
        identity.AddClaim(new Claim(ClaimTypes.Name, username));

        if (Value(capPrincipal, OIDC.Claims.Email) is { } email)
            identity.AddClaim(new Claim(ClaimTypes.Email, email));

        // Distinct: the id token and userinfo can both carry the role set.
        var roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in capPrincipal.FindAll(OIDC.Claims.Role))
            if (!string.IsNullOrEmpty(role.Value) && roles.Add(role.Value))
                identity.AddClaim(new Claim(ClaimTypes.Role, role.Value));

        // Already under JC's names and in the formats the projection parses, so copied as they are.
        foreach (var type in OIDC.UserClaims.All)
            if (Value(capPrincipal, type) is { } value)
                identity.AddClaim(new Claim(type, value));

        // name is the full name. Only stands in where cap_identity was not requested.
        if (identity.FindFirst(DefaultClaims.DisplayName) is null
            && Value(capPrincipal, OIDC.Claims.Name) is { } name)
            identity.AddClaim(new Claim(DefaultClaims.DisplayName, name));

        var context = new CapPrincipalContext(identity, capPrincipal, userId, isRefresh);
        foreach (var enricher in enrichers)
            await enricher.EnrichAsync(context, cancellationToken);

        logger.LogDebug("Built the session principal for CAP user {UserId} ({Username}) with {RoleCount} roles, refresh: {IsRefresh}.",
            userId, username, roles.Count, isRefresh);

        return new ClaimsPrincipal(identity);
    }

    private static string? Value(ClaimsPrincipal principal, string type)
        => principal.FindFirst(type)?.Value is { Length: > 0 } value ? value : null;
}
