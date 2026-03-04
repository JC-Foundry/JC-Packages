namespace JC.Core.Models;

public enum IssueType
{
    Suggestion,Bug
}

public class ReportedIssue
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    public IssueType Type { get; set; }
    public required string Description { get; set; }
    
    public byte[]? Image { get; set; }
    public bool ReportSent { get; set; }
    public int? ExternalId { get; set; }
    public bool Closed { get; set; }
    
    public DateTime Created { get; set; }
    
    public string? UserId { get; set; }
    public string? UserDisplay { get; set; }
}