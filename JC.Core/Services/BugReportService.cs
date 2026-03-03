using JC.Core.Data;
using JC.Core.Helpers;
using JC.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JC.Core.Services;

public class BugReportService
{
    private readonly IDataDbContext _context;
    private readonly GitHelper _gitHelper;
    private readonly ILogger<BugReportService> _logger;
    private readonly string _owner;
    private readonly string _repo;

    public BugReportService(IConfiguration config,
        IDataDbContext context,
        GitHelper gitHelper,
        ILogger<BugReportService> logger)
    {
        _context = context;
        _gitHelper = gitHelper;
        _logger = logger;
        _owner = config["Github:Owner"] ?? throw new ArgumentNullException(nameof(config));
        _repo = config["Github:Repo"] ?? throw new ArgumentNullException(nameof(config));
    }
    
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

        await _context.ReportedIssues.AddAsync(ri);
        await _context.SaveChangesAsync();
        return ri;
    }
}