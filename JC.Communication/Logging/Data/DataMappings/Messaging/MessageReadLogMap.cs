using JC.Communication.Logging.Models.Messaging;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Logging.Data.DataMappings.Messaging;

public class MessageReadLogMap : IEntityTypeConfiguration<MessageReadLog>
{
    public void Configure(EntityTypeBuilder<MessageReadLog> builder)
    {
        builder.HasKey(l => new { l.MessageId, l.UserId });

        builder.Property(l => l.MessageId).IsRequired().HasMaxLength(36);
        builder.Property(l => l.UserId).IsRequired().HasMaxLength(36);
        builder.Property(l => l.ReadAtUtc).IsRequired().HasPrecision(0);

        builder.HasIndex(l => l.MessageId);
        builder.HasIndex(l => l.UserId);

        builder = LogModelMapping<MessageReadLog>.MapLogModel(builder);
    }
}
