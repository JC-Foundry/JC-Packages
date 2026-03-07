using JC.Web.Observability.Models;
using JC.Web.Observability.Models.Options;

namespace JC.Web.Observability.Services;

/// <summary>
/// Resolves geographic location from an IP address.
/// JC.Web does not ship a built-in implementation — consumers should implement this
/// interface using their chosen provider (e.g. MaxMind GeoLite2, IP2Location, ip-api).
/// When registered in DI, the request metadata middleware will automatically enrich
/// <see cref="RequestMetadata"/> with the resolved <see cref="GeoLocation"/>.
/// </summary>
public interface IGeoLocationProvider
{
    /// <summary>
    /// Resolves the geographic location for the given IP address.
    /// </summary>
    /// <param name="ipAddress">The client IP address to look up.</param>
    /// <param name="options">Controls the granularity of the lookup (region, city).</param>
    /// <returns>A <see cref="GeoLocation"/> if the lookup succeeded; <c>null</c> if the IP could not be resolved.</returns>
    GeoLocation? Resolve(string ipAddress, GeoLocationOptions options);

    /// <summary>
    /// Asynchronously resolves the geographic location for the given IP address.
    /// Override for API-based providers that require async HTTP calls.
    /// The default implementation delegates to the synchronous <see cref="Resolve"/> method.
    /// </summary>
    /// <param name="ipAddress">The client IP address to look up.</param>
    /// <param name="options">Controls the granularity of the lookup (region, city).</param>
    /// <returns>A <see cref="GeoLocation"/> if the lookup succeeded; <c>null</c> if the IP could not be resolved.</returns>
    Task<GeoLocation?> ResolveAsync(string ipAddress, GeoLocationOptions options)
        => Task.FromResult(Resolve(ipAddress, options));
}