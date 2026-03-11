# JC.Github — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- A GitHub repository and a [personal access token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens) with issue write permissions
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Github`:

```xml
<ProjectReference Include="path/to/JC.Github/JC.Github.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### DbContext

Your `DbContext` must implement `IGithubDbContext` and call `ApplyGithubMappings()` in `OnModelCreating`:

```csharp
public class AppDbContext : DataDbContext, IGithubDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ReportedIssue> ReportedIssues { get; set; }
    public DbSet<IssueComment> IssueComments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyGithubMappings();
    }
}
```

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddGithub<AppDbContext>(builder.Configuration);
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// Register the webhook endpoint (only active if EnableWebhooks is true in options)
app.UseGithubWebhooks();
```

### Configuration — `appsettings.json`

Only secrets are stored in configuration:

```json
{
  "Github": {
    "ApiKey": "ghp_your_personal_access_token",
    "Secret": "your-webhook-secret"
  }
}
```

`ApiKey` is always required. `Secret` is required when webhooks are enabled (the default). All other settings are configured via `GithubOptions`.

### Defaults

When called with no configure callback, `AddGithub` registers:

| Registration | Lifetime | Description |
|-------------|----------|-------------|
| `IOptions<GithubOptions>` | Singleton | GitHub configuration options |
| `GitHelper` | Singleton | HTTP client for the GitHub API — sends requests with Bearer auth, `X-GitHub-Api-Version: 2022-11-28`, and the configured User-Agent |
| `BugReportService` | Scoped | Creates issues locally and on GitHub |
| `GithubWebhookService` | Scoped | Processes incoming webhook events |
| `IGithubDbContext` → `TContext` | Scoped | Your DbContext as the GitHub data context |
| `IRepositoryContext<ReportedIssue>` | Scoped | Repository for reported issues |
| `IRepositoryContext<IssueComment>` | Scoped | Repository for issue comments |

Default option values:

| Option | Default | Description |
|--------|---------|-------------|
| `GithubApiUrl` | `"https://api.github.com"` | Base URL for the GitHub REST API |
| `GithubApiVersion` | `"2022-11-28"` | API version header sent with requests |
| `GitHelperUserAgent` | `"JC-Application"` | User-Agent header sent with GitHub API requests |
| `GithubRepoOwner` | `""` | Repository owner (user or organisation) for `BugReportService` |
| `GithubRepoName` | `""` | Repository name for `BugReportService` |
| `EnableWebhooks` | `true` | Registers the webhook POST endpoint |
| `WebhookPath` | `"/api/github/webhook"` | URL path for the webhook endpoint |
| `WebhookSecret` | Set from `Github:Secret` | HMAC-SHA256 secret for webhook signature validation |

`UseGithubWebhooks` maps a POST endpoint at the configured `WebhookPath`. Incoming requests are validated using HMAC-SHA256 signature verification (fixed-time comparison) against the `WebhookSecret`. The endpoint handles `issues` and `issue_comment` events from GitHub, and responds to `ping` events for connection testing.

When a bug report is submitted via `BugReportService.RecordIssue()`, it is always persisted locally to the `ReportedIssues` table. If the GitHub API call succeeds, the issue's `ExternalId` is set to the GitHub issue number and `ReportSent` is marked `true`. If the API call fails, the local record is still saved — the failure is logged but not thrown.

## 2. Full configuration

### AddGithub — service registration

```csharp
builder.Services.AddGithub<AppDbContext>(builder.Configuration, options =>
{
    options.GithubApiUrl = "https://api.github.com";
    options.GithubApiVersion = "2022-11-28";
    options.GitHelperUserAgent = "JC-Application";
    options.GithubRepoOwner = "your-username";
    options.GithubRepoName = "your-repo";
    options.EnableWebhooks = true;
    options.WebhookPath = "/api/github/webhook";
});
```

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TContext` | `DbContext, IDataDbContext, IGithubDbContext` | Your DbContext. Must extend `DataDbContext` (which implements `IDataDbContext`) and implement `IGithubDbContext` (requires `DbSet<ReportedIssue>` and `DbSet<IssueComment>` properties) |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configuration` | `IConfiguration` | *required* | Application configuration — reads `Github:ApiKey` and `Github:Secret` |
| `configure` | `Action<GithubOptions>?` | `null` | Optional callback to configure `GithubOptions`. Runs before internal post-configuration, so values such as `EnableWebhooks` are finalised before the webhook secret is validated |

#### GithubOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `GithubApiUrl` | `string` | `"https://api.github.com"` | Base URL for the GitHub REST API |
| `GithubApiVersion` | `string` | `"2022-11-28"` | The `X-GitHub-Api-Version` header value sent with all API requests |
| `GitHelperUserAgent` | `string` | `"JC-Application"` | The `User-Agent` header value sent with all GitHub API requests. GitHub requires a User-Agent header on all API requests |
| `GithubRepoOwner` | `string` | `""` | The GitHub repository owner (user or organisation) used by `BugReportService`. Throws `InvalidOperationException` at resolution time if empty |
| `GithubRepoName` | `string` | `""` | The GitHub repository name used by `BugReportService`. Throws `InvalidOperationException` at resolution time if empty |
| `EnableWebhooks` | `bool` | `true` | When `true`, the webhook endpoint is registered and `Github:Secret` is required in configuration. Set to `false` if you only need outbound issue creation |
| `WebhookPath` | `string` | `"/api/github/webhook"` | The URL path where the webhook POST endpoint is mapped. Must match the webhook URL configured in your GitHub repository settings |
| `WebhookSecret` | `string` | From `Github:Secret` | The HMAC-SHA256 secret used to validate incoming webhook signatures. Set automatically from configuration — cannot be overridden via the configure callback (internal setter). Throws `InvalidOperationException` if `EnableWebhooks` is `true` and `Github:Secret` is missing |

#### Configuration keys

| Key | Required | Description |
|-----|----------|-------------|
| `Github:ApiKey` | Yes | Personal access token for GitHub API authentication |
| `Github:Secret` | When `EnableWebhooks = true` | HMAC-SHA256 secret — must match the secret configured in your GitHub repository's webhook settings |

### Disabling webhooks

If you only need to create issues from your application and don't need to receive events from GitHub:

```csharp
builder.Services.AddGithub<AppDbContext>(builder.Configuration, options =>
{
    options.EnableWebhooks = false;
});
```

With webhooks disabled:
- `Github:Secret` is not required in configuration
- `UseGithubWebhooks()` returns without registering an endpoint
- `GithubWebhookService` is still registered but won't receive traffic

### UseGithubWebhooks — endpoint registration

```csharp
app.UseGithubWebhooks();
```

Maps a POST endpoint at the configured `WebhookPath`. The endpoint:

1. Validates the `X-Hub-Signature-256` header against `WebhookSecret` using HMAC-SHA256 with fixed-time comparison — returns `401 Unauthorized` if invalid
2. Reads the `X-GitHub-Event` header — returns `400 Bad Request` if missing
3. Responds `200 OK` to `ping` events (GitHub connection test)
4. Deserialises the JSON payload and filters out pull request events
5. Routes `issues` events to update or create `ReportedIssue` records
6. Routes `issue_comment` events to create, update, or soft-delete `IssueComment` records

The endpoint is excluded from OpenAPI documentation.

### ApplyGithubMappings — entity configuration

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyGithubMappings();
}
```

