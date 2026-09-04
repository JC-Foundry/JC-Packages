using JC.CAP.Models;
using JC.CAP.Models.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.CAP.Services;

/// <summary>
/// The application's live members, read from CAP's users API and kept in memory for a configurable window.
/// Each member is a cache entry of its own, so a single lookup costs nothing, and the whole set is refreshed
/// together whenever the window has passed or any entry has gone.
/// </summary>
public class CapUserCache(
    IMemoryCache cache,
    CapApiClient client,
    IOptions<CapOptions> options,
    TimeProvider clock,
    ILogger<CapUserCache> logger) : IDisposable
{
    private const string KeyPrefix = "jc-cap:user:";

    private readonly SemaphoreSlim _lock = new(1, 1);

    // The ids and when they were read, replaced as one so a reader never sees half a refresh.
    private volatile Membership? _membership;

    /// <summary>Every member, from the cache where it is fresh and complete, otherwise from CAP.</summary>
    public async Task<IReadOnlyList<CapUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.Cache.Enabled)
            return await LoadAsync(cancellationToken);

        return TryAssemble(_membership, out var users) ? users : await RefreshAsync(force: false, cancellationToken);
    }

    /// <summary>One member by CAP account id, or <c>null</c> where they are not a member.</summary>
    public async Task<CapUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (!options.Value.Cache.Enabled)
            return Find(await LoadAsync(cancellationToken), userId);

        if (cache.TryGetValue(Key(userId), out CapUser? user) && user is not null)
            return user;

        // A fresh list that does not name the id is an answer, so a stranger never reaches CAP.
        var membership = _membership;
        if (membership is not null && IsFresh(membership) && !membership.UserIds.Contains(userId, StringComparer.Ordinal))
            return null;

        return Find(await RefreshAsync(force: false, cancellationToken), userId);
    }

    /// <summary>Reads every member from CAP now, replacing what is held.</summary>
    public Task<IReadOnlyList<CapUser>> RefreshAsync(CancellationToken cancellationToken = default)
        => RefreshAsync(force: true, cancellationToken);

    /// <summary>Drops everything held, so the next read goes to CAP.</summary>
    public void Invalidate()
    {
        var membership = Interlocked.Exchange(ref _membership, null);
        if (membership is null) return;

        foreach (var userId in membership.UserIds)
            cache.Remove(Key(userId));
    }

    private async Task<IReadOnlyList<CapUser>> RefreshAsync(bool force, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Another request may have refreshed while this one waited for the lock.
            if (!force && TryAssemble(_membership, out var current))
                return current;

            var users = await LoadAsync(cancellationToken);
            var lifetime = options.Value.Cache.UserLifetime;

            foreach (var user in users)
                cache.Set(Key(user.Id), user, lifetime);

            _membership = new Membership(users.Select(u => u.Id).ToList(), clock.GetUtcNow());

            logger.LogDebug("Refreshed {Count} CAP users into the cache.", users.Count);

            return users;
        }
        finally
        {
            _lock.Release();
        }
    }

    // Complete only when the list is fresh and every entry is still held: one evicted entry refreshes all.
    private bool TryAssemble(Membership? membership, out IReadOnlyList<CapUser> users)
    {
        users = [];

        if (membership is null || !IsFresh(membership))
            return false;

        var assembled = new List<CapUser>(membership.UserIds.Count);
        foreach (var userId in membership.UserIds)
        {
            if (!cache.TryGetValue(Key(userId), out CapUser? user) || user is null)
                return false;

            assembled.Add(user);
        }

        users = assembled;
        return true;
    }

    private bool IsFresh(Membership membership)
        => clock.GetUtcNow() - membership.RefreshedAt < options.Value.Cache.UserLifetime;

    // Live members only: account enabled and membership enabled, which is what "the application's users" means.
    private async Task<IReadOnlyList<CapUser>> LoadAsync(CancellationToken cancellationToken)
    {
        var members = await client.GetUsersAsync(enabledAccounts: true, cancellationToken: cancellationToken);

        return members.Select(member => new CapUser(member)).ToList();
    }

    private static CapUser? Find(IReadOnlyList<CapUser> users, string userId)
        => users.FirstOrDefault(user => string.Equals(user.Id, userId, StringComparison.Ordinal));

    private static string Key(string userId) => $"{KeyPrefix}{userId}";

    public void Dispose() => _lock.Dispose();

    private sealed record Membership(IReadOnlyList<string> UserIds, DateTimeOffset RefreshedAt);
}
