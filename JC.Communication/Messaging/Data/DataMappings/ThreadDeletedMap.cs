using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Messaging.Data.DataMappings;

public class ThreadDeletedMap : IEntityTypeConfiguration<ThreadDeleted>
{
    public void Configure(EntityTypeBuilder<ThreadDeleted> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasMaxLength(36);

        builder.Property(d => d.ThreadId).IsRequired().HasMaxLength(36);
        builder.Property(d => d.UserId).IsRequired().HasMaxLength(36);

        builder.HasIndex(d => d.ThreadId);
        builder.HasIndex(d => d.UserId);

        builder = AuditModelMapping<ThreadDeleted>.MapAuditModel(builder);
    }
}
