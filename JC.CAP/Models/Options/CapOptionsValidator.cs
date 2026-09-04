using CAP.SSO.Models;
using JC.CAP.Enums;
using Microsoft.Extensions.Options;

namespace JC.CAP.Models.Options;

/// <summary>Fails startup on a registration that could only fail later, at the first redirect to CAP.</summary>
internal sealed class CapOptionsValidator : IValidateOptions<CapOptions>
{
    public ValidateOptionsResult Validate(string? name, CapOptions options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl)
            || (baseUrl.Scheme != Uri.UriSchemeHttps && baseUrl.Scheme != Uri.UriSchemeHttp))
            failures.Add($"{CapDictionary.BaseUrlKey} is required and must be an absolute http or https URL.");

        if (string.IsNullOrWhiteSpace(options.ClientId))
            failures.Add($"{Key(nameof(options.ClientId))} is required.");

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            failures.Add($"{Key(nameof(options.ClientSecret))} is required.");

        if (!options.Scopes.Contains(OIDC.Scopes.OpenId))
            failures.Add($"{Key(nameof(options.Scopes))} must include {OIDC.Scopes.OpenId}.");

        // Without it is_enabled never arrives, and an absent claim reads as a disabled account.
        if (!options.Scopes.Contains(OIDC.Scopes.CapIdentity))
            failures.Add($"{Key(nameof(options.Scopes))} must include {OIDC.Scopes.CapIdentity}.");

        foreach (var (label, path) in LocalPaths(options))
            if (string.IsNullOrEmpty(path) || path[0] != '/')
                failures.Add($"{Key(label)} must be a local path starting with '/'.");

        if (options.AccessDenied == CapAccessDenied.LocalPath
            && (string.IsNullOrEmpty(options.AccessDeniedPath) || options.AccessDeniedPath[0] != '/'))
            failures.Add($"{Key(nameof(options.AccessDeniedPath))} must be a local path starting with '/' when {Key(nameof(options.AccessDenied))} is {nameof(CapAccessDenied.LocalPath)}.");

        if (options.Session.Lifetime <= TimeSpan.Zero)
            failures.Add($"{Key("Session:Lifetime")} must be positive.");

        if (options.Session.RefreshSkew < TimeSpan.Zero || options.Session.RefreshFailureGrace < TimeSpan.Zero)
            failures.Add($"{Key("Session:RefreshSkew")} and {Key("Session:RefreshFailureGrace")} cannot be negative.");

        // IMemoryCache refuses a non-positive expiry.
        if (options.Cache.UserLifetime <= TimeSpan.Zero)
            failures.Add($"{Key("Cache:UserLifetime")} must be positive.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static string Key(string name) => $"{CapOptions.ConfigSection}:{name}";

    private static IEnumerable<(string Label, string Path)> LocalPaths(CapOptions o)
    {
        yield return (nameof(o.CallbackPath), o.CallbackPath);
        yield return (nameof(o.PostLogoutCallbackPath), o.PostLogoutCallbackPath);
        yield return (nameof(o.SignInPath), o.SignInPath);
        yield return (nameof(o.SignOutPath), o.SignOutPath);
        yield return (nameof(o.RefreshPath), o.RefreshPath);
        yield return (nameof(o.DeniedPath), o.DeniedPath);
        yield return (nameof(o.TwoFactorPath), o.TwoFactorPath);
    }
}
