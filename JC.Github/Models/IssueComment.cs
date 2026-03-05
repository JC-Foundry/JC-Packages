namespace JC.Github.Models;

/// <summary>
/// Represents a comment on a GitHub issue, synced via webhook.
/// </summary>
public class IssueComment
{
    /// <summary>Gets the unique identifier for this comment record.</summary>
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the GitHub issue number this comment belongs to.</summary>
    public int IssueNumber { get; set; }

    /// <summary>Gets or sets GitHub's own comment identifier, used to track edits and deletions.</summary>
    public long CommentId { get; set; }

    /// <summary>Gets or sets the comment body text.</summary>
    public required string Body { get; set; }

    /// <summary>Gets or sets the GitHub username of the comment author.</summary>
    public required string Author { get; set; }

    /// <summary>Gets or sets the UTC date and time the comment was created on GitHub.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC date and time the comment was last updated on GitHub.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets whether the comment has been deleted on GitHub.</summary>
    public bool Deleted { get; set; }
}