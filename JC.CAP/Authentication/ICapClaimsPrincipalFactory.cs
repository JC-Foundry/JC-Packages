using System.Security.Claims;

namespace JC.CAP.Authentication;

/// <summary>Builds the session principal from the one CAP returned, at sign-in and on every refresh.</summary>
/// <remarks>Registered with <c>TryAdd</c>, so an application can replace it as JC.Identity lets one replace <c>IUserClaimsPrincipalFactory</c>.</remarks>
public interface ICapClaimsPrincipalFactory
{
    /// <summary>Translates CAP's principal into the cookie's vocabulary, then runs the enrichers.</summary>
    /// <param name="capPrincipal">The merged principal OpenIddict built from CAP's tokens and userinfo.</param>
    /// <param name="isRefresh"><c>true</c> when rebuilding after a token refresh rather than at sign-in.</param>
    /// <param name="cancellationToken">Cancels the enrichers.</param>
    /// <returns>The principal to sign the session cookie in with.</returns>
    Task<ClaimsPrincipal> CreateAsync(ClaimsPrincipal capPrincipal, bool isRefresh, CancellationToken cancellationToken = default);
}
