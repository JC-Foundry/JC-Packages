using JC.Core.Models;
using JC.Identity.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace JC.Identity.Middleware;

public class IdentityMiddleware(RequestDelegate next, IOptions<IdentityMiddlewareOptions> options)
{
    private readonly RequestDelegate _next = next;
    private readonly IdentityMiddlewareOptions _options = options.Value;

    private static readonly string[] StaticFileExtensions =
    [
        ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".svg", ".ico",
        ".woff", ".woff2", ".ttf", ".eot", ".map", ".json", ".xml"
    ];

    public async Task InvokeAsync(HttpContext context, IUserInfo userInfo)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip static files
        if (IsStaticFile(path))
        {
            await _next(context);
            return;
        }

        // Skip if not authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Skip excluded paths
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // Check if user is disabled first (before any other checks)
        if (!userInfo.IsEnabled)
        {
            context.Response.Redirect(_options.AccessDeniedRoute);
            return;
        }

        // Check password change (before 2FA)
        if (_options.RequirePasswordChange && userInfo.RequiresPasswordChange)
        {
            if (!path.StartsWith(_options.ChangePasswordRoute, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect(_options.ChangePasswordRoute);
                return;
            }
        }

        // Check 2FA enforcement (only if password is sorted)
        if (_options.EnforceTwoFactor && !userInfo.TwoFactorEnabled)
        {
            if (!path.StartsWith(_options.TwoFactorRoute, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect(_options.TwoFactorRoute);
                return;
            }
        }

        await _next(context);
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
