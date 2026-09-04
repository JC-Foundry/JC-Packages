using System.Security.Claims;
using JC.CAP.Authentication;
using JC.CAP.Enums;
using JC.CAP.Helpers;
using JC.CAP.Models.Options;
using JC.CAP.Services;
using JC.Core.Helpers;
using JC.Identity.Shared.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Client.AspNetCore;
using static OpenIddict.Client.AspNetCore.OpenIddictClientAspNetCoreConstants;

namespace JC.CAP.Extensions;

/// <summary>Extension methods for <see cref="IEndpointRouteBuilder"/> mapping JC.CAP's endpoints.</summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps JC.CAP's local endpoints: the sign-in trigger, the callback CAP returns the code to, sign-out,
    /// the callback CAP returns to after ending its session, and the three re-check endpoints the identity
    /// rules and the application send a session to. Paths come from <see cref="CapOptions"/>.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <returns>The route builder for chaining.</returns>
    /// <remarks>
    /// Endpoints rather than a controller, so a Razor Pages application that never mapped controllers still
    /// gets them. Both callbacks accept POST as well as GET, since CAP may answer with a form post.
    /// </remarks>
    public static IEndpointRouteBuilder MapCap(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<CapOptions>>().Value;
        string[] getOrPost = [HttpMethods.Get, HttpMethods.Post];

        // Cast to Delegate where a handler takes only HttpContext: it would otherwise bind as a
        // RequestDelegate and its IResult be discarded.
        endpoints.MapGet(options.SignInPath, SignIn)
                 .AllowAnonymous();

        endpoints.MapMethods(options.CallbackPath, getOrPost, Callback)
                 .AllowAnonymous()
                 .DisableAntiforgery();

        endpoints.MapPost(options.SignOutPath, (Delegate)SignOut)
                 .AllowAnonymous();

        endpoints.MapMethods(options.PostLogoutCallbackPath, getOrPost, (Delegate)PostLogoutCallback)
                 .AllowAnonymous()
                 .DisableAntiforgery();

        endpoints.MapGet(options.RefreshPath, Refresh)
                 .AllowAnonymous();

        endpoints.MapGet(options.DeniedPath, Denied)
                 .AllowAnonymous();

        endpoints.MapGet(options.TwoFactorPath, TwoFactor)
                 .AllowAnonymous();

        return endpoints;
    }

    /// <summary>Starts a sign-in by challenging CAP, returning to <c>returnUrl</c> afterwards.</summary>
    private static IResult SignIn(HttpContext context,
        [FromQuery(Name = CapEndpoints.ReturnUrlParameter)] string? returnUrl)
    {
        var target = LocalUrlHelper.OrDefault(returnUrl);

        if (context.User.Identity?.IsAuthenticated == true)
            return Results.LocalRedirect(target);

        var properties = new AuthenticationProperties { RedirectUri = target };
        properties.SetString(Properties.RegistrationId, CapDefaults.RegistrationId);

        return Results.Challenge(properties, [OpenIddictClientAspNetCoreDefaults.AuthenticationScheme]);
    }

    /// <summary>CAP has returned the code: OpenIddict has exchanged it, so build the session and sign in.</summary>
    private static async Task<IResult> Callback(HttpContext context,
        [FromServices] ICapClaimsPrincipalFactory factory,
        [FromServices] IOptions<CapOptions> options)
    {
        var result = await context.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
        if (result.Principal is null)
            throw new InvalidOperationException("The CAP callback carried no principal.", result.Failure);

        var principal = await factory.CreateAsync(result.Principal, isRefresh: false, context.RequestAborted);

        // The return URL is redirected to below rather than stored, so it never rides in the cookie.
        var properties = new AuthenticationProperties { IsPersistent = options.Value.Session.Persistent };
        CapTokens.Store(properties, result.Properties);

        await context.SignInAsync(CapDefaults.AuthenticationScheme, principal, properties);

        return Results.LocalRedirect(LocalUrlHelper.OrDefault(result.Properties?.RedirectUri));
    }

    /// <summary>Ends the session here, then at CAP, returning to <c>returnUrl</c> afterwards.</summary>
    private static async Task<IResult> SignOut(HttpContext context)
    {
        // Validated where the application registered antiforgery, which Razor Pages and MVC both do.
        var antiforgery = context.RequestServices.GetService<IAntiforgery>();
        if (antiforgery is not null && !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var target = LocalUrlHelper.OrDefault(await ReturnUrlAsync(context));

        var session = await context.AuthenticateAsync(CapDefaults.AuthenticationScheme);
        await context.SignOutAsync(CapDefaults.AuthenticationScheme);

        if (!session.Succeeded)
            return Results.LocalRedirect(target);

        var properties = new AuthenticationProperties { RedirectUri = target };
        properties.SetString(Properties.RegistrationId, CapDefaults.RegistrationId);

        if (CapTokens.IdentityToken(session.Properties) is { } identityToken)
            properties.SetString(Properties.IdentityTokenHint, identityToken);

        return Results.SignOut(properties, [OpenIddictClientAspNetCoreDefaults.AuthenticationScheme]);
    }

    /// <summary>CAP has ended its session: land on the return URL stored at sign-out.</summary>
    private static async Task<IResult> PostLogoutCallback(HttpContext context)
    {
        var result = await context.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);

        return Results.LocalRedirect(LocalUrlHelper.OrDefault(result.Properties?.RedirectUri));
    }

    /// <summary>Refreshes the tokens now and re-reads CAP's live state, then returns to <c>returnUrl</c>.</summary>
    private static async Task<IResult> Refresh(HttpContext context,
        [FromQuery(Name = CapEndpoints.ReturnUrlParameter)] string? returnUrl,
        [FromServices] CapSessionRefresher refresher)
    {
        var target = LocalUrlHelper.OrDefault(returnUrl);

        var session = await context.AuthenticateAsync(CapDefaults.AuthenticationScheme);
        if (!session.Succeeded)
            return Results.LocalRedirect(target);

        var result = await refresher.RefreshAsync(session.Properties!, context.RequestAborted);
        switch (result.Outcome)
        {
            case CapRefreshOutcome.Refreshed:
                await ReissueAsync(context, result.Principal!, session.Properties!);
                break;

            case CapRefreshOutcome.Refused:
            case CapRefreshOutcome.NoRefreshToken:
                // Ending the session lets the next protected request challenge CAP, which decides afresh.
                await context.SignOutAsync(CapDefaults.AuthenticationScheme);
                break;

            // Unavailable: the session stands, and the cookie events keep trying.
        }

        return Results.LocalRedirect(target);
    }

    /// <summary>Where the rules send a disabled account: re-check with CAP, then hand over to CAP's denied page.</summary>
    private static Task<IResult> Denied(HttpContext context,
        [FromQuery(Name = CapEndpoints.ReturnUrlParameter)] string? returnUrl,
        [FromServices] CapSessionRefresher refresher,
        [FromServices] CapLinks links)
        => ReCheckAsync(context, returnUrl, refresher, DefaultClaims.IsEnabled, links.Denied, refusedToCapPage: true);

    /// <summary>Where the rules send an account owing two-factor: re-check with CAP, then hand over to enrolment.</summary>
    private static Task<IResult> TwoFactor(HttpContext context,
        [FromQuery(Name = CapEndpoints.ReturnUrlParameter)] string? returnUrl,
        [FromServices] CapSessionRefresher refresher,
        [FromServices] CapLinks links)
        => ReCheckAsync(context, returnUrl, refresher, DefaultClaims.TwoFactorEnabled, links.EnableAuthenticator, refusedToCapPage: false);

    // Both rule-set destinations share one shape: refresh, and if the claim now says the rule is satisfied go
    // back in, otherwise hand over to CAP's page. The cookie is a snapshot, so without the refresh a user just
    // back from CAP would be sent straight there again.
    private static async Task<IResult> ReCheckAsync(HttpContext context, string? returnUrl, CapSessionRefresher refresher,
        string claimType, string capPage, bool refusedToCapPage)
    {
        var target = LocalUrlHelper.OrDefault(returnUrl);

        var session = await context.AuthenticateAsync(CapDefaults.AuthenticationScheme);
        if (!session.Succeeded)
            return Results.LocalRedirect(target);

        var result = await refresher.RefreshAsync(session.Properties!, context.RequestAborted);
        switch (result.Outcome)
        {
            case CapRefreshOutcome.Refreshed:
                await ReissueAsync(context, result.Principal!, session.Properties!);
                return Flag(result.Principal!, claimType)
                    ? Results.LocalRedirect(target)
                    : Results.Redirect(capPage);

            case CapRefreshOutcome.Refused:
                await context.SignOutAsync(CapDefaults.AuthenticationScheme);
                return refusedToCapPage ? Results.Redirect(capPage) : Results.LocalRedirect(target);

            case CapRefreshOutcome.NoRefreshToken:
                // Nothing to re-check with, so the next challenge has CAP decide.
                await context.SignOutAsync(CapDefaults.AuthenticationScheme);
                return Results.LocalRedirect(target);

            default:
                // Unavailable: the session stands, and CAP's page is the only place left to send them.
                return Results.Redirect(capPage);
        }
    }

    // Cleared so the cookie slides from now; the stored timestamps would otherwise carry the old expiry forward.
    private static Task ReissueAsync(HttpContext context, ClaimsPrincipal principal, AuthenticationProperties properties)
    {
        properties.IssuedUtc = null;
        properties.ExpiresUtc = null;

        return context.SignInAsync(CapDefaults.AuthenticationScheme, principal, properties);
    }

    // The same reading the projection makes of a boolean claim.
    private static bool Flag(ClaimsPrincipal principal, string claimType)
        => string.Equals(principal.FindFirst(claimType)?.Value, "true", StringComparison.OrdinalIgnoreCase);

    // Query first, then the form, so a sign-out button can carry it either way.
    private static async Task<string?> ReturnUrlAsync(HttpContext context)
    {
        if (context.Request.Query.TryGetValue(CapEndpoints.ReturnUrlParameter, out var query))
            return query;

        if (context.Request.HasFormContentType
            && (await context.Request.ReadFormAsync()).TryGetValue(CapEndpoints.ReturnUrlParameter, out var form))
            return form;

        return null;
    }
}
