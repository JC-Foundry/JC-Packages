using JC.Core.Models;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;

namespace JC.Core.Data;

/// <summary>
/// Default EF Core DbContext implementation for the core data model.
/// Configures <see cref="ReportedIssue"/> and <see cref="AuditEntry"/> entities.
/// </summary>
public class DataDbContext : DbContext, IDataDbContext
{
    /// <summary>
    /// Initialises a new instance of <see cref="DataDbContext"/> with the specified options.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public DataDbContext(DbContextOptions<DataDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    public DbSet<ReportedIssue> ReportedIssues => Set<ReportedIssue>();

    /// <inheritdoc />
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportedIssue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired();
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.AuditDate).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TableName);
            entity.HasIndex(e => e.AuditDate);
        });

        base.OnModelCreating(modelBuilder);
    }
}