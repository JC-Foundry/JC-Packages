using System.Security.Claims;

namespace JC.CAP.Authentication;

/// <summary>What an <see cref="ICapClaimsEnricher"/> is given: the identity being built, and where it came from.</summary>
public sealed class CapPrincipalContext
{
    /// <summary>The session identity under construction. Add claims here.</summary>
    public ClaimsIdentity Identity { get; }

    /// <summary>The principal as CAP returned it, before translation.</summary>
    public ClaimsPrincipal CapPrincipal { get; }

    /// <summary>The CAP account id, the <c>sub</c> claim.</summary>
    public string UserId { get; }

    /// <summary>Whether this is a rebuild after a token refresh rather than a sign-in.</summary>
    public bool IsRefresh { get; }

    public CapPrincipalContext(ClaimsIdentity identity, ClaimsPrincipal capPrincipal, string userId, bool isRefresh)
    {
        Identity = identity;
        CapPrincipal = capPrincipal;
        UserId = userId;
        IsRefresh = isRefresh;
    }
}
