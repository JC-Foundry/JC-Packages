namespace JC.Web.Observability.Models.Options;

/// <summary>
/// Controls the granularity of geographic location resolution.
/// Passed to <see cref="Abstractions.IGeoLocationProvider"/> implementations
/// so they can skip unnecessary lookups when finer-grained data is not needed.
/// </summary>
public class GeoLocationOptions
{
    /// <summary>
    /// Whether to include region/state/province in the result. Defaults to <c>true</c>.
    /// </summary>
    public bool IncludeRegion { get; set; } = true;

    /// <summary>
    /// Whether to include city/town in the result. Defaults to <c>false</c>.
    /// </summary>
    public bool IncludeCity { get; set; } = false;
}