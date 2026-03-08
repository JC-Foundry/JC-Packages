namespace JC.Web.ClientProfiling.Models.Options;

/// <summary>
/// Configuration for client IP address resolution in <see cref="Helpers.ClientIpResolver"/>.
/// </summary>
public class ClientIpOptions
{
    /// <summary>
    /// When <c>true</c>, the resolver reads forwarded/proxy headers (e.g. <c>CF-Connecting-IP</c>,
    /// <c>X-Real-IP</c>, <c>X-Forwarded-For</c>) <b>before</b> falling back to
    /// <see cref="Microsoft.AspNetCore.Http.ConnectionInfo.RemoteIpAddress"/>.
    /// <para>
    /// Enable this when the application sits behind a trusted reverse proxy (Cloudflare, nginx, etc.)
    /// that sets forwarded headers. In this scenario, <c>RemoteIpAddress</c> is typically the proxy's
    /// local address (e.g. <c>::1</c>), not the real client IP.
    /// </para>
    /// <para>
    /// <b>Warning:</b> Do not enable this if the application is directly exposed to the internet —
    /// clients can spoof forwarded headers. Only enable when all traffic flows through a trusted proxy.
    /// </para>
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool TrustProxyHeaders { get; set; } = false;
}
