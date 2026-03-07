namespace JC.Web.Observability.Models;

/// <summary>
/// Represents the geographic location resolved from a client IP address.
/// Populated by an <see cref="Abstractions.IGeoLocationProvider"/> implementation
/// and optionally included in <see cref="RequestMetadata"/> when a provider is registered.
/// </summary>
public class GeoLocation
{
    /// <summary>
    /// The country name (e.g. "United Kingdom", "United States").
    /// </summary>
    public string? Country { get; }

    /// <summary>
    /// The ISO 3166-1 alpha-2 country code (e.g. "GB", "US").
    /// </summary>
    public string? CountryCode { get; }

    /// <summary>
    /// The region, state, province, or county (e.g. "England", "California").
    /// Only populated when <see cref="Options.GeoLocationOptions.IncludeRegion"/> is <c>true</c>.
    /// </summary>
    public string? Region { get; }

    /// <summary>
    /// The city or town (e.g. "London", "San Francisco").
    /// Only populated when <see cref="Options.GeoLocationOptions.IncludeCity"/> is <c>true</c>.
    /// </summary>
    public string? City { get; }

    /// <param name="country">The country name.</param>
    /// <param name="countryCode">The ISO 3166-1 alpha-2 country code.</param>
    /// <param name="region">The region, state, or province.</param>
    /// <param name="city">The city or town.</param>
    public GeoLocation(string? country, string? countryCode, string? region = null, string? city = null)
    {
        Country = country;
        CountryCode = countryCode;
        Region = region;
        City = city;
    }
}