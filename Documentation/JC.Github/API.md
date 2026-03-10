# JC.Github — API reference

Complete reference of all public types, properties, and methods in JC.Github. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Setup.md), not here.

---

# Models

## ReportedIssue

**Namespace:** `JC.Github.Models`

Entity representing a locally persisted issue report, optionally synced with GitHub.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Local unique identifier. |
| `Type` | `IssueType` | — | get; set; | Whether this is a bug or suggestion. |
| `Description` | `string` | — | get; set; | The issue body text. Marked `required`. |
| `Image` | `byte[]?` | `null` | get; set; | Optional screenshot data. Not populated by `RecordIssue`. |
| `ReportSent` | `bool` | `false` | get; set; | Whether the GitHub API call succeeded. |
| `ExternalId` | `int?` | `null` | get; set; | GitHub issue number, or `null` if not synced. |
| `Closed` | `bool` | `false` | get; set; | Whether the issue has been closed. |
| `Created` | `DateTime` | — | get; set; | UTC timestamp of local creation. |
| `UserId` | `string?` | `null` | get; set; | Local-only creator identifier. |
| `UserDisplay` | `string?` | `null` | get; set; | Local-only creator display name. |

---

## IssueComment

**Namespace:** `JC.Github.Models`

Entity representing a GitHub issue comment, synced via webhooks.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Local unique identifier. |
| `IssueNumber` | `int` | — | get; set; | GitHub issue number this comment belongs to. |
| `CommentId` | `long` | — | get; set; | GitHub's unique comment identifier. |
| `Body` | `string` | — | get; set; | Comment text. Marked `required`. |
| `Author` | `string` | — | get; set; | GitHub username of the commenter. Marked `required`. |
| `CreatedAt` | `DateTime` | — | get; set; | UTC timestamp from GitHub. |
| `UpdatedAt` | `DateTime?` | `null` | get; set; | UTC timestamp of last edit on GitHub. |
| `Deleted` | `bool` | `false` | get; set; | Whether the comment has been soft-deleted (set when deleted on GitHub). |

---

## NewIssueResponse

**Namespace:** `JC.Github.Models.Responses`

Deserialisation model for the GitHub API response when creating an issue.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Id` | `long` | get; set; | GitHub's internal unique identifier for the issue. |
| `Number` | `int` | get; set; | The issue number within the repository. |
| `Title` | `string?` | get; set; | The issue title. |
| `State` | `string?` | get; set; | The issue state (e.g. `"open"`, `"closed"`). |
| `Html_Url` | `string?` | get; set; | The browser URL for the issue. |

---

## WebhookPayload

**Namespace:** `JC.Github.Models.Responses`

Deserialisation model for incoming GitHub webhook request bodies.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Action` | `string` | get; set; | The action that triggered the webhook (e.g. `"opened"`, `"closed"`, `"created"`, `"edited"`, `"deleted"`). Marked `required`. |
| `Issue` | `WebhookIssue?` | get; set; | The issue associated with the event, if applicable. |
| `Comment` | `WebhookComment?` | get; set; | The comment associated with the event, if applicable. |

---

## WebhookIssue

**Namespace:** `JC.Github.Models.Responses`

Deserialisation model for the issue object within a GitHub webhook payload.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Number` | `int` | get; set; | The issue number within the repository. |
| `Title` | `string` | get; set; | The issue title. Marked `required`. |
| `Body` | `string?` | get; set; | The issue body text. May be `null` if the issue has no body. |
| `State` | `string` | get; set; | The issue state (`"open"` or `"closed"`). Marked `required`. |
| `User` | `WebhookUser` | get; set; | The GitHub user who created the issue. Marked `required`. |
| `PullRequest` | `object?` | get; set; | Present if the issue is actually a pull request. Used to filter out PR-related events. |

---

## WebhookComment

**Namespace:** `JC.Github.Models.Responses`

Deserialisation model for the comment object within a GitHub webhook payload.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Id` | `long` | get; set; | GitHub's unique comment identifier. |
| `Body` | `string` | get; set; | The comment text. Marked `required`. |
| `User` | `WebhookUser` | get; set; | The GitHub user who wrote the comment. Marked `required`. |
| `CreatedAt` | `DateTime` | get; set; | UTC timestamp of comment creation. |
| `UpdatedAt` | `DateTime` | get; set; | UTC timestamp of last edit. |

---

## WebhookUser

**Namespace:** `JC.Github.Models.Responses`

