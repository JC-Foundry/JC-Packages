using CAP.SSO.Enums;

namespace JC.CAP.Models;

/// <summary>
/// CAP's API refused or failed a call. <see cref="Reason"/> is CAP's machine-readable half when it sent one;
/// <see cref="Exception.Message"/> is prose for a log and must not be matched on.
/// </summary>
public class CapApiException : Exception
{
    /// <summary>The HTTP status CAP answered with, or <c>0</c> when the failure was obtaining the access token.</summary>
    public int StatusCode { get; }

    /// <summary>Why CAP refused, when it said. <see cref="ApiErrorReason.ApplicationUnavailable"/> needs a CAP operator: do not retry.</summary>
    public ApiErrorReason? Reason { get; }

    /// <summary>The OIDC error code, when the token endpoint refused to issue an access token.</summary>
    public string? OidcError { get; }

    /// <summary>Whether CAP is not currently serving this application. Nothing the caller does will clear it.</summary>
    public bool IsApplicationUnavailable => Reason == ApiErrorReason.ApplicationUnavailable;

    public CapApiException(string message, int statusCode = 0, ApiErrorReason? reason = null,
        string? oidcError = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Reason = reason;
        OidcError = oidcError;
    }
}
