using JC.Core.Data;
using JC.Core.Helpers;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using JC.Github.Data;
using JC.Github.Helpers;
using JC.Github.Models;
using JC.Github.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Github.Services;

/// <summary>
/// Service for recording bug reports and feature suggestions, with optional GitHub issue creation.
/// </summary>
public class BugReportService
{
    private readonly IRepositoryContext<ReportedIssue> _reportedIssues;
    private readonly GitHelper _gitHelper;
    private readonly ILogger<BugReportService> _logger;
    private readonly string _owner;
    private readonly string _repo;

    public BugReportService(IOptions<GithubOptions> options,
        IRepositoryContext<ReportedIssue> reportedIssues,
        GitHelper gitHelper,
        ILogger<BugReportService> logger)
    {
        _reportedIssues = reportedIssues;
        _gitHelper = gitHelper;
        _logger = logger;

        var opts = options.Value;
        _owner = string.IsNullOrEmpty(opts.GithubRepoOwner)
            ? throw new InvalidOperationException("GithubOptions.GithubRepoOwner is not configured.")
            : opts.GithubRepoOwner;
        _repo = string.IsNullOrEmpty(opts.GithubRepoName)
            ? throw new InvalidOperationException("GithubOptions.GithubRepoName is not configured.")
            : opts.GithubRepoName;
    }
    
    /// <summary>
    /// Records a new issue, attempts to create a corresponding GitHub issue, and persists it to the database.
    /// GitHub failures are logged but do not prevent the local record from being saved.
    /// </summary>
    /// <param name="description">The issue description.</param>
    /// <param name="issueType">The type of issue (bug or suggestion).</param>
    /// <param name="creatorId">Optional identifier of the reporting user.</param>
    /// <param name="creatorName">Optional display name of the reporting user.</param>
    /// <returns>The persisted <see cref="ReportedIssue"/> entity.</returns>
    public async Task<ReportedIssue> RecordIssue(string description, IssueType issueType, string? creatorId = null, string? creatorName = null)
    {
        var ri = new ReportedIssue
        {
            Description = description,
            Type = issueType,
            Created = DateTime.UtcNow,
            ReportSent = false,
            UserId = creatorId,
            UserDisplay = creatorName
        };

        try
        {
            var issueNumber = await _gitHelper.RecordIssue(_owner, _repo, "New " + issueType, description);
            ri.ReportSent = true;
            ri.ExternalId = issueNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording issue in Github.");
        }
        
        await _reportedIssues.AddAsync(ri);
        return ri;
    }
}