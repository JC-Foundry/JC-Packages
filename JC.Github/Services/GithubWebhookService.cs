using JC.Core.Services.DataRepositories;
using JC.Github.Models;
using JC.Github.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JC.Github.Services;

/// <summary>
/// Processes GitHub webhook events and updates the database accordingly using the repository pattern.
/// </summary>
public class GithubWebhookService(
    IRepositoryContext<ReportedIssue> reportedIssues,
    IRepositoryContext<IssueComment> issueComments,
    ILogger<GithubWebhookService> logger)
{
    /// <summary>
    /// Routes a webhook event to the appropriate handler based on the event type.
    /// </summary>
    /// <param name="eventType">The GitHub event type from the <c>X-GitHub-Event</c> header.</param>
    /// <param name="payload">The deserialised webhook payload.</param>
    public async Task ProcessEventAsync(string eventType, WebhookPayload payload)
    {
        if (payload.Issue is null) return;

        switch (eventType)
        {
            case "issues":
                await HandleIssueEventAsync(payload.Issue);
                break;
            case "issue_comment" when payload.Comment is not null:
                await HandleCommentEventAsync(payload.Action, payload.Issue, payload.Comment);
                break;
            default:
                logger.LogDebug("Ignoring unhandled GitHub event type: {EventType}", eventType);
                break;
        }
    }

    private async Task HandleIssueEventAsync(WebhookIssue webhookIssue)
    {
        var issue = await reportedIssues
            .GetAll(r => r.ExternalId == webhookIssue.Number)
            .FirstOrDefaultAsync();

        if (issue is null)
        {
            try
            {
                await reportedIssues.AddAsync(new ReportedIssue
                {
                    Description = webhookIssue.Body ?? webhookIssue.Title,
                    Type = IssueType.Bug, // Default — no reliable way to determine type from GitHub payload without labels
                    Created = DateTime.UtcNow,
                    ReportSent = true,
                    ExternalId = webhookIssue.Number,
                    Closed = webhookIssue.State == "closed"
                });

                logger.LogInformation("Created ReportedIssue from GitHub issue #{IssueNumber}", webhookIssue.Number);
            }
            catch (DbUpdateException ex)
            {
                logger.LogDebug(ex, "ReportedIssue for GitHub issue #{IssueNumber} already exists, skipping", webhookIssue.Number);
            }
        }
        else
        {
            issue.Description = webhookIssue.Body ?? webhookIssue.Title;
            issue.Closed = webhookIssue.State == "closed";
            await reportedIssues.UpdateAsync(issue);

            logger.LogInformation("Updated ReportedIssue for GitHub issue #{IssueNumber}", webhookIssue.Number);
        }
    }

    private async Task HandleCommentEventAsync(string action, WebhookIssue webhookIssue, WebhookComment comment)
    {
        var existing = await issueComments
            .GetAll(c => c.CommentId == comment.Id)
            .FirstOrDefaultAsync();

        switch (action)
        {
            case "created" when existing is null:
                try
                {
                    await issueComments.AddAsync(new IssueComment
                    {
                        IssueNumber = webhookIssue.Number,
                        CommentId = comment.Id,
                        Body = comment.Body,
                        Author = comment.User.Login,
                        CreatedAt = comment.CreatedAt,
                        UpdatedAt = comment.UpdatedAt
                    });

                    logger.LogInformation("Created comment {CommentId} for issue #{IssueNumber}",
                        comment.Id, webhookIssue.Number);
                }
                catch (DbUpdateException ex)
                {
                    logger.LogDebug(ex, "Comment {CommentId} for issue #{IssueNumber} already exists, skipping",
                        comment.Id, webhookIssue.Number);
                }
                break;

            case "edited" when existing is not null:
                existing.Body = comment.Body;
                existing.UpdatedAt = comment.UpdatedAt;
                await issueComments.UpdateAsync(existing);

                logger.LogInformation("Updated comment {CommentId}", comment.Id);
                break;

            case "deleted" when existing is not null:
                existing.Deleted = true;
                await issueComments.UpdateAsync(existing);

                logger.LogInformation("Soft-deleted comment {CommentId}", comment.Id);
                break;

            default:
                logger.LogDebug("Ignoring comment action '{Action}' for comment {CommentId}",
                    action, comment.Id);
                break;
        }
    }
}