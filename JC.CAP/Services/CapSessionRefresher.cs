using JC.CAP.Authentication;
using JC.CAP.Helpers;
using JC.CAP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JC.CAP.Services;

/// <summary>
/// Exchanges the session's refresh token with CAP and rebuilds the principal from CAP's live state. Shared by
/// the cookie events, which run it as the access token nears expiry, and the re-check endpoints, which run it
/// on demand.
/// </summary>
public class CapSessionRefresher(
    OpenIddictClientService client,
    ICapClaimsPrincipalFactory factory,
    ILogger<CapSessionRefresher> logger)
{
    // CAP answering "no" and CAP not answering are different outcomes. OpenIddict reports a transport
    // failure, an unparseable response and any 5xx as server_error, and a 503 as temporarily_unavailable.
    private static readonly HashSet<string> UnavailableErrors = new(StringComparer.Ordinal)
    {
        Errors.ServerError, Errors.TemporarilyUnavailable, Errors.SlowDown
    };

    /// <summary>Refreshes the tokens held on <paramref name="properties"/>, replacing them in place on success.</summary>
    /// <param name="properties">The session's authentication properties, carrying the tokens.</param>
    /// <param name="cancellationToken">Cancels the exchange.</param>
    /// <returns>The outcome, with the rebuilt principal when CAP issued new tokens.</returns>
    public virtual async Task<CapRefreshResult> RefreshAsync(AuthenticationProperties properties,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var refreshToken = CapTokens.RefreshToken(properties);
        if (string.IsNullOrEmpty(refreshToken))
            return CapRefreshResult.NoRefreshToken;

        OpenIddictClientModels.RefreshTokenAuthenticationResult result;
        try
        {
            result = await client.AuthenticateWithRefreshTokenAsync(new OpenIddictClientModels.RefreshTokenAuthenticationRequest
            {
                RegistrationId = CapDefaults.RegistrationId,
                RefreshToken = refreshToken,
                CancellationToken = cancellationToken
            });
        }
        catch (OpenIddictExceptions.ProtocolException ex) when (!UnavailableErrors.Contains(ex.Error ?? string.Empty))
        {
            logger.LogInformation("CAP refused the token refresh ({Error}): {Description}", ex.Error, ex.ErrorDescription);
            return CapRefreshResult.Refused(ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "CAP could not be reached for a token refresh.");
            return CapRefreshResult.Unavailable(ex);
        }

        var principal = await factory.CreateAsync(result.Principal, isRefresh: true, cancellationToken);

        // A token CAP did not reissue is kept rather than dropped.
        CapTokens.Store(properties,
            result.AccessToken,
            result.AccessTokenExpirationDate,
            result.IdentityToken ?? CapTokens.IdentityToken(properties),
            result.RefreshToken ?? refreshToken);

        return CapRefreshResult.Refreshed(principal);
    }
}
