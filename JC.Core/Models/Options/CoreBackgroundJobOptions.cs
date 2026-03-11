namespace JC.Core.Models.Options;

public class CoreBackgroundJobOptions
{
    public bool RegisterAuditCleanupJob { get; set; } = true;
    public ushort AuditRetentionMonths { get; set; } = 6;
    public ushort MinimumRetentionRecords { get; set; } = 30;
    public bool RetentionRecordsPerTable { get; set; } = true;
    public ushort CleanupChunkingValue { get; set; } = 500;


    public bool RegisterSoftDeleteCleanupJob { get; set; } = false;
    public ushort SoftDeleteRetentionMonths { get; set; } = 24;
    public List<string> SoftDeleteRetentionBlacklist { get; private set; } = [];

    public void SetSoftDeleteRetentionBlacklist(params IEnumerable<string> classNames)
        => SoftDeleteRetentionBlacklist = classNames.Select(n => n.ToLowerInvariant()).ToList();
}