using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Notifications.Models;

/// <summary>
/// Optional custom UI styling for a <see cref="Notification"/>. Stored in a separate table
/// to avoid null column bloat on the notification table when no custom styling is needed.
/// At least one of <see cref="CustomColourClass"/> or <see cref="CustomIconClass"/> must be set.
/// </summary>
public class NotificationStyle : AuditModel
{
    /// <summary>Gets or sets the identifier of the associated notification. Also serves as the primary key.</summary>
    [Key]
    public string NotificationId { get; set; }

    /// <summary>Gets or sets the navigation property to the parent notification.</summary>
    [ForeignKey(nameof(NotificationId))]
    public Notification Notification { get; set; }

    /// <summary>Gets or sets the custom CSS colour class, overriding the default derived from <see cref="NotificationType"/>.</summary>
    public string? CustomColourClass { get; set; }

    /// <summary>Gets or sets the custom CSS icon class, overriding the default derived from <see cref="NotificationType"/>.</summary>
    public string? CustomIconClass { get; set; }
}