using JC.Core.Data;
using JC.Core.Models;
using JC.Core.Models.Auditing;
using JC.Core.Services;
using JC.Identity.Extensions;
using JC.Identity.Models;
using JC.Identity.Models.MultiTenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JC.Identity.Data;

/// <summary>
/// Identity-aware data context extending <see cref="IdentityDbContext{TUser, TRole, TKey}"/> and implementing
/// <see cref="IDataDbContext"/>. Configures core entities, tenant entities, and applies multi-tenancy query filters.
/// </summary>
/// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
/// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
public class IdentityDataDbContext<TUser, TRole> : IdentityDbContext<TUser, TRole, string>, IDataDbContext
    where TUser : BaseUser
    where TRole : BaseRole
{
    private readonly IUserInfo _userInfo;

    /// <summary>
    /// Initialises a new instance of <see cref="IdentityDataDbContext{TUser, TRole}"/>.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    /// <param name="userInfo">The current user information, used for tenant query filters.</param>
    public IdentityDataDbContext(DbContextOptions options, IUserInfo userInfo) : base(options)
    {
        _userInfo = userInfo;
    }

    /// <summary>
    /// The current user's tenant identifier. Referenced by global query filters — EF Core
    /// re-evaluates this property per query rather than caching the value at model creation time.
    /// </summary>
    public string? CurrentTenantId => _userInfo.TenantId;

    /// <inheritdoc />
    public DbSet<AuditEntry> AuditEntries { get; set; }
    
    /// <summary>Gets the set of tenants.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <inheritdoc cref="SaveChangesAsync" />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditService = new AuditService(this, _userInfo);
        var pendingCreates = await auditService.ProcessChangesAsync(ChangeTracker);
        var result = await base.SaveChangesAsync(cancellationToken);
        if (pendingCreates.Count > 0)
        {
            await auditService.ProcessCreatesAsync(pendingCreates);
            await base.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

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

        modelBuilder.Entity<TUser>(entity =>
        {
            entity.Property(e => e.TenantId).HasMaxLength(36);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Domain).HasMaxLength(256);
            entity.HasIndex(e => e.Domain);
        });

        modelBuilder.ApplyTenantQueryFilters(this);
    }
}