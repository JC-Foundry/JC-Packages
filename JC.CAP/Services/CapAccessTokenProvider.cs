using CAP.SSO.Models;
using JC.CAP.Authentication;
using JC.CAP.Models;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Client;

namespace JC.CAP.Services;

/// <summary>
/// The client-credentials token JC.CAP calls CAP's API with: one per process, renewed under a lock shortly
/// before it expires, and discarded when CAP answers 401 so the next call fetches a fresh one.
/// </summary>
public class CapAccessTokenProvider(
    OpenIddictClientService client,
    TimeProvider clock,
    ILogger<CapAccessTokenProvider> logger) : IDisposable
{
    private static readonly TimeSpan RenewAhead = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile CachedToken? _cached;

    /// <summary>Gets a token that is good for at least the next thirty seconds, requesting one where needed.</summary>
    /// <exception cref="CapApiException">CAP refused to issue one. <see cref="CapApiException.OidcError"/> says why.</exception>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (Usable(_cached) is { } token)
            return token.Value;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (Usable(_cached) is { } renewed)
                return renewed.Value;

            var requested = await RequestAsync(cancellationToken);
            _cached = requested;

            return requested.Value;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Discards the held token, after CAP has refused it.</summary>
    public void Invalidate() => _cached = null;

    private CachedToken? Usable(CachedToken? cached)
        => cached is not null && clock.GetUtcNow() < cached.Expires - RenewAhead ? cached : null;

    private async Task<CachedToken> RequestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await client.AuthenticateWithClientCredentialsAsync(new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
            {
                RegistrationId = CapDefaults.RegistrationId,
                Scopes = [OIDC.Scopes.CapApi],
                CancellationToken = cancellationToken
            });

            // No expiry means no caching: the next call asks again rather than trusting a guess.
            return new CachedToken(result.AccessToken, result.AccessTokenExpirationDate ?? DateTimeOffset.MinValue);
        }
        catch (OpenIddictExceptions.ProtocolException ex)
        {
            logger.LogError("CAP refused to issue an API token ({Error}): {Description}", ex.Error, ex.ErrorDescription);

            throw new CapApiException($"CAP refused to issue an API token: {ex.ErrorDescription ?? ex.Error}",
                oidcError: ex.Error, innerException: ex);
        }
    }

    public void Dispose() => _lock.Dispose();

    private sealed record CachedToken(string Value, DateTimeOffset Expires);
}
