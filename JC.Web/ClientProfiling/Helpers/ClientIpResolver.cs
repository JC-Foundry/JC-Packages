using Microsoft.AspNetCore.Http;

namespace JC.Web.ClientProfiling.Helpers;

/// <summary>
/// Resolves the client IP address from an HTTP request.
/// <para>
/// The primary resolution strategy uses <see cref="ConnectionInfo.RemoteIpAddress"/>, which is
/// the correct source when ASP.NET Core's <c>UseForwardedHeaders()</c> middleware is configured
/// with trusted proxies/networks. The forwarded headers middleware updates <c>RemoteIpAddress</c>
/// from <c>X-Forwarded-For</c> after validating the request came from a trusted source.
/// </para>
/// <para>
/// An optional header fallback mode is available for deployments where
/// forwarded headers middleware cannot be configured (e.g. non-standard proxy setups).
/// <b>Warning:</b> header fallback blindly trusts request headers and is unsafe if the application
/// is directly exposed to the internet without a trusted reverse proxy. Only enable this when the
/// application is exclusively accessed through a trusted proxy chain.
/// </para>
/// </summary>
public static class ClientIpResolver
{
    private const string CloudflareHeader = "CF-Connecting-IP";
    private const string CloudflareIpv6Header = "CF-Connecting-IPv6";
    private const string RealIpHeader = "X-Real-IP";
    private const string ForwardedForHeader = "X-Forwarded-For";
    private const string UnknownIp = "unknown";

    /// <summary>
    /// Resolves the client IP address from the given <see cref="HttpContext"/>.
    /// <para>
    /// By default, returns <see cref="ConnectionInfo.RemoteIpAddress"/> which is correct when
    /// <c>UseForwardedHeaders()</c> is configured with trusted proxies.
    /// </para>
    /// <para>
    /// When <paramref name="useHeaderFallback"/> is <c>true</c> and <c>RemoteIpAddress</c> is not
    /// available, falls back to inspecting forwarded headers in order:
    /// <c>CF-Connecting-IPv6</c> (Cloudflare Pseudo IPv4), <c>CF-Connecting-IP</c> (Cloudflare),
    /// <c>X-Real-IP</c> (nginx), then the first entry in
    /// <c>X-Forwarded-For</c> (general proxies). This fallback is <b>not safe</b> if the
    /// application is directly exposed — forwarded headers can be spoofed by clients.
    /// </para>
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="useHeaderFallback">
    /// When <c>true</c>, falls back to inspecting forwarded headers if <c>RemoteIpAddress</c> is unavailable.
    /// Only enable when the application is behind a trusted proxy and not directly exposed. Defaults to <c>false</c>.
    /// </param>
    /// <returns>The resolved client IP address string, or <c>"unknown"</c> if no IP could be determined.</returns>
    public static string Resolve(HttpContext context, bool useHeaderFallback = false)
    {
        // Primary: RemoteIpAddress — correct after UseForwardedHeaders() with trusted proxies
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
            return remoteIp.ToString();

        if (!useHeaderFallback)
            return UnknownIp;

        // Fallback: manual header inspection (unsafe without trusted proxy chain)
        return ResolveFromHeaders(context) ?? UnknownIp;
    }

    private static string? ResolveFromHeaders(HttpContext context)
    {
        var headers = context.Request.Headers;

        // Cloudflare — CF-Connecting-IPv6 holds the real IPv6 when Pseudo IPv4 overwrites CF-Connecting-IP
        if (TryGetHeaderValue(headers, CloudflareIpv6Header, out var cfIpv6))
            return cfIpv6;

        // Cloudflare — single IP from Cloudflare edge
        if (TryGetHeaderValue(headers, CloudflareHeader, out var cfIp))
            return cfIp;

        // nginx / reverse proxy — single IP
        if (TryGetHeaderValue(headers, RealIpHeader, out var realIp))
            return realIp;

        // General proxies — comma-separated, first entry is the original client
        if (headers.TryGetValue(ForwardedForHeader, out var forwardedFor))
        {
            var value = forwardedFor.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                var commaIndex = value.IndexOf(',');
                var firstIp = (commaIndex >= 0 ? value[..commaIndex] : value).Trim();
                if (!string.IsNullOrEmpty(firstIp))
                    return firstIp;
            }
        }

        return null;
    }

    private static bool TryGetHeaderValue(IHeaderDictionary headers, string headerName, out string value)
    {
        value = string.Empty;
        if (!headers.TryGetValue(headerName, out var headerValue))
            return false;

        var trimmed = headerValue.ToString().Trim();
        if (string.IsNullOrEmpty(trimmed))
            return false;

        value = trimmed;
        return true;
    }
}