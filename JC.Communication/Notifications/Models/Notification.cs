using System.ComponentModel.DataAnnotations;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Notifications.Models;

/// <summary>
/// Represents an in-app notification targeted at a specific user.
/// Supports plain text and HTML body content, optional custom styling,
/// expiration, and read/unread state managed via domain methods.
/// </summary>
public sealed class Notification : AuditModel
{
    /// <summary>Gets the unique identifier for this notification.</summary>
    [Key]
    [MaxLength(36)]
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the notification title.</summary>
    [Required]
    [MaxLength(255)]
    public string Title { get; set; }

    /// <summary>Gets or sets the plain text body of the notification.</summary>
    [Required]
    [MaxLength(8192)]
    public string Body { get; set; }

    /// <summary>Gets or sets the optional HTML body of the notification.</summary>
    public string? BodyHtml { get; set; }

    /// <summary>Gets or sets the identifier of the user this notification is for.</summary>
    [Required]
    public string UserId { get; set; }

    /// <summary>Gets or sets the notification type. Defaults to <see cref="NotificationType.Info"/>.</summary>
    public NotificationType Type { get; set; } = NotificationType.Info;

    /// <summary>Gets whether this notification has been read.</summary>
    public bool IsRead { get; private set; }

    /// <summary>Gets the UTC date and time when the notification was read, or <c>null</c> if unread.</summary>
    public DateTime? ReadAtUtc { get; private set; }

    /// <summary>Gets or sets the optional UTC expiration date and time for this notification.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>Gets or sets an optional URL link associated with the notification.</summary>
    public string? UrlLink { get; set; }

    /// <summary>Gets or sets the optional custom styling for this notification.</summary>
    public NotificationStyle? Style { get; set; }

    /// <summary>
    /// Marks the notification as read and records the current UTC time.
    /// </summary>
    public void Read()
    {
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the notification as unread and clears the read timestamp.
    /// </summary>
    public void Unread()
    {
        IsRead = false;
        ReadAtUtc = null;
    }
}