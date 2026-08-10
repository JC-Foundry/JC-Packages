using JC.Core.Data.DataMappings;
using JC.Tenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
// ReSharper disable RedundantAssignment

namespace JC.Tenancy.Data.DataMappings;

public class TenantMap : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(36);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(256);
        builder.Property(t => t.Domain).HasMaxLength(256);

        //Unique so a concurrent add cannot slip past ITenantStore's check. Not applied to Domain:
        //it is nullable, and SQL Server would then allow only one tenant without a domain
        builder.HasIndex(t => t.Name).IsUnique();

        //Tenants are commonly resolved by domain on the way in
        builder.HasIndex(t => t.Domain);

        builder = AuditModelMapping<Tenant>.MapAuditModel(builder);
    }
}
