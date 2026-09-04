namespace JC.CAP.Enums;

/// <summary>What a token refresh against CAP came to.</summary>
public enum CapRefreshOutcome
{
    /// <summary>CAP issued new tokens and the principal was rebuilt from its live state.</summary>
    Refreshed,

    /// <summary>CAP refused: access was withdrawn, or the refresh token has expired. The session ends.</summary>
    Refused,

    /// <summary>CAP could not be reached, or answered with a server error. Nothing is known about the account.</summary>
    Unavailable,

    /// <summary>The session holds no refresh token, so it cannot be re-checked before it ends.</summary>
    NoRefreshToken
}
