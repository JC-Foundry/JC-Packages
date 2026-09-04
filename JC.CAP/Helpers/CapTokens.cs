using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using static OpenIddict.Client.AspNetCore.OpenIddictClientAspNetCoreConstants;

namespace JC.CAP.Helpers;

/// <summary>The CAP tokens the session cookie carries, stored under OpenIddict's own token names.</summary>
internal static class CapTokens
{
    /// <summary>Copies the tokens an OpenIddict result holds onto the cookie's properties.</summary>
    public static void Store(AuthenticationProperties target, AuthenticationProperties? source)
    {
        if (source is null) return;

        Store(target,
            source.GetTokenValue(Tokens.BackchannelAccessToken),
            Expiration(source.GetTokenValue(Tokens.BackchannelAccessTokenExpirationDate)),
            source.GetTokenValue(Tokens.BackchannelIdentityToken),
            source.GetTokenValue(Tokens.RefreshToken));
    }

    /// <summary>Stores the tokens, replacing any already held.</summary>
    public static void Store(AuthenticationProperties target, string? accessToken, DateTimeOffset? accessTokenExpiration,
        string? identityToken, string? refreshToken)
    {
        var tokens = new List<AuthenticationToken>(4);

        if (!string.IsNullOrEmpty(accessToken))
            tokens.Add(new AuthenticationToken { Name = Tokens.BackchannelAccessToken, Value = accessToken });

        if (accessTokenExpiration is { } expiration)
            tokens.Add(new AuthenticationToken
            {
                Name = Tokens.BackchannelAccessTokenExpirationDate,
                Value = expiration.ToString("o", CultureInfo.InvariantCulture)
            });

        if (!string.IsNullOrEmpty(identityToken))
            tokens.Add(new AuthenticationToken { Name = Tokens.BackchannelIdentityToken, Value = identityToken });

        if (!string.IsNullOrEmpty(refreshToken))
            tokens.Add(new AuthenticationToken { Name = Tokens.RefreshToken, Value = refreshToken });

        target.StoreTokens(tokens);
    }

    public static string? AccessToken(AuthenticationProperties? properties)
        => properties?.GetTokenValue(Tokens.BackchannelAccessToken);

    public static DateTimeOffset? AccessTokenExpiration(AuthenticationProperties? properties)
        => Expiration(properties?.GetTokenValue(Tokens.BackchannelAccessTokenExpirationDate));

    public static string? IdentityToken(AuthenticationProperties? properties)
        => properties?.GetTokenValue(Tokens.BackchannelIdentityToken);

    public static string? RefreshToken(AuthenticationProperties? properties)
        => properties?.GetTokenValue(Tokens.RefreshToken);

    private static DateTimeOffset? Expiration(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
            ? date
            : null;
}
