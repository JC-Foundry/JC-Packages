namespace JC.Communication.Notifications.Models.Options;

public class NotificationBackgroundJobOptions
{
    public bool EnableNotificationLogCleanupJob { get; set; } = true;
    public ushort NotificationLogRetentionMonths { get; set; } = 6;
    public ushort MinimumRetentionRecords { get; set; } = 30;
    public ushort NotificationLogCleanupChunkingValue { get; set; } = 500;
}