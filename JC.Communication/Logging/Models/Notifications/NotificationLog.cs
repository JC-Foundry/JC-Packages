using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Communication.Notifications.Models;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Logging.Models.Notifications;

/// <summary>
/// Audit record for a notification read or unread event.
/// A new log entry is created each time a notification's read state changes,
/// recording which user performed the action and when.
/// </summary>
public class NotificationLog : LogModel
{
    /// <summary>Gets the unique identifier for this log entry.</summary>
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the identifier of the notification this log relates to.</summary>
    [Required]
    public string NotificationId { get; set; }

    /// <summary>Gets or sets the navigation property to the associated notification.</summary>
    [ForeignKey(nameof(NotificationId))]
    public Notification Notification { get; set; }

    /// <summary>Gets the timestamp of the event, derived from <see cref="AuditModel.CreatedUtc"/>.</summary>
    [NotMapped]
    public DateTime Timestamp => CreatedUtc;

    /// <summary>Gets or sets the identifier of the user who performed the read/unread action.</summary>
    [Required]
    public string UserId { get; set; }

    /// <summary>Gets or sets whether this event represents a read (<c>true</c>) or unread (<c>false</c>) action. Defaults to <c>true</c>.</summary>
    public bool IsRead { get; set; } = true;
}