using JC.Github.Models;
using Microsoft.EntityFrameworkCore;

namespace JC.Github.Data;

/// <summary>
/// Database context interface for GitHub integration entities.
/// </summary>
public interface IGithubDbContext
{
    /// <summary>Gets or sets the reported issues table.</summary>
    DbSet<ReportedIssue> ReportedIssues { get; set; }

    /// <summary>Gets or sets the issue comments table.</summary>
    DbSet<IssueComment> IssueComments { get; set; }
}