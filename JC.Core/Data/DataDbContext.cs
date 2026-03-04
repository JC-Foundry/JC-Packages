using JC.Core.Models;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;

namespace JC.Core.Data;

public class DataDbContext : DbContext, IDataDbContext
{
    public DataDbContext(DbContextOptions<DataDbContext> options) : base(options)
    {
    }

    public DbSet<ReportedIssue> ReportedIssues => Set<ReportedIssue>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

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