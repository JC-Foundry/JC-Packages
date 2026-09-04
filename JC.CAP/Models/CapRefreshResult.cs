using System.Security.Claims;
using JC.CAP.Enums;

namespace JC.CAP.Models;

/// <summary>
/// The result of a token refresh. <see cref="Principal"/> is set only when <see cref="Outcome"/> is
/// <see cref="CapRefreshOutcome.Refreshed"/>; <see cref="Error"/> only when CAP refused or was unavailable.
/// </summary>
public sealed record CapRefreshResult(CapRefreshOutcome Outcome, ClaimsPrincipal? Principal = null, Exception? Error = null)
{
    public static readonly CapRefreshResult NoRefreshToken = new(CapRefreshOutcome.NoRefreshToken);

    public static CapRefreshResult Refreshed(ClaimsPrincipal principal) => new(CapRefreshOutcome.Refreshed, principal);

    public static CapRefreshResult Refused(Exception error) => new(CapRefreshOutcome.Refused, Error: error);

    public static CapRefreshResult Unavailable(Exception error) => new(CapRefreshOutcome.Unavailable, Error: error);
}