Deserialisation model for the user object within GitHub webhook payloads.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Login` | `string` | get; set; | The GitHub username. Marked `required`. |

---

# Enums

## IssueType

**Namespace:** `JC.Github.Models`

Enum indicating the type of reported issue.

| Member | Value | Description |
|--------|-------|-------------|
| `Suggestion` | `0` | A feature suggestion. GitHub issue title: "New Suggestion". |
| `Bug` | `1` | A bug report. GitHub issue title: "New Bug". |

---

# Services

## BugReportService

**Namespace:** `JC.Github.Services`

Manages local persistence and GitHub synchronisation of issue reports. Reads `GithubRepoOwner` and `GithubRepoName` from `GithubOptions` at construction time — throws `InvalidOperationException` if either is empty. Inject via `BugReportService`.

### Methods

#### RecordIssue(string description, IssueType issueType, string? creatorId = null, string? creatorName = null)

**Returns:** `Task<ReportedIssue>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `description` | `string` | — | The issue body text. Used as the GitHub issue body. |
| `issueType` | `IssueType` | — | Whether this is a `Bug` or `Suggestion`. Determines the GitHub issue title (`"New Bug"` or `"New Suggestion"`). |
| `creatorId` | `string?` | `null` | Local-only user identifier. Stored on the `ReportedIssue` but not sent to GitHub. |
| `creatorName` | `string?` | `null` | Local-only display name. Stored on the `ReportedIssue` but not sent to GitHub. |

Creates a new `ReportedIssue` entity with `Created` set to `DateTime.UtcNow` and `ReportSent` initially `false`. Attempts to create a corresponding GitHub issue via `GitHelper.RecordIssue` using the configured owner and repo. The GitHub issue title is set to `"New "` followed by the `issueType` name.

If the GitHub API call succeeds, `ReportSent` is set to `true` and `ExternalId` is populated with the returned issue number. If it fails, the exception is logged but not thrown — `ReportSent` remains `false` and `ExternalId` remains `null`.

The entity is always persisted to the database via `IRepositoryContext<ReportedIssue>.AddAsync` regardless of GitHub sync outcome.

---

## GithubWebhookService

**Namespace:** `JC.Github.Services`

Processes incoming GitHub webhook events and synchronises the local database. Inject via `GithubWebhookService`.

### Methods

#### ProcessEventAsync(string eventType, WebhookPayload payload)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `eventType` | `string` | — | The GitHub event type from the `X-GitHub-Event` header (e.g. `"issues"`, `"issue_comment"`). |
| `payload` | `WebhookPayload` | — | The deserialised webhook request body. |

Routes the event to the appropriate handler based on `eventType`. Returns immediately without processing if `payload.Issue` is `null`.

For `"issues"` events: looks up an existing `ReportedIssue` by `ExternalId` matching the issue number. If none exists, creates a new record with `Type = IssueType.Bug`, `ReportSent = true`, `Description` set to the issue body (falling back to the title if body is null), and `Closed` reflecting the current state. If a matching record exists, updates its `Description` and `Closed` status. Catches `DbUpdateException` silently on create to handle duplicate webhook deliveries.

For `"issue_comment"` events (requires `payload.Comment` to be non-null): looks up an existing `IssueComment` by `CommentId`. On `"created"` action, inserts a new comment record (catching `DbUpdateException` for duplicates). On `"edited"`, updates the `Body` and `UpdatedAt` fields. On `"deleted"`, sets `Deleted = true` (soft-delete).

All other event types are logged at debug level and ignored.

---

# Helpers

## GitHelper

**Namespace:** `JC.Github.Helpers`

Low-level HTTP client for the GitHub API. Configured as a singleton with authentication headers set at construction time. Inject via `GitHelper`.

### Constructor

#### GitHelper(GithubOptions options, string apiKey)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `GithubOptions` | — | Options providing the API URL (`GithubApiUrl`), API version (`GithubApiVersion`), and user agent (`GitHelperUserAgent`). |
| `apiKey` | `string` | — | The personal access token used for `Authorization: Bearer` authentication. |

Creates a `FlurlClient` configured with `Authorization`, `X-GitHub-Api-Version` (from `options.GithubApiVersion`), and `User-Agent` (from `options.GitHelperUserAgent`) headers applied to all requests.

### Methods

#### RecordIssue(string owner, string repo, string title, string desc)

**Returns:** `Task<int>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `owner` | `string` | — | The GitHub username or organisation that owns the repository. |
| `repo` | `string` | — | The repository name. |
| `title` | `string` | — | The title for the new issue. |
| `desc` | `string` | — | The body content for the new issue. |

Sends a `POST` request to `/repos/{owner}/{repo}/issues` with the title and body as JSON. Deserialises the response as `NewIssueResponse` and returns the `Number` property (the GitHub issue number). Does not catch exceptions — failures propagate directly to the caller.

---

# Data

## IGithubDbContext

**Namespace:** `JC.Github.Data`

Contract for the GitHub data context, exposing entity sets for issue tracking.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `ReportedIssues` | `DbSet<ReportedIssue>` | get; set; | The set of locally persisted issue reports. |
| `IssueComments` | `DbSet<IssueComment>` | get; set; | The set of locally persisted issue comments. |
