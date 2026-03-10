using JC.Communication.Logging.Models.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Logging.Data.DataMappings.Notifications;

public class NotificationLogMap : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasMaxLength(36);

        builder.Property(l => l.NotificationId).IsRequired().HasMaxLength(36);
        builder.Property(l => l.UserId).IsRequired().HasMaxLength(36);
        builder.Property(l => l.IsRead).IsRequired();

        builder.HasIndex(l => l.NotificationId);
        builder.HasIndex(l => l.UserId);
    }
}
