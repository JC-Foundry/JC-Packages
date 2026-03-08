using JC.Core.Models.Auditing;
using JC.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace JC.Core.Data;

/// <summary>
/// Default EF Core DbContext implementation for the core data model.
/// Configures <see cref="AuditEntry"/> entities and automatically creates audit trail
/// entries on save via <see cref="AuditService"/>.
/// </summary>
public class DataDbContext : DbContext, IDataDbContext
{
    /// <summary>
    /// Initialises a new instance of <see cref="DataDbContext"/> with the specified options.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public DataDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    public DbSet<AuditEntry> AuditEntries { get; set; }

    /// <inheritdoc cref="SaveChangesAsync" />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditService = new AuditService(this, null);
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

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.UserId).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.TableName).HasMaxLength(256);
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.AuditDate).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TableName);
            entity.HasIndex(e => e.AuditDate);
        });
    }
}