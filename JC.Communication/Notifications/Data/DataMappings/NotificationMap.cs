using JC.Communication.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Notifications.Data.DataMappings;

public class NotificationMap : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasMaxLength(36);

        builder.Property(n => n.Title).IsRequired().HasMaxLength(255);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(8192);
        builder.Property(n => n.UserId).IsRequired().HasMaxLength(36);
        builder.Property(n => n.Type).IsRequired().HasConversion<int>();

        builder.Property(n => n.IsRead).IsRequired();

        builder.HasOne(n => n.Style)
            .WithOne(s => s.Notification)
            .HasForeignKey<NotificationStyle>(s => s.NotificationId);

        builder.HasIndex(n => n.UserId);
    }
}
