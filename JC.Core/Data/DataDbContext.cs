using JC.Core.Data.DataMappings;
using JC.Core.Models.Auditing;
using JC.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace JC.Core.Data;

/// <summary>
/// Default EF Core DbContext implementation for the core data model.
/// Configures <see cref="AuditEntry"/> entities and automatically creates audit trail
/// entries on save via <see cref="AuditService"/>.
/// </summary>
public class DataDbContext : DbContext, IDataDbContext
{
    private readonly IServiceProvider? _appServices;

    /// <summary>
    /// Initialises a new instance of <see cref="DataDbContext"/> with the specified options.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public DataDbContext(DbContextOptions options) : base(options)
    {
        _appServices = options.FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider;
    }

    /// <inheritdoc />
    public DbSet<AuditEntry> AuditEntries { get; set; }

    /// <inheritdoc cref="SaveChangesAsync" />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditService = new AuditService(this, _appServices);
        var pendingCreates = await auditService.ProcessChangesAsync(ChangeTracker);
        var result = await base.SaveChangesAsync(cancellationToken);
        if (pendingCreates.Count > 0)
        {
            await auditService.ProcessCreatesAsync(pendingCreates);
            await base.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        AuditEntryMapping.MapAuditEntry(modelBuilder.Entity<AuditEntry>());
    }
}