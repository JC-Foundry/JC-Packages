using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Messaging.Data.DataMappings;

public class ChatParticipantMap : IEntityTypeConfiguration<ChatParticipant>
{
    public void Configure(EntityTypeBuilder<ChatParticipant> builder)
    {
        builder.HasKey(p => new { p.ThreadId, p.UserId });

        builder.Property(p => p.ThreadId).IsRequired().HasMaxLength(36);
        builder.Property(p => p.UserId).IsRequired().HasMaxLength(36);
        builder.Property(p => p.CanSeeHistory).IsRequired();
        builder.Property(p => p.JoinedAtUtc).IsRequired().HasPrecision(0);

        builder.HasIndex(p => p.UserId);

        builder = AuditModelMapping<ChatParticipant>.MapAuditModel(builder);
    }
}
