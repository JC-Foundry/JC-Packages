using System.Globalization;
using Microsoft.Extensions.Logging;

namespace JC.Core.Helpers;

public static class CountryHelper
{
    private static List<Country>? _countries;

    /// <summary>
    /// Gets all countries derived from .NET's culture/region data.
    /// Results are cached after first call.
    /// </summary>
    public static IReadOnlyList<Country> GetCountries(ILogger? logger = null)
    {
        return _countries ??= CultureInfo
            .GetCultures(CultureTypes.SpecificCultures)
            .Select(culture =>
            {
                try
                {
                    var region = new RegionInfo(culture.Name);
                    return new Country(region.TwoLetterISORegionName, region.EnglishName);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to create RegionInfo for culture '{CultureName}'.", culture.Name);
                    return null;
                }
            })
            .Where(c => c != null)
            .DistinctBy(c => c!.Code)
            .OrderBy(c => c!.Name)
            .ToList()!;
    }

    /// <summary>
    /// Gets countries as a dictionary of Code -> Name for dropdown binding.
    /// </summary>
    public static Dictionary<string, string> GetCountriesDictionary()
        => GetCountries().ToDictionary(c => c.Code, c => c.Name);

    /// <summary>
    /// Gets the country name for a given ISO 3166-1 alpha-2 code.
    /// </summary>
    public static string? GetCountryName(string code)
        => GetCountries().FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Name;

    /// <summary>
    /// Gets the country code for a given country name.
    /// </summary>
    public static string? GetCountryCode(string name)
        => GetCountries().FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Code;
}

public record Country(string Code, string Name);
