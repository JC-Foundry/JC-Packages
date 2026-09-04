using CAP.SSO.Models;
using JC.CAP.Authentication;
using JC.Core.Models;
using Microsoft.Extensions.Logging;

namespace JC.CAP.Services;

/// <summary>
/// Publishes the roles declared on <typeparamref name="TRoles"/> to CAP. Run once at startup by
/// <c>SyncCapRolesAsync</c>, or on a schedule through JC.BackgroundJobs; calling both is harmless.
/// </summary>
/// <typeparam name="TRoles">The application's roles class, extending <see cref="SystemRoles"/>.</typeparam>
public class CapRoleSyncJob<TRoles>(CapApiClient client, ILogger<CapRoleSyncJob<TRoles>> logger) : IBackgroundJob
    where TRoles : SystemRoles
{
    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken cancellationToken = default) => SyncAsync(cancellationToken);

    /// <summary>Publishes the catalogue and returns what CAP did with it. A failure propagates.</summary>
    public async Task<CatalogueSync> SyncAsync(CancellationToken cancellationToken = default)
    {
        var roles = SystemRoles.GetAllRoles<TRoles>();
        var catalogue = SystemRoles.ToCatalogue(roles);
        var sync = await client.PublishRolesAsync(catalogue, cancellationToken);

        logger.LogInformation("Published {Count} roles from {Roles} to CAP. {Display}",
            catalogue.Count, typeof(TRoles).Name, sync.Display);

        if (sync.MarkedStale > 0)
            logger.LogWarning("CAP holds {Count} roles this application no longer publishes. A CAP operator should review them.",
                sync.MarkedStale);

        // A recased key is a bug in the application's source: every role check is case-sensitive.
        foreach (var (sent, held) in sync.Recased)
            logger.LogWarning("Role key '{Sent}' is held by CAP as '{Held}'. Correct the constant to match.", sent, held);

        return sync;
    }
}
