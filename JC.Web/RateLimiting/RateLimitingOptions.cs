using System.Threading.RateLimiting;

namespace JC.Web.RateLimiting;

/// <summary>
/// Configuration for JC.Web rate limiting middleware.
/// Wraps ASP.NET Core's built-in rate limiting with sensible defaults and simplified partitioning.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Whether rate limiting is enabled. When <c>false</c>, the middleware is not registered.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The rate limiting strategy to apply. Defaults to <see cref="RateLimitingStrategy.FixedWindow"/>.
    /// </summary>
    public RateLimitingStrategy Strategy { get; set; } = RateLimitingStrategy.SlidingWindow;

    /// <summary>
    /// The maximum number of requests permitted within the <see cref="Window"/>.
    /// Used by <see cref="RateLimitingStrategy.FixedWindow"/> and <see cref="RateLimitingStrategy.SlidingWindow"/>.
    /// Defaults to <c>100</c>.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// The time window for rate limiting. Defaults to 1 minute.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How requests are partitioned for rate limiting. Defaults to <see cref="RateLimitPartitionBy.ClientIp"/>.
    /// </summary>
    public RateLimitPartitionBy PartitionBy { get; set; } = RateLimitPartitionBy.ClientIp;

    /// <summary>
    /// The number of segments per window for <see cref="RateLimitingStrategy.SlidingWindow"/>.
    /// Ignored by other strategies. Defaults to <c>6</c>.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 6;

    /// <summary>
    /// The number of tokens added per <see cref="Window"/> for <see cref="RateLimitingStrategy.TokenBucket"/>.
    /// Ignored by other strategies. Defaults to <c>10</c>.
    /// </summary>
    public int TokensPerPeriod { get; set; } = 10;

    /// <summary>
    /// The maximum number of tokens the bucket can hold for <see cref="RateLimitingStrategy.TokenBucket"/>.
    /// Ignored by other strategies. Defaults to <see cref="PermitLimit"/>.
    /// </summary>
    public int TokenLimit { get; set; } = 0;

    /// <summary>
    /// The maximum number of concurrent requests for <see cref="RateLimitingStrategy.Concurrency"/>.
    /// Ignored by other strategies. Defaults to <see cref="PermitLimit"/>.
    /// </summary>
    public int ConcurrencyLimit { get; set; } = 0;

    /// <summary>
    /// The maximum number of requests to queue when the limit is reached.
    /// Queued requests are processed when permits become available. Defaults to <c>0</c> (no queuing).
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// The processing order for queued requests. Defaults to <see cref="QueueProcessingOrder.OldestFirst"/>.
    /// </summary>
    public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;

    /// <summary>
    /// Whether to exclude static file requests from rate limiting.
    /// When <c>true</c>, requests for common static file extensions (.css, .js, .png, .jpg, .jpeg, .gif,
    /// .svg, .ico, .woff, .woff2, .ttf, .eot, .map, .webp, .avif, .bmp) are not counted against the rate limit.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool ExcludeStaticFiles { get; set; } = true;

    private static readonly HashSet<string> StaticFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico",
        ".woff", ".woff2", ".ttf", ".eot", ".map", ".webp", ".avif", ".bmp"
    };

    /// <summary>
    /// Returns whether the given request path is for a static file.
    /// </summary>
    internal static bool IsStaticFile(string path)
    {
        var ext = Path.GetExtension(path);
        return !string.IsNullOrEmpty(ext) && StaticFileExtensions.Contains(ext);
    }
}

/// <summary>
/// The rate limiting algorithm to apply.
/// </summary>
public enum RateLimitingStrategy
{
    /// <summary>Fixed window — resets the counter at the end of each window.</summary>
    FixedWindow,

    /// <summary>Sliding window — divides the window into segments for smoother limiting.</summary>
    SlidingWindow,

    /// <summary>Token bucket — tokens replenish at a fixed rate, allowing controlled bursts.</summary>
    TokenBucket,

    /// <summary>Concurrency — limits the number of concurrent requests rather than rate.</summary>
    Concurrency
}

/// <summary>
/// How requests are partitioned for rate limiting.
/// </summary>
public enum RateLimitPartitionBy
{
    /// <summary>Partition by client IP address, resolved via <see cref="ClientProfiling.Helpers.ClientIpResolver"/>.</summary>
    ClientIp,

    /// <summary>Partition by authenticated user identity. Falls back to endpoint path for anonymous requests.</summary>
    User,

    /// <summary>Partition by request endpoint path.</summary>
    Endpoint,

    /// <summary>Partition by client IP combined with endpoint path.</summary>
    ClientIpAndEndpoint
}
