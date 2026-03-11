using JC.Core.Models;
using JC.Identity.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Identity.Middleware;

/// <summary>
/// Middleware that enforces identity business rules: disabled account redirection,
/// password change enforcement, and optional 2FA enforcement. Skips static files,
/// unauthenticated requests, and excluded paths.
/// </summary>
public class IdentityMiddleware(RequestDelegate next, IOptions<IdentityMiddlewareOptions> options, ILogger<IdentityMiddleware> logger)
{
    private readonly IdentityMiddlewareOptions _options = options.Value;

    private static readonly string[] StaticFileExtensions =
    [
        ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".svg", ".ico",
        ".woff", ".woff2", ".ttf", ".eot", ".map", ".json", ".xml"
    ];

    /// <summary>
    /// Evaluates identity business rules and invokes the next middleware if all checks pass.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="userInfo">The current user information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IUserInfo userInfo)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip static files
        if (IsStaticFile(path))
        {
            await next(context);
            return;
        }

        // Skip if not authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await next(context);
            return;
        }

        // Skip excluded paths
        if (IsExcludedPath(path))
        {
            await next(context);
            return;
        }

        // Check if user is disabled first (before any other checks)
        if (!userInfo.IsEnabled)
        {
            logger.LogWarning("Disabled user {UserId} attempted to access {Path} — redirecting to access denied.", userInfo.UserId, path);
            context.Response.Redirect(_options.AccessDeniedRoute);
            return;
        }

        // Check password change (before 2FA)
        if (_options.RequirePasswordChange && userInfo.RequiresPasswordChange)
        {
            if (!path.StartsWith(_options.ChangePasswordRoute, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("User {UserId} requires password change — redirecting from {Path}.", userInfo.UserId, path);
                context.Response.Redirect(_options.ChangePasswordRoute);
                return;
            }
        }

        // Check 2FA enforcement (only if password is changed)
        if (_options.EnforceTwoFactor && !userInfo.TwoFactorEnabled)
        {
            if (!path.StartsWith(_options.TwoFactorRoute, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("User {UserId} requires 2FA setup — redirecting from {Path}.", userInfo.UserId, path);
                context.Response.Redirect(_options.TwoFactorRoute);
                return;
            }
        }

        await next(context);
    }

    private static bool IsStaticFile(string path)
    {
        return StaticFileExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsExcludedPath(string path)
    {
        return _options.ExcludedPaths.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }
}
