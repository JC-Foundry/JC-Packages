using System.Text.Json.Serialization;

namespace JC.Github.Models.Responses;

public class NewIssueResponse
{
    public long Id { get; set; }
    public int Number { get; set; }
    public string? Title { get; set; }
    public string? State { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
}