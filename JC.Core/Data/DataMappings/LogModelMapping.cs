using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Core.Data.DataMappings;

public static class LogModelMapping<T>
    where T : LogModel
{
    public static EntityTypeBuilder<T> MapLogModel(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.CreatedById).HasMaxLength(36);
        builder.Property(e => e.CreatedUtc).HasPrecision(0);

        builder.HasIndex(e => e.CreatedById);
        builder.HasIndex(e => e.CreatedUtc);

        return builder;
    }
}