using JC.Communication.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Notifications.Data.DataMappings;

public class NotificationStyleMap : IEntityTypeConfiguration<NotificationStyle>
{
    public void Configure(EntityTypeBuilder<NotificationStyle> builder)
    {
        builder.HasKey(s => s.NotificationId);
        builder.Property(s => s.NotificationId).HasMaxLength(36);

        builder.Property(s => s.CustomColourClass).HasMaxLength(128);
        builder.Property(s => s.CustomIconClass).HasMaxLength(128);
    }
}
