namespace JC.Core.Models;

/// <summary>
/// The type of issue being reported.
/// </summary>
public enum IssueType
{
    /// <summary>A feature suggestion.</summary>
    Suggestion,

    /// <summary>A bug report.</summary>
    Bug
}

/// <summary>
/// Represents a reported issue (bug or suggestion) that can optionally be synced to GitHub.
/// </summary>
public class ReportedIssue
{
    /// <summary>Gets the unique identifier for this issue.</summary>
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the type of issue (suggestion or bug).</summary>
    public IssueType Type { get; set; }

    /// <summary>Gets or sets the description of the issue.</summary>
    public required string Description { get; set; }

    /// <summary>Gets or sets an optional screenshot image as a byte array.</summary>
    public byte[]? Image { get; set; }

    /// <summary>Gets or sets whether the issue has been sent to GitHub.</summary>
    public bool ReportSent { get; set; }

    /// <summary>Gets or sets the GitHub issue number, if the report was successfully created.</summary>
    public int? ExternalId { get; set; }

    /// <summary>Gets or sets whether the issue has been closed/resolved.</summary>
    public bool Closed { get; set; }

    /// <summary>Gets or sets the UTC date and time the issue was created.</summary>
    public DateTime Created { get; set; }

    /// <summary>Gets or sets the identifier of the user who reported the issue.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the display name of the user who reported the issue.</summary>
    public string? UserDisplay { get; set; }
}