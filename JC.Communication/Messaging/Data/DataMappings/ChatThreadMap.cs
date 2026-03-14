using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Messaging.Data.DataMappings;

public class ChatThreadMap : IEntityTypeConfiguration<ChatThread>
{
    public void Configure(EntityTypeBuilder<ChatThread> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(36);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(256);
        builder.Property(t => t.Description).HasMaxLength(1024);
        builder.Property(t => t.IsDefaultThread).IsRequired();
        builder.Property(t => t.LastActivityUtc).HasPrecision(0);
        builder.Property(t => t.IsGroupThread).IsRequired();

        builder.HasMany(t => t.Messages)
            .WithOne(m => m.Thread)
            .HasForeignKey(m => m.ThreadId);

        builder.HasMany(t => t.Participants)
            .WithOne(p => p.Thread)
            .HasForeignKey(p => p.ThreadId);

        builder.HasOne(t => t.ChatMetadata)
            .WithOne(m => m.Thread)
            .HasForeignKey<ChatMetadata>(m => m.ThreadId);

        builder.HasMany(t => t.UserThreadDeletions)
            .WithOne(d => d.Thread)
            .HasForeignKey(d => d.ThreadId);

        builder = AuditModelMapping<ChatThread>.MapAuditModel(builder);
    }
}
