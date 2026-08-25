using JC.Core.Models;
using JC.Identity.Shared.Models;
using JC.Identity.Shared.Models.Options;
using Microsoft.Extensions.Logging;

namespace JC.Identity.Shared.Helpers;

/// <summary>
/// Evaluates the identity business rules (disabled accounts, required password changes and optional
/// two-factor setup) against the path being requested, under whichever rule set applies to it.
/// </summary>
public static class IdentityRules
{
    private static readonly string[] StaticFileExtensions =
    [
        ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".svg", ".ico",
        ".woff", ".woff2", ".ttf", ".eot", ".map", ".json", ".xml"
    ];

    /// <summary>
    /// Gets the route the caller should be sent to under whichever rule set applies, or <c>null</c>
    /// where the request may proceed.
    /// </summary>
    /// <param name="userInfo">The current user information.</param>
    /// <param name="path">The path being requested.</param>
    /// <param name="isAuthenticated">Whether the caller is authenticated.</param>
    /// <param name="options">The rule sets to choose between.</param>
    /// <param name="logger">Optional logger recording why a caller was redirected.</param>
    /// <param name="services">The request's services, passed to the conditions. Optional.</param>
    /// <returns>The route to redirect to, or <c>null</c> to continue.</returns>
    public static string? GetRedirect(IUserInfo userInfo, string path, bool isAuthenticated,
        IdentityMiddlewareOptions options, ILogger? logger = null, IServiceProvider? services = null)
    {
        // Selected after the cheap checks, so a condition never runs on a stylesheet.
        if (!isAuthenticated || IsStaticFile(path)) return null;

        var context = new IdentityRuleContext(path, isAuthenticated, userInfo, services);

        return Evaluate(userInfo, path, SelectRuleSet(context, options), logger);
    }

    /// <summary>
    /// Gets the route the caller should be sent to under a known rule set, or <c>null</c> where the
    /// request may proceed.
    /// </summary>
    /// <param name="userInfo">The current user information.</param>
    /// <param name="path">The path being requested.</param>
    /// <param name="isAuthenticated">Whether the caller is authenticated.</param>
    /// <param name="ruleSet">The rule set to apply.</param>
    /// <param name="logger">Optional logger recording why a caller was redirected.</param>
    /// <returns>The route to redirect to, or <c>null</c> to continue.</returns>
    public static string? GetRedirect(IUserInfo userInfo, string path, bool isAuthenticated,
        IdentityRuleSet ruleSet, ILogger? logger = null)
    {
        if (!isAuthenticated || IsStaticFile(path)) return null;

        return Evaluate(userInfo, path, ruleSet, logger);
    }

    /// <summary>
    /// Gets the first rule set whose condition matches, or <see cref="IdentityMiddlewareOptions.Default"/>
    /// where none does.
    /// </summary>
    /// <param name="context">What the conditions decide on.</param>
    /// <param name="options">The rule sets to choose between.</param>
    /// <returns>The rule set applying to this request.</returns>
    /// <remarks>
    /// Public because a caller that has to name a route itself, such as a link to the change-password
    /// page, needs the same set the rules would have used.
    /// </remarks>
    public static IdentityRuleSet SelectRuleSet(IdentityRuleContext context, IdentityMiddlewareOptions options)
        => options.RuleSets.FirstOrDefault(r => r.Condition is null || r.Condition(context)) ?? options.Default;

    private static string? Evaluate(IUserInfo userInfo, string path, IdentityRuleSet ruleSet, ILogger? logger)
    {
        if (IsExcludedPath(path, ruleSet)) return null;

        // Disabled beats everything else: a disabled account should not be routed to a
        // password-change or 2FA page it has no business completing.
        if (!userInfo.IsEnabled)
        {
            logger?.LogWarning("Disabled user {UserId} attempted to access {Path}, redirecting to access denied under rule set {RuleSet}.",
                userInfo.UserId, path, ruleSet.Name);
            return ruleSet.AccessDeniedRoute;
        }

        if (ruleSet.RequirePasswordChange && userInfo.RequiresPasswordChange
            && !path.StartsWith(ruleSet.ChangePasswordRoute, StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogInformation("User {UserId} requires password change, redirecting from {Path} under rule set {RuleSet}.",
                userInfo.UserId, path, ruleSet.Name);
            return ruleSet.ChangePasswordRoute;
        }

        if (ruleSet.EnforceTwoFactor && !userInfo.TwoFactorEnabled
            && !path.StartsWith(ruleSet.TwoFactorRoute, StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogInformation("User {UserId} requires 2FA setup, redirecting from {Path} under rule set {RuleSet}.",
                userInfo.UserId, path, ruleSet.Name);
            return ruleSet.TwoFactorRoute;
        }

        return null;
    }

    private static bool IsStaticFile(string path)
        => StaticFileExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    private static bool IsExcludedPath(string path, IdentityRuleSet ruleSet)
        => ruleSet.ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
}