Applies EF Core entity configuration for both models:

**ReportedIssue:**
- Primary key: `Id` (GUID string)
- Required: `Description`, `Type`, `Created`
- Unique index: `ExternalId`
- Indexes: `UserId`, `Closed`

**IssueComment:**
- Primary key: `Id` (GUID string)
- Required: `Body`, `Author`, `CreatedAt`
- Index: `IssueNumber`
- Unique index: `CommentId`

### BugReportService — creating issues

`BugReportService` is resolved from DI. It creates a local record and attempts to push the issue to GitHub:

```csharp
public class MyController : Controller
{
    private readonly BugReportService _bugReportService;
    private readonly IUserInfo _userInfo;

    public MyController(BugReportService bugReportService, IUserInfo userInfo)
    {
        _bugReportService = bugReportService;
        _userInfo = userInfo;
    }

    public async Task<IActionResult> ReportBug(string description)
    {
        var issue = await _bugReportService.RecordIssue(
            description: description,
            issueType: IssueType.Bug,
            creatorId: _userInfo.UserId,          // or User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            creatorName: _userInfo.DisplayName     // or User.Identity?.Name
        );

        // issue.ReportSent == true if GitHub API call succeeded
        // issue.ExternalId contains the GitHub issue number if sent
        return Ok();
    }
}
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `description` | `string` | *required* | The issue description / body text |
| `issueType` | `IssueType` | *required* | `IssueType.Bug` or `IssueType.Suggestion` |
| `creatorId` | `string?` | `null` | Optional user ID of the reporter — stored locally, not sent to GitHub |
| `creatorName` | `string?` | `null` | Optional display name of the reporter — stored locally, not sent to GitHub |

The GitHub issue title is automatically set to `"New Bug"` or `"New Suggestion"` based on the `issueType`.

If the GitHub API call fails, the error is logged but not thrown — the local record is always persisted regardless of GitHub availability.

## 3. Apply migrations

JC.Github introduces the `ReportedIssues` and `IssueComments` tables. After configuring your DbContext, generate and apply the migration:

```bash
dotnet ef migrations add AddGithubTables --project YourApp
dotnet ef database update --project YourApp
```

Alternatively, generate the migration and apply it programmatically at startup:

```bash
dotnet ef migrations add AddGithubTables --project YourApp
```

```csharp
await app.Services.MigrateDatabaseAsync<AppDbContext>();
```

## 4. Verify

1. Run the application.
2. Call `BugReportService.RecordIssue()` with a test description — check that a record appears in the `ReportedIssues` table and an issue is created in your GitHub repository.
3. If webhooks are enabled, configure a webhook in your GitHub repository settings pointing to `{your-app-url}/api/github/webhook` with the same secret. Close the issue on GitHub and verify the local `ReportedIssue` record updates its `Closed` field.

## Next steps

- [Guide](Guide.md) — webhook event handling, issue lifecycle, and comment tracking.
- [API Reference](API.md)
