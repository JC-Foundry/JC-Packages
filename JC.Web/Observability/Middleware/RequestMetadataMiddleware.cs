using JC.Web.Observability.Helpers;
using JC.Web.Observability.Models;
using JC.Web.Observability.Models.Options;
using JC.Web.Observability.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace JC.Web.Observability.Middleware;

/// <summary>
/// Middleware that builds <see cref="RequestMetadata"/> early in the pipeline
/// and stores it in <see cref="HttpContext.Items"/> for downstream access.
/// Resolves the client IP via <see cref="ClientIpResolver"/>, parses the user agent
/// via <see cref="UserAgentService"/>, and optionally enriches with geolocation data
/// if an <see cref="IGeoLocationProvider"/> is registered.
/// Retrieve via <see cref="HttpContextExtensions.GetRequestMetadata"/>.
/// </summary>
public class RequestMetadataMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Builds request metadata from the current context and stores it
    /// in <see cref="HttpContext.Items"/> before invoking the next middleware.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        UserAgentService userAgentService,
        IGeoLocationProvider? geoLocationProvider = null,
        IOptions<GeoLocationOptions>? geoLocationOptions = null)
    {
        var request = context.Request;

        var clientIp = ClientIpResolver.Resolve(context);
        var userAgent = userAgentService.Parse(request.Headers.UserAgent.ToString());

        GeoLocation? geoLocation = null;
        if (geoLocationProvider != null)
        {
            var options = geoLocationOptions?.Value ?? new GeoLocationOptions();
            geoLocation = await geoLocationProvider.ResolveAsync(clientIp, options);
        }

        var metadata = new RequestMetadata(
            clientIp: clientIp,
            agent: userAgent,
            isHttps: request.IsHttps,
            requestTimestamp: DateTimeOffset.UtcNow,
            geoLocation: geoLocation,
            requestPath: $"{request.Method} {request.Path}",
            requestQuery: request.QueryString.ToString(),
            requestOrigin: request.Headers.Origin.ToString(),
            requestReferer: request.Headers.Referer.ToString(),
            requestId: context.TraceIdentifier);

        context.Items[typeof(RequestMetadata)] = metadata;

        await next(context);
    }
}