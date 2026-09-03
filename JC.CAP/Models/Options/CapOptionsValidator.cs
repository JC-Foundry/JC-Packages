using CAP.SSO.Models;
using Microsoft.Extensions.Options;

namespace JC.CAP.Models.Options;

/// <summary>Fails startup on a registration that could only fail later, at the first redirect to CAP.</summary>
internal sealed class CapOptionsValidator : IValidateOptions<CapOptions>
{
    public ValidateOptionsResult Validate(string? name, CapOptions options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer)
            || (issuer.Scheme != Uri.UriSchemeHttps && issuer.Scheme != Uri.UriSchemeHttp))
            failures.Add("CAP:Issuer must be an absolute http or https URL.");

        if (string.IsNullOrWhiteSpace(options.ClientId))
            failures.Add("CAP:ClientId is required.");

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            failures.Add("CAP:ClientSecret is required.");

        if (!options.Scopes.Contains(OIDC.Scopes.OpenId))
            failures.Add($"CAP:Scopes must include {OIDC.Scopes.OpenId}.");

        foreach (var (label, path) in LocalPaths(options))
            if (string.IsNullOrEmpty(path) || path[0] != '/')
                failures.Add($"CAP:{label} must be a local path starting with '/'.");

        if (options.Session.Lifetime <= TimeSpan.Zero)
            failures.Add("CAP:Session:Lifetime must be positive.");

        if (options.Session.RefreshSkew < TimeSpan.Zero || options.Session.RefreshFailureGrace < TimeSpan.Zero)
            failures.Add("CAP:Session:RefreshSkew and RefreshFailureGrace cannot be negative.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static IEnumerable<(string Label, string Path)> LocalPaths(CapOptions o)
    {
        yield return (nameof(o.CallbackPath), o.CallbackPath);
        yield return (nameof(o.PostLogoutCallbackPath), o.PostLogoutCallbackPath);
        yield return (nameof(o.LoginPath), o.LoginPath);
        yield return (nameof(o.LogoutPath), o.LogoutPath);
        yield return (nameof(o.RefreshPath), o.RefreshPath);
        yield return (nameof(o.DeniedPath), o.DeniedPath);
        yield return (nameof(o.TwoFactorPath), o.TwoFactorPath);
    }
}
