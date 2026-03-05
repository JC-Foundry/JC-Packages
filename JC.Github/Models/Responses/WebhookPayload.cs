using System.Text.Json.Serialization;

namespace JC.Github.Models.Responses;

/// <summary>
/// Represents the root payload received from a GitHub webhook event.
/// </summary>
public class WebhookPayload
{
    /// <summary>Gets or sets the action that triggered the event (e.g. "opened", "closed", "created").</summary>
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    /// <summary>Gets or sets the issue associated with this event. Null for non-issue events such as <c>ping</c>.</summary>
    [JsonPropertyName("issue")]
    public WebhookIssue? Issue { get; set; }

    /// <summary>Gets or sets the comment associated with this event, if applicable.</summary>
    [JsonPropertyName("comment")]
    public WebhookComment? Comment { get; set; }
}

/// <summary>
/// Represents the issue object within a GitHub webhook payload.
/// </summary>
public class WebhookIssue
{
    /// <summary>Gets or sets the GitHub issue number.</summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>Gets or sets the issue title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>Gets or sets the issue body text.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Gets or sets the issue state ("open" or "closed").</summary>
    [JsonPropertyName("state")]
    public required string State { get; set; }

    /// <summary>Gets or sets the user who triggered the event.</summary>
    [JsonPropertyName("user")]
    public required WebhookUser User { get; set; }

    /// <summary>Gets or sets the pull request metadata. Present when the issue is actually a pull request; otherwise <c>null</c>.</summary>
    [JsonPropertyName("pull_request")]
    public object? PullRequest { get; set; }
}

/// <summary>
/// Represents a comment object within a GitHub webhook payload.
/// </summary>
public class WebhookComment
{
    /// <summary>Gets or sets GitHub's unique identifier for this comment.</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>Gets or sets the comment body text.</summary>
    [JsonPropertyName("body")]
    public required string Body { get; set; }

    /// <summary>Gets or sets the comment author.</summary>
    [JsonPropertyName("user")]
    public required WebhookUser User { get; set; }

    /// <summary>Gets or sets the UTC date and time the comment was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC date and time the comment was last updated. Set to <see cref="CreatedAt"/> when not yet edited.</summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Represents a GitHub user within a webhook payload.
/// </summary>
public class WebhookUser
{
    /// <summary>Gets or sets the GitHub username.</summary>
    [JsonPropertyName("login")]
    public required string Login { get; set; }
}