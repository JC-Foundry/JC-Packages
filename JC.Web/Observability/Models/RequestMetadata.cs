using System.Text.Json;
using JC.Core.Extensions;

namespace JC.Web.Observability.Models;

/// <summary>
/// Captures structured metadata about an HTTP request including client IP, user agent,
/// protocol, and request properties. Built by the request metadata middleware and stored
/// in <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> for downstream access.
/// </summary>
public class RequestMetadata
{
    public string ClientIp { get; }
    public UserAgent UserAgent { get; }
    public GeoLocation? GeoLocation { get; }
    public bool IsHttps { get; }

    public DateTimeOffset RequestTimestamp { get; }
    public string? RequestPath { get; }
    public string? RequestOrigin { get; }
    public string? RequestReferer { get; }
    public string? RequestId { get; }

    public RequestMetadata(
        string clientIp,
        UserAgent agent,
        bool isHttps,
        DateTimeOffset requestTimestamp,
        GeoLocation? geoLocation = null,
        string? requestPath = null,
        string? requestOrigin = null,
        string? requestReferer = null,
        string? requestId = null)
    {
        ClientIp = clientIp;
        UserAgent = agent;
        GeoLocation = geoLocation;
        IsHttps = isHttps;

        RequestTimestamp = requestTimestamp;
        RequestPath = requestPath;
        RequestOrigin = requestOrigin;
        RequestReferer = requestReferer;
        RequestId = requestId;
    }

    /// <summary>
    /// Returns a JSON string representation of the request metadata for structured logging.
    /// Sensitive properties (client IP, origin, referer, city) are masked by default using
    /// <see cref="StringExtensions.Mask"/> with 0 visible characters.
    /// </summary>
    /// <param name="maskIp">Whether to mask the client IP address. Defaults to <c>true</c>.</param>
    /// <param name="maskOrigin">Whether to mask the request origin. Defaults to <c>true</c>.</param>
    /// <param name="maskReferer">Whether to mask the request referer. Defaults to <c>true</c>.</param>
    /// <param name="maskCity">Whether to mask the city. Defaults to <c>true</c>.</param>
    /// <returns>A JSON string containing all request metadata properties.</returns>
    public string ToLogEntry(bool maskIp = true, bool maskOrigin = true, bool maskReferer = true, bool maskCity = true)
    {
        var entry = new Dictionary<string, object?>
        {
            ["RequestId"] = RequestId,
            ["Timestamp"] = RequestTimestamp.ToString("o"),
            ["ClientIp"] = maskIp ? ClientIp.Mask(0) : ClientIp,
            ["IsHttps"] = IsHttps,
            ["RequestPath"] = RequestPath,
            ["Origin"] = maskOrigin ? RequestOrigin?.Mask(0) : RequestOrigin,
            ["Referer"] = maskReferer ? RequestReferer?.Mask(0) : RequestReferer,
            ["Browser"] = UserAgent.Browser,
            ["BrowserVersion"] = UserAgent.BrowserVersion,
            ["OS"] = UserAgent.OperatingSystem,
            ["OSVersion"] = UserAgent.OperatingSystemVersion,
            ["DeviceType"] = UserAgent.DeviceType.ToString(),
            ["IsBot"] = UserAgent.IsBot,
            ["UserAgent"] = UserAgent.RawValue,
            ["Country"] = GeoLocation?.Country,
            ["CountryCode"] = GeoLocation?.CountryCode,
            ["Region"] = GeoLocation?.Region,
            ["City"] = maskCity ? GeoLocation?.City?.Mask(0) : GeoLocation?.City
        };

        return JsonSerializer.Serialize(entry);
    }
}