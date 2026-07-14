using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Core.Data.DataMappings;

public static class AuditEntryMapping
{
    public static EntityTypeBuilder<AuditEntry> MapAuditEntry(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);
        builder.Property(e => e.UserId).HasMaxLength(256);
        builder.Property(e => e.UserName).HasMaxLength(256);
        builder.Property(e => e.ActionUserId).HasMaxLength(256);
        builder.Property(e => e.SourceApplication).HasMaxLength(256);
        builder.Property(e => e.TableName).HasMaxLength(256);
        builder.Property(e => e.EntityKey).HasMaxLength(512);
        builder.Property(e => e.Action).IsRequired();
        builder.Property(e => e.AuditDate).IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ActionUserId);
        builder.HasIndex(e => e.SourceApplication);
        builder.HasIndex(e => e.TableName);
        builder.HasIndex(e => e.AuditDate);
        builder.HasIndex(e => new { e.TableName, e.EntityKey });

        return builder;
    }
}
