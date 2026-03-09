using Hangfire;

namespace JC.BackgroundJobs.Services;

/// <summary>
/// Collects recurring Hangfire job registrations during service configuration.
/// Processed at startup by <see cref="HangfireJobRegistrationService"/>.
/// </summary>
internal sealed class HangfireJobRegistry
{
    private readonly List<Action<IRecurringJobManager, IServiceProvider>> _registrations = [];

    /// <summary>Adds a recurring job registration action to be processed at startup.</summary>
    /// <param name="registration">The action that registers the job with <see cref="IRecurringJobManager"/>.</param>
    internal void Add(Action<IRecurringJobManager, IServiceProvider> registration)
        => _registrations.Add(registration);

    /// <summary>Gets the collected registration actions.</summary>
    internal IReadOnlyList<Action<IRecurringJobManager, IServiceProvider>> Registrations => _registrations;
}
