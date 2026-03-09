using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JC.BackgroundJobs.Services;

/// <summary>
/// Hosted service that registers all collected recurring Hangfire jobs at startup.
/// </summary>
internal sealed class HangfireJobRegistrationService(
    HangfireJobRegistry registry,
    IRecurringJobManager recurringJobManager,
    IServiceProvider serviceProvider,
    ILogger<HangfireJobRegistrationService> logger) : IHostedService
{
    /// <summary>Registers all collected recurring Hangfire jobs with the <see cref="IRecurringJobManager"/>.</summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering {Count} recurring Hangfire job(s)", registry.Registrations.Count);

        foreach (var registration in registry.Registrations)
            registration(recurringJobManager, serviceProvider);

        return Task.CompletedTask;
    }

    /// <summary>No-op — nothing to clean up on shutdown.</summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
