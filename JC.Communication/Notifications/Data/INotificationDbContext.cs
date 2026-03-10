using JC.Communication.Logging.Models.Notifications;
using JC.Communication.Notifications.Models;
using Microsoft.EntityFrameworkCore;

// Located here because it includes domain models, not just logging models.
namespace JC.Communication.Notifications.Data;

/// <summary>
/// Marker interface for a DbContext that supports notification entities.
/// Must be implemented by the consuming application's DbContext to enable
/// notification persistence and logging.
/// </summary>
public interface INotificationDbContext
{
    /// <summary>Gets or sets the notifications table.</summary>
    DbSet<Notification> Notifications { get; set; }

    /// <summary>Gets or sets the notification styles table.</summary>
    DbSet<NotificationStyle> NotificationStyles { get; set; }

    /// <summary>Gets or sets the notification logs table.</summary>
    DbSet<NotificationLog> NotificationLogs { get; set; }
}