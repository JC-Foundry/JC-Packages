using JC.Web.Security.Models.Options;

namespace JC.Web.Security.Helpers;

internal static class ValidationHelper
{
    internal static void Validate(SecurityHeaderOptions options)
    {
        // Validate HSTS max-age is not negative
        if (options.HstsMaxAge < TimeSpan.Zero)
            throw new ArgumentException("HSTS max-age cannot be negative.");

        // Validate Permissions-Policy is not whitespace if set
        if (options.PermissionsPolicy is not null && string.IsNullOrWhiteSpace(options.PermissionsPolicy))
            throw new ArgumentException("Permissions-Policy cannot be empty or whitespace. Set to null to disable.");

        // Validate CSP builds without errors
        if (options.ContentSecurityPolicy is not null)
        {
            var builder = new ContentSecurityPolicyBuilder();
            options.ContentSecurityPolicy(builder);
            var result = builder.Build();

            if (result is not null && string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("Content-Security-Policy builder produced an empty policy.");
        }
    }
}