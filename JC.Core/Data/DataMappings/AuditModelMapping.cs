using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Core.Data.DataMappings;

public static class AuditModelMapping<T>
    where T : AuditModel
{
    public static EntityTypeBuilder<T> MapAuditModel(EntityTypeBuilder<T> builder)
    {
        // Create properties
        builder.Property(e => e.CreatedById).HasMaxLength(36);
        builder.Property(e => e.CreatedUtc).HasPrecision(0);

        // Modification properties
        builder.Property(e => e.LastModifiedById).HasMaxLength(36);
        builder.Property(e => e.LastModifiedUtc).HasPrecision(0);

        // Soft-delete properties
        builder.Property(e => e.DeletedById).HasMaxLength(36);
        builder.Property(e => e.DeletedUtc).HasPrecision(0);
        builder.Property(e => e.IsDeleted);

        // Restore properties
        builder.Property(e => e.RestoredById).HasMaxLength(36);
        builder.Property(e => e.RestoredUtc).HasPrecision(0);

        builder.HasIndex(e => e.CreatedById);
        builder.HasIndex(e => e.CreatedUtc);
        builder.HasIndex(e => e.LastModifiedById);
        builder.HasIndex(e => e.DeletedById);
        builder.HasIndex(e => e.IsDeleted);
        builder.HasIndex(e => e.RestoredById);

        return builder;
    }
}