# JC.Github — Guide

Covers bug and feature reporting, querying reported issues and comments, webhook event handling, and direct GitHub API access. See [Setup](Setup.md) for registration.

## Bug and feature reporting

### Submitting a report

Inject `BugReportService` to create a local issue record and push it to GitHub:

```csharp
public class FeedbackController(BugReportService bugReportService, IUserInfo userInfo) : Controller
{
    [HttpPost]
    public async Task<IActionResult> ReportBug(string description)
    {
        var issue = await bugReportService.RecordIssue(
            description: description,
            issueType: IssueType.Bug,
            creatorId: userInfo.UserId,
            creatorName: userInfo.DisplayName
        );

        if (issue.ReportSent)
            TempData["Message"] = $"Bug #{issue.ExternalId} created on GitHub.";
        else
            TempData["Message"] = "Bug saved locally — GitHub sync failed.";

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Suggest(string description)
    {
        var issue = await bugReportService.RecordIssue(
            description: description,
            issueType: IssueType.Suggestion,
            creatorId: userInfo.UserId,
            creatorName: userInfo.DisplayName
        );

        return RedirectToAction("Index");
    }
}
```

`RecordIssue` always persists the issue locally, then attempts to create a GitHub issue. The GitHub issue title is automatically set to `"New Bug"` or `"New Suggestion"` based on the `issueType`, with the `description` as the body.

**Nuance:** GitHub API failures are graceful — the error is logged but not thrown. Check `issue.ReportSent` to determine whether the GitHub sync succeeded. If it failed, `ExternalId` will be `null`.

### ReportedIssue properties

After `RecordIssue` returns, the persisted entity contains:

```csharp
var issue = await bugReportService.RecordIssue(description, IssueType.Bug);

issue.Id;          // Local GUID identifier
issue.Description; // The description you provided
issue.Type;        // IssueType.Bug or IssueType.Suggestion
issue.Created;     // DateTime.UtcNow at time of creation
issue.ReportSent;  // true if GitHub API call succeeded
issue.ExternalId;  // GitHub issue number (e.g. 42), or null if not synced
issue.Closed;      // false — newly created issues are always open
issue.UserId;      // creatorId parameter, or null if not provided
issue.UserDisplay; // creatorName parameter, or null if not provided
issue.Image;       // byte[] — not populated by RecordIssue (always null)
```

**Nuance:** `creatorId` and `creatorName` are stored locally only — they are not sent to GitHub. The GitHub issue is created under the identity of the personal access token configured in `Github:ApiKey`.

**Nuance:** The `Image` property exists on `ReportedIssue` for storing screenshots as a byte array, but `RecordIssue` does not populate it. If you need screenshot support, set it on the entity manually before or after calling `RecordIssue`.

## Querying reports and comments

### Listing reported issues

Use `IRepositoryContext<ReportedIssue>` to query your local issue records:

```csharp
public class IssueListService(IRepositoryContext<ReportedIssue> issues)
{
    public async Task<List<ReportedIssue>> GetOpenIssuesAsync()
    {
        return await issues.GetAllAsync(i => !i.Closed);
    }

    public async Task<List<ReportedIssue>> GetUnsentIssuesAsync()
    {
        // Issues that failed to sync with GitHub
        return await issues.GetAllAsync(i => !i.ReportSent);
    }

    public async Task<List<ReportedIssue>> GetByUserAsync(string userId)
    {
        return await issues.GetAllAsync(i => i.UserId == userId);
    }

    public async Task<ReportedIssue?> GetByGithubNumberAsync(int issueNumber)
    {
        return await issues.AsQueryable()
            .FirstOrDefaultAsync(i => i.ExternalId == issueNumber);
    }
}
```

**Nuance:** `ReportedIssue` does not extend `AuditModel`, so `FilterDeleted` is not available. It uses a manual `Closed` property instead of soft-delete — query with `.Where(i => !i.Closed)` or `.GetAllAsync(i => !i.Closed)`.

### Paginating issues

```csharp
public async Task<IPagination<ReportedIssue>> GetPagedIssuesAsync(int page, int pageSize = 20)
{
    return await issues.AsQueryable()
        .Where(i => !i.Closed)
        .OrderByDescending(i => i.Created)
        .ToPagedListAsync(page, pageSize);
}
```

### Querying comments for an issue

```csharp
public class CommentService(IRepositoryContext<IssueComment> comments)
{
    public async Task<List<IssueComment>> GetCommentsAsync(int issueNumber)
    {
        return await comments.GetAllAsync(
            c => c.IssueNumber == issueNumber && !c.Deleted,
            q => q.OrderBy(c => c.CreatedAt)
        );
    }

    public async Task<List<IssueComment>> GetAllCommentsIncludingDeletedAsync(int issueNumber)
    {
        return await comments.GetAllAsync(c => c.IssueNumber == issueNumber);
    }
}
```

**Nuance:** `IssueComment` uses a `Deleted` boolean for soft-delete (set when a comment is deleted on GitHub via webhook). Like `ReportedIssue`, it does not extend `AuditModel` — filter deleted comments manually with `.Where(c => !c.Deleted)`.

