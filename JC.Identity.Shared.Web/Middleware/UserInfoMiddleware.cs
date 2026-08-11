using JC.Core.Models;
using JC.Identity.Shared.Extensions;
using JC.Identity.Shared.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Identity.Shared.Web.Middleware;

/// <summary>
/// Middleware that populates <see cref="IUserInfo"/> from the current principal on the first
/// request per scope.
/// </summary>
/// <remarks>
/// The projection itself is <see cref="UserInfoExtensions.PopulateFrom{T}(T, System.Security.Claims.ClaimsPrincipal, IdentityProjectionOptions, ILogger)"/>
/// in JC.Identity.Shared, so an authority with no HTTP pipeline reaches the same behaviour.
/// </remarks>
public class UserInfoMiddleware(RequestDelegate next, ILogger<UserInfoMiddleware> logger)
{
    /// <summary>
    /// Populates the scoped <see cref="IUserInfo"/> instance and invokes the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var userInfo = context.RequestServices.GetRequiredService<IUserInfo>();

        if (!userInfo.IsSetup)
        {
            var options = context.RequestServices
                .GetRequiredService<IOptions<IdentityProjectionOptions>>().Value;

            userInfo.PopulateFrom(context.User, options, logger);
        }

        await next(context);
    }
}
