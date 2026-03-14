using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Messaging.Data.DataMappings;

public class ChatMessageMap : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasMaxLength(36);

        builder.Property(m => m.ThreadId).IsRequired().HasMaxLength(36);
        builder.Property(m => m.Message).IsRequired().HasMaxLength(8192);
        builder.Property(m => m.ReplyToMessageId).HasMaxLength(36);

        builder.HasOne(m => m.ReplyToMessage)
            .WithMany()
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.ThreadId);

        builder = AuditModelMapping<ChatMessage>.MapAuditModel(builder);
    }
}
