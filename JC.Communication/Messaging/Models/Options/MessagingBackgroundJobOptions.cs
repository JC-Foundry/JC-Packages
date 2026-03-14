namespace JC.Communication.Messaging.Models.Options;

public class MessagingBackgroundJobOptions
{
    public bool EnableActivityLogCleanupJob { get; set; } = true;
    public ushort ActivityLogRetentionMonths { get; set; } = 6;
    public ushort ActivityLogMinimumRetentionRecords { get; set; } = 30;
    public ushort ActivityLogCleanupChunkingValue { get; set; } = 500;

    public bool EnableReadLogCleanupJob { get; set; } = true;
    public ushort ReadLogRetentionMonths { get; set; } = 6;
    public ushort ReadLogMinimumRetentionRecords { get; set; } = 30;
    public ushort ReadLogCleanupChunkingValue { get; set; } = 500;

    /// <summary>
    /// When <c>true</c>, the most recent read log per user per message is always retained
    /// regardless of retention period. Takes precedence over <see cref="ReadLogRetentionMonths"/>.
    /// </summary>
    public bool KeepMostRecentReadLog { get; set; } = true;
}
