using Hangfire;
using JC.Core.Models;

namespace JC.BackgroundJobs.Models;

/// <summary>
/// Configuration options for a recurring Hangfire job registered via
/// <c>AddHangfireJob&lt;TJob&gt;</c>.
/// </summary>
public class HangfireJobOptions
{
    /// <summary>Gets or sets the cron expression for the recurring schedule. Defaults to every minute.</summary>
    public string Cron { get; set; } = "* * * * *";

    /// <summary>Gets or sets the Hangfire queue name. Defaults to <c>"default"</c>.</summary>
    public string Queue { get; set; } = "default";

    /// <summary>Gets or sets the unique job identifier. Defaults to the job type name.</summary>
    public string? JobId { get; set; }

    /// <summary>Gets or sets the time zone for cron evaluation. Defaults to <see cref="TimeZoneInfo.Utc"/>.</summary>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;

    /// <summary>Gets or sets how missed job executions are handled. Defaults to <see cref="MisfireHandlingMode.Relaxed"/>.</summary>
    public MisfireHandlingMode MisfireHandling { get; set; } = MisfireHandlingMode.Relaxed;
}

/// <summary>
/// Typed wrapper around <see cref="HangfireJobOptions"/> so that each job type
/// gets its own options instance in DI.
/// </summary>
public class HangfireJobOptionsFor<TJob>(HangfireJobOptions options)
    where TJob : class, IBackgroundJob
{
    /// <summary>Gets the underlying <see cref="HangfireJobOptions"/> instance.</summary>
    public HangfireJobOptions Value { get; } = options;
}
