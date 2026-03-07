using JC.Web.Observability.Models;
using UAParser;
using UserAgent = JC.Web.Observability.Models.UserAgent;

namespace JC.Web.Observability.Services;

/// <summary>
/// Parses user agent strings into structured <see cref="UserAgent"/> objects
/// using the UAParser library. Maintains a singleton <see cref="Parser"/> instance
/// for efficient repeated parsing.
/// </summary>
public class UserAgentService
{
    private static readonly Parser Parser = Parser.GetDefault();

    /// <summary>
    /// Parses a raw user agent string into a <see cref="UserAgent"/> model.
    /// Returns a model with <see cref="DeviceType.Unknown"/> and null properties
    /// if the user agent string is null or empty.
    /// </summary>
    /// <param name="userAgentString">The raw user agent header value.</param>
    /// <returns>A populated <see cref="UserAgent"/> instance.</returns>
    public UserAgent Parse(string? userAgentString)
    {
        if (string.IsNullOrWhiteSpace(userAgentString))
            return new UserAgent(
                rawValue: string.Empty,
                browser: null,
                browserVersion: null,
                os: null,
                osVersion: null,
                type: DeviceType.Unknown);

        var clientInfo = Parser.Parse(userAgentString);

        var browser = NullIfOther(clientInfo.UA.Family);
        var browserVersion = BuildVersion(clientInfo.UA.Major, clientInfo.UA.Minor, clientInfo.UA.Patch);

        var os = NullIfOther(clientInfo.OS.Family);
        var osVersion = BuildVersion(clientInfo.OS.Major, clientInfo.OS.Minor, clientInfo.OS.Patch);

        var deviceType = ResolveDeviceType(clientInfo, userAgentString);

        return new UserAgent(
            rawValue: userAgentString,
            browser: browser,
            browserVersion: browserVersion,
            os: os,
            osVersion: osVersion,
            type: deviceType);
    }

    private static DeviceType ResolveDeviceType(ClientInfo clientInfo, string rawUa)
    {
        var deviceFamily = clientInfo.Device.Family?.ToLowerInvariant() ?? string.Empty;
        var uaFamily = clientInfo.UA.Family?.ToLowerInvariant() ?? string.Empty;
        var rawLower = rawUa.ToLowerInvariant();

        // Bot detection — check UA family and common bot patterns
        if (uaFamily.Contains("bot") ||
            uaFamily.Contains("crawler") ||
            uaFamily.Contains("spider") ||
            uaFamily.Contains("slurp") ||
            rawLower.Contains("bot/") ||
            rawLower.Contains("crawler") ||
            rawLower.Contains("spider") ||
            rawLower.Contains("headlesschrome") ||
            rawLower.Contains("phantomjs") ||
            rawLower.Contains("lighthouse"))
            return DeviceType.Bot;

        // Tablet detection — check before mobile since tablets often contain "mobile" patterns
        if (deviceFamily.Contains("ipad") ||
            rawLower.Contains("tablet") ||
            (rawLower.Contains("android") && !rawLower.Contains("mobile")))
            return DeviceType.Tablet;

        // Mobile detection
        if (deviceFamily.Contains("iphone") ||
            deviceFamily.Contains("ipod") ||
            rawLower.Contains("mobile") ||
            rawLower.Contains("android"))
            return DeviceType.Mobile;

        // Desktop if we have a recognised OS, otherwise unknown
        var osFamily = clientInfo.OS.Family?.ToLowerInvariant() ?? string.Empty;
        if (osFamily.Contains("windows") ||
            osFamily.Contains("mac os") ||
            osFamily.Contains("linux") ||
            osFamily.Contains("chrome os"))
            return DeviceType.Desktop;

        return DeviceType.Unknown;
    }

    private static string? NullIfOther(string? value)
        => string.IsNullOrEmpty(value) || value.Equals("Other", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;

    private static string? BuildVersion(string? major, string? minor, string? patch)
    {
        if (string.IsNullOrEmpty(major))
            return null;

        if (string.IsNullOrEmpty(minor))
            return major;

        if (string.IsNullOrEmpty(patch) || patch == "0")
            return $"{major}.{minor}";

        return $"{major}.{minor}.{patch}";
    }
}