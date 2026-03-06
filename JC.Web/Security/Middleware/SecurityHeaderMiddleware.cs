using JC.Web.Security.Helpers;
using JC.Web.Security.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace JC.Web.Security.Middleware;

/// <summary>
/// Middleware that applies security headers to all HTTP responses based on <see cref="SecurityHeaderOptions"/>.
/// Header values are pre-computed at construction time to avoid per-request overhead.
/// </summary>
public class SecurityHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeaderOptions _options;

    // Pre-computed header values (null = don't add)
    private readonly string? _xContentTypeOptions;
    private readonly string? _xFrameOptions;
    private readonly string? _referrerPolicy;
    private readonly string? _permissionsPolicy;
    private readonly string? _crossOriginOpenerPolicy;
    private readonly string? _crossOriginResourcePolicy;
    private readonly string? _crossOriginEmbedderPolicy;
    private readonly string? _contentSecurityPolicy;
    private readonly string? _strictTransportSecurity;

    public SecurityHeaderMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeaderOptions> options,
        IHostEnvironment environment)
    {
        _next = next;
        _options = options.Value;

        // Pre-compute all header values
        _xContentTypeOptions = _options.EnableXContentTypeOptions ? "nosniff" : null;
        _xFrameOptions = HeaderEnumMapping.GetXFrameOptions(_options.XFrameOptions);
        _referrerPolicy = HeaderEnumMapping.GetReferrerPolicy(_options.ReferrerPolicy);
        _permissionsPolicy = _options.PermissionsPolicy;
        _crossOriginOpenerPolicy = HeaderEnumMapping.GetCrossOriginOpenerPolicy(_options.CrossOriginOpenerPolicy);
        _crossOriginResourcePolicy = HeaderEnumMapping.GetCrossOriginResourcePolicy(_options.CrossOriginResourcePolicy);
        _crossOriginEmbedderPolicy = HeaderEnumMapping.GetCrossOriginEmbedderPolicy(_options.CrossOriginEmbedderPolicy);

        // Build CSP if configured
        if (_options.ContentSecurityPolicy is not null)
        {
            var builder = new ContentSecurityPolicyBuilder();
            _options.ContentSecurityPolicy(builder);
            _contentSecurityPolicy = builder.Build();
        }

        // HSTS — respect production-only toggle
        if (_options.EnableHsts && (!_options.HstsProductionOnly || environment.IsProduction()))
        {
            var maxAge = (long)_options.HstsMaxAge.TotalSeconds;
            _strictTransportSecurity = _options.HstsIncludeSubDomains
                ? $"max-age={maxAge}; includeSubDomains"
                : $"max-age={maxAge}";
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Add security headers (null = skip)
            SetHeader(headers, "X-Content-Type-Options", _xContentTypeOptions);
            SetHeader(headers, "X-Frame-Options", _xFrameOptions);
            SetHeader(headers, "Referrer-Policy", _referrerPolicy);
            SetHeader(headers, "Permissions-Policy", _permissionsPolicy);
            SetHeader(headers, "Cross-Origin-Opener-Policy", _crossOriginOpenerPolicy);
            SetHeader(headers, "Cross-Origin-Resource-Policy", _crossOriginResourcePolicy);
            SetHeader(headers, "Cross-Origin-Embedder-Policy", _crossOriginEmbedderPolicy);
            SetHeader(headers, "Content-Security-Policy", _contentSecurityPolicy);

            // HSTS must only be sent over HTTPS
            if (context.Request.IsHttps)
                SetHeader(headers, "Strict-Transport-Security", _strictTransportSecurity);

            // Remove headers that leak server info
            if (_options.RemoveServerHeader)
                headers.Remove("Server");

            if (_options.RemoveXPoweredByHeader)
                headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static void SetHeader(IHeaderDictionary headers, string name, string? value)
    {
        if (value is not null)
            headers[name] = value;
    }
}
