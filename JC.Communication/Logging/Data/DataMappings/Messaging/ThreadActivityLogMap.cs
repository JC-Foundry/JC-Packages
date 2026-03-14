using JC.Communication.Logging.Models.Messaging;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Logging.Data.DataMappings.Messaging;

public class ThreadActivityLogMap : IEntityTypeConfiguration<ThreadActivityLog>
{
    public void Configure(EntityTypeBuilder<ThreadActivityLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasMaxLength(36);

        builder.Property(l => l.ThreadId).IsRequired().HasMaxLength(36);
        builder.Property(l => l.ActivityTimestampUtc).IsRequired().HasPrecision(0);
        builder.Property(l => l.ActivityType).IsRequired().HasConversion<int>();
        builder.Property(l => l.ActivityDetails).HasMaxLength(512);

        builder.HasIndex(l => l.ThreadId);

        builder = LogModelMapping<ThreadActivityLog>.MapLogModel(builder);
    }
}
