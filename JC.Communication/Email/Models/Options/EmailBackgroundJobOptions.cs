namespace JC.Communication.Email.Models.Options;

public class EmailBackgroundJobOptions
{
    public bool EnableEmailLogCleanupJob { get; set; } = true;
    
    public ushort EmailLogRetentionMonths { get; set; } = 6;
    public ushort MinimumRetentionRecords { get; set; } = 10;
    
    public ushort EmailLogCleanupChunkingValue { get; set; } = 500;
}