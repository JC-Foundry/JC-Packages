using JC.Core.Models;
using JC.Identity.Shared.Models.Options;
using Microsoft.Extensions.Logging;

namespace JC.Identity.Shared.Helpers;

/// <summary>
/// Evaluates the identity business rules — disabled accounts, required password changes and
/// optional two-factor setup — against the path being requested.
/// </summary>
public static class IdentityRules
{
    private static readonly string[] StaticFileExtensions =
    [
        ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".svg", ".ico",
        ".woff", ".woff2", ".ttf", ".eot", ".map", ".json", ".xml"
    ];

    /// <summary>
    /// Gets the route the caller should be sent to, or <c>null</c> where the request may proceed.
    /// </summary>
    /// <param name="userInfo">The current user information.</param>
    /// <param name="path">The path being requested.</param>
    /// <param name="isAuthenticated">Whether the caller is authenticated.</param>
    /// <param name="options">The configured routes and which rules are enforced.</param>
    /// <param name="logger">Optional logger recording why a caller was redirected.</param>
    /// <returns>The route to redirect to, or <c>null</c> to continue.</returns>
    public static string? GetRedirect(IUserInfo userInfo, string path, bool isAuthenticated,
        IdentityMiddlewareOptions options, ILogger? logger = null)
    {
        if (!isAuthenticated || IsStaticFile(path) || IsExcludedPath(path, options)) return null;

        // Disabled beats everything else — a disabled account should not be routed to a
        // password-change or 2FA page it has no business completing.
        if (!userInfo.IsEnabled)
        {
            logger?.LogWarning("Disabled user {UserId} attempted to access {Path} — redirecting to access denied.",
                userInfo.UserId, path);
            return options.AccessDeniedRoute;
        }

        if (options.RequirePasswordChange && userInfo.RequiresPasswordChange
            && !path.StartsWith(options.ChangePasswordRoute, StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogInformation("User {UserId} requires password change — redirecting from {Path}.",
                userInfo.UserId, path);
            return options.ChangePasswordRoute;
        }

        if (options.EnforceTwoFactor && !userInfo.TwoFactorEnabled
            && !path.StartsWith(options.TwoFactorRoute, StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogInformation("User {UserId} requires 2FA setup — redirecting from {Path}.",
                userInfo.UserId, path);
            return options.TwoFactorRoute;
        }

        return null;
    }

    private static bool IsStaticFile(string path)
        => StaticFileExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    private static bool IsExcludedPath(string path, IdentityMiddlewareOptions options)
        => options.ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
}
