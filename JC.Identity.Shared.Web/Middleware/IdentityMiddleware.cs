using JC.Core.Models;
using JC.Identity.Shared.Helpers;
using JC.Identity.Shared.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Identity.Shared.Web.Middleware;

/// <summary>
/// Middleware that enforces the identity business rules, redirecting where one is not satisfied.
/// </summary>
/// <remarks>
/// The rules themselves are <see cref="IdentityRules"/> in JC.Identity.Shared; this only supplies
/// the request path and performs the redirect. Must run after <see cref="UserInfoMiddleware"/>.
/// </remarks>
public class IdentityMiddleware(RequestDelegate next, IOptions<IdentityMiddlewareOptions> options,
    ILogger<IdentityMiddleware> logger)
{
    private readonly IdentityMiddlewareOptions _options = options.Value;

    /// <summary>
    /// Evaluates the identity business rules and invokes the next middleware if all checks pass.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="userInfo">The current user information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IUserInfo userInfo)
    {
        var redirect = IdentityRules.GetRedirect(
            userInfo,
            context.Request.Path.Value ?? string.Empty,
            context.User.Identity?.IsAuthenticated ?? false,
            _options,
            logger);

        if (redirect is not null)
        {
            context.Response.Redirect(redirect);
            return;
        }

        await next(context);
    }
}
