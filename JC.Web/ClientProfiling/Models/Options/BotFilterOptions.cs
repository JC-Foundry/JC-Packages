namespace JC.Web.ClientProfiling.Models.Options;

/// <summary>
/// Configuration for the bot filtering middleware.
/// Controls which bots are blocked, which paths are protected, and what status code is returned.
/// </summary>
public class BotFilterOptions
{
    /// <summary>
    /// The HTTP status code returned to blocked bots.
    /// Must be one of: 204, 400, 401, 403, or 404. Defaults to <c>403</c>.
    /// </summary>
    public BotFilterStatusCode StatusCode { get; set; } = BotFilterStatusCode.Forbidden;

    /// <summary>
    /// Bot browser names (case-insensitive) that are allowed through the filter.
    /// Matched against <see cref="UserAgent.Browser"/>.
    /// When empty, no bots are allowed (all detected bots are blocked).
    /// </summary>
    public HashSet<string> AllowedBots { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional path filter predicate. When set, only requests matching this predicate
    /// are subject to bot filtering. When <c>null</c>, all paths are filtered.
    /// </summary>
    /// <example>
    /// <code>
    /// options.PathFilter = path => path.StartsWithSegments("/api");
    /// </code>
    /// </example>
    public Func<string, bool>? PathFilter { get; set; }

    /// <summary>
    /// Whether bot filtering is enabled. When <c>false</c>, the <see cref="Middleware.BotFilterMiddleware"/>
    /// passes all requests through without inspection. Defaults to <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// HTTP status codes that can be returned by the bot filtering middleware.
/// </summary>
public enum BotFilterStatusCode
{
    /// <summary>204 No Content</summary>
    NoContent = 204,

    /// <summary>400 Bad Request</summary>
    BadRequest = 400,

    /// <summary>401 Unauthorized</summary>
    Unauthorized = 401,

    /// <summary>403 Forbidden</summary>
    Forbidden = 403,

    /// <summary>404 Not Found</summary>
    NotFound = 404
}