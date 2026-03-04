using JC.Core.Data;
using JC.Core.Models;
using JC.Core.Models.Auditing;
using JC.Identity.Extensions;
using JC.Identity.Models;
using JC.Identity.Models.MultiTenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JC.Identity.Data;

public class IdentityDataDbContext<TUser, TRole> : IdentityDbContext<TUser, TRole, string>, IDataDbContext
    where TUser : BaseUser
    where TRole : BaseRole
{
    private readonly IUserInfo _userInfo;

    public IdentityDataDbContext(DbContextOptions options, IUserInfo userInfo) : base(options)
    {
        _userInfo = userInfo;
    }

    public DbSet<ReportedIssue> ReportedIssues => Set<ReportedIssue>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.Domain);
        });

        modelBuilder.ApplyTenantQueryFilters(_userInfo);
    }
}