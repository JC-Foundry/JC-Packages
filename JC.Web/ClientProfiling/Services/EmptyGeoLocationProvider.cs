using JC.Web.ClientProfiling.Models;
using JC.Web.ClientProfiling.Models.Options;

namespace JC.Web.ClientProfiling.Services;

public class EmptyGeoLocationProvider : IGeoLocationProvider
{
    public GeoLocation? Resolve(string ipAddress, GeoLocationOptions options) => null;
}