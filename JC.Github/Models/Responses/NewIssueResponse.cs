namespace JC.Github.Models.Responses;

public class NewIssueResponse
{
    public long Id { get; set; }
    public int Number { get; set; }
    public string? Title { get; set; }
    public string? State { get; set; }
    public string? Html_Url { get; set; }
}