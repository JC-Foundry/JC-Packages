using System.Security.Claims;
using JC.CAP.Authentication;
using JC.CAP.Enums;
using JC.CAP.Helpers;
using JC.CAP.Models.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace JC.CAP.Services;

/// <summary>
/// The session cookie's events. Refreshes the tokens silently as the access token nears expiry, inside
/// cookie authentication so the projection middleware only ever sees the refreshed principal, and turns a
/// role refusal into whatever <see cref="CapOptions.AccessDenied"/> says.
/// </summary>
public class CapCookieEvents(
    CapSessionRefresher refresher,
    CapLinks links,
    IOptions<CapOptions> options,
    TimeProvider clock,
    ILogger<CapCookieEvents> logger) : CookieAuthenticationEvents
{
    /// <inheritdoc />
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var session = options.Value.Session;
        var now = clock.GetUtcNow();
        var expiry = CapTokens.AccessTokenExpiration(context.Properties);

        // Nothing stored to refresh: a principal signed in by something other than the callback.
        if (expiry is null || now < expiry.Value - session.RefreshSkew)
            return;

        var result = await refresher.RefreshAsync(context.Properties, context.HttpContext.RequestAborted);

        switch (result.Outcome)
        {
            case CapRefreshOutcome.Refreshed:
                context.ReplacePrincipal(result.Principal!);
                context.ShouldRenew = true;
                return;

            case CapRefreshOutcome.Refused:
                await RejectAsync(context, "CAP refused the refresh");
                return;

            case CapRefreshOutcome.NoRefreshToken:
                // No offline_access, so the session ends with the access token.
                if (now >= expiry.Value)
                    await RejectAsync(context, "the access token has expired and no refresh token is held");
                return;

            case CapRefreshOutcome.Unavailable:
                // Open through the grace period, so an outage is not a mass sign-out; closed after it.
                if (now >= expiry.Value + session.RefreshFailureGrace)
                    await RejectAsync(context, "CAP has been unreachable past the grace period");
                return;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The framework never answers a forbid with a bare status: it always routes here with a redirect
    /// already built, so a 403 has to be written by the event.
    /// </remarks>
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        switch (options.Value.AccessDenied)
        {
            case CapAccessDenied.Forbid:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;

            case CapAccessDenied.CapDeniedPage:
                // The framework's own shape: a fetch gets the status and a Location, a navigation is sent.
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.Headers.Location = links.Denied;
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                }
                else
                {
                    context.Response.Redirect(links.Denied);
                }
                return Task.CompletedTask;

            default:
                // LocalPath: the cookie's own path, which ConfigureCapCookie set from the options.
                return base.RedirectToAccessDenied(context);
        }
    }

    private async Task RejectAsync(CookieValidatePrincipalContext context, string reason)
    {
        logger.LogInformation("Ending the CAP session for {UserId}: {Reason}.",
            context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, reason);

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CapDefaults.AuthenticationScheme);
    }

    // The framework's test, which it keeps private.
    private static bool IsAjaxRequest(HttpRequest request)
        => string.Equals(request.Query[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal)
           || string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.Ordinal);
}