### IssueComment properties

```csharp
comment.Id;           // Local GUID identifier
comment.IssueNumber;  // GitHub issue number this comment belongs to
comment.CommentId;    // GitHub's unique comment ID (long)
comment.Body;         // Comment text
comment.Author;       // GitHub username of the commenter
comment.CreatedAt;    // UTC timestamp from GitHub
comment.UpdatedAt;    // UTC last-modified timestamp from GitHub (null if never edited)
comment.Deleted;      // true if the comment was deleted on GitHub
```

## Webhook event handling

### How webhooks work

When you configure a webhook in your GitHub repository settings, GitHub sends POST requests to your application whenever events occur. JC.Github handles this automatically:

1. **Signature validation** — every request is verified using HMAC-SHA256 with your `WebhookSecret`. Invalid signatures return `401 Unauthorized`.
2. **Event routing** — the `X-GitHub-Event` header determines which handler processes the payload.
3. **Database sync** — issue and comment changes are persisted to your local database.

### Supported events

| GitHub event | Actions handled | Behaviour |
|-------------|----------------|-----------|
| `ping` | — | Returns `200 OK` (GitHub connection test) |
| `issues` | `opened`, `closed`, `reopened`, `edited` | Creates or updates `ReportedIssue` records |
| `issue_comment` | `created`, `edited`, `deleted` | Creates, updates, or soft-deletes `IssueComment` records |

Events with an `Issue` field that aren't `issues` or `issue_comment` are logged at debug level and ignored. Events without an `Issue` field (e.g. `push`, `star`) return `400 BadRequest` — configure your GitHub webhook to send only the events listed above.

### Issue sync behaviour

When a webhook arrives for an `issues` event:

- **New issue** (no matching `ExternalId` in the database): a new `ReportedIssue` is created with `ReportSent = true`, `ExternalId` set to the GitHub issue number, and `Closed` reflecting the current state.
- **Existing issue** (matching `ExternalId` found): the `Description` and `Closed` status are updated to match GitHub.

```csharp
// After someone closes issue #42 on GitHub, the webhook updates the local record:
var issue = await issues.AsQueryable()
    .FirstOrDefaultAsync(i => i.ExternalId == 42);

issue.Closed; // true — updated by the webhook
```

**Nuance:** Issues created via webhook always have `Type = IssueType.Bug` regardless of the actual issue content. The webhook payload does not carry enough information to distinguish bugs from suggestions. If you need accurate type classification, consider using GitHub labels and post-processing.

**Nuance:** For issue descriptions, the webhook handler uses `Body ?? Title` — if the GitHub issue has no body, the title is used as the description instead.

### Comment sync behaviour

When a webhook arrives for an `issue_comment` event:

- **`created`** — a new `IssueComment` record is inserted with the comment body, author, and timestamps.
- **`edited`** — the existing comment's `Body` and `UpdatedAt` are updated. Stale edits are ignored — if the incoming `UpdatedAt` is equal to or earlier than the stored value, the update is skipped.
- **`deleted`** — the existing comment's `Deleted` flag is set to `true`. The record is retained for audit purposes.

```csharp
// After a comment is edited on GitHub, the local record reflects the change:
var comment = await comments.AsQueryable()
    .FirstOrDefaultAsync(c => c.CommentId == 123456789);

comment.Body;      // Updated to the new comment text
comment.UpdatedAt; // Updated to the edit timestamp
```

**Nuance:** Pull request comments are filtered out. GitHub fires `issue_comment` events for both issues and pull requests — the webhook endpoint checks for the presence of a `PullRequest` field on the payload and ignores PR events.

### Duplicate handling

Both issue and comment webhook handlers catch `DbUpdateException` silently. This handles the case where GitHub delivers the same webhook twice (at-least-once delivery). The unique indexes on `ExternalId` and `CommentId` prevent duplicate records, and the duplicate attempt is logged at debug level without throwing.

## GitHelper

### Direct GitHub API access

`GitHelper` is registered as a singleton and can be injected for direct GitHub API calls:

```csharp
public class CustomGithubService(GitHelper gitHelper)
{
    public async Task<int> CreateIssueAsync(string owner, string repo, string title, string body)
    {
        var issueNumber = await gitHelper.RecordIssue(owner, repo, title, body);
        return issueNumber;
    }
}
```

`RecordIssue` creates a GitHub issue via `POST /repos/{owner}/{repo}/issues` and returns the new issue number.

**Nuance:** Unlike `BugReportService`, `GitHelper.RecordIssue` does not catch exceptions. If the GitHub API call fails, the exception propagates to the caller. Use `BugReportService` when you want graceful failure handling with local persistence, and `GitHelper` when you want direct API access with full control over error handling.

**Nuance:** `GitHelper` sends requests with `Authorization: Bearer {apiKey}`, the `X-GitHub-Api-Version` from `GithubOptions.GithubApiVersion`, and the `User-Agent` from `GithubOptions.GitHelperUserAgent`. GitHub requires a User-Agent header on all requests.
