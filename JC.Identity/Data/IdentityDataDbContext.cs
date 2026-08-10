using JC.Core.Data;
using JC.Core.Data.DataMappings;
using JC.Core.Models;
using JC.Core.Models.Auditing;
using JC.Core.Services;
using JC.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace JC.Identity.Data;

/// <summary>
/// Identity-aware data context extending <see cref="IdentityDbContext{TUser, TRole, TKey}"/> and
/// implementing <see cref="IDataDbContext"/>. Configures the Identity model and the audit trail.
/// </summary>
/// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
/// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
/// <remarks>
/// Does <b>not</b> filter by tenant. A tenant-scoped application derives from this type, implements
/// <c>JC.Tenancy.Data.ITenantScopedContext</c> and calls <c>ApplyTenantFilters</c> from its own
/// <c>OnModelCreating</c> — which is what lets a single-tenant application skip JC.Tenancy entirely.
/// <para>
/// <see cref="BaseUser"/> does not implement <see cref="Core.Models.MultiTenancy.IMultiTenancy"/>,
/// so users stay unfiltered even then. Required, not incidental: a filter on the user entity breaks
/// <c>UserManager</c> and <c>SignInManager</c>, which resolve a user before any tenant scope exists.
/// </para>
/// </remarks>
public class IdentityDataDbContext<TUser, TRole> : IdentityDbContext<TUser, TRole, string>, IDataDbContext
    where TUser : BaseUser
    where TRole : BaseRole
{
    private readonly IUserInfo _userInfo;
    private readonly IServiceProvider? _appServices;

    /// <summary>
    /// Initialises a new instance of <see cref="IdentityDataDbContext{TUser, TRole}"/>.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    /// <param name="userInfo">The current user information, used to attribute audit entries.</param>
    public IdentityDataDbContext(DbContextOptions options, IUserInfo userInfo) : base(options)
    {
        _userInfo = userInfo;
        _appServices = options.FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider;
    }

    /// <inheritdoc />
    public DbSet<AuditEntry> AuditEntries { get; set; }

    /// <inheritdoc cref="SaveChangesAsync" />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditService = new AuditService(this, _appServices, _userInfo);
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

        AuditEntryMapping.MapAuditEntry(modelBuilder.Entity<AuditEntry>());

        modelBuilder.Entity<TUser>(entity =>
        {
            entity.Property(e => e.TenantId).HasMaxLength(36);
        });
    }
}
