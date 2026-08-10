# JC.Github

Turns GitHub into the issue tracker for an application's own bug reports, in both directions — outbound issue creation, and an optional signed webhook that folds issue and comment activity back into local records.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Github/JC.Github.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- A `DbContext` implementing `IGithubDbContext`
- A GitHub personal access token with permission to create issues in the target repository
- For webhooks: a webhook configured in the repository, with a secret

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

builder.Services.AddGithub<AppDbContext>(builder.Configuration, options =>
{
    options.GithubRepoOwner = "your-username";
    options.GithubRepoName = "your-repo";
});
```

### Endpoints — `Program.cs`

```csharp
var app = builder.Build();

// Maps the webhook endpoint when webhooks are enabled
app.UseGithubWebhooks();
```

### Data — `AppDbContext`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyGithubMappings();
}
```

### Configuration — `appsettings.json`

```json
{
  "Github": {
    "ApiKey": "ghp_your_personal_access_token",
    "Secret": "your-webhook-secret"
  }
}
```

`ApiKey` is always required. `Secret` is required whenever webhooks are enabled, which is the default — both are validated at registration, so a missing value fails at start-up.

## Feature areas

### Reporting an issue

```csharp
public class FeedbackService(BugReportService reports)
{
    public Task<ReportedIssue> ReportAsync(string description, string? userId) =>
        reports.RecordIssue(description, IssueType.Bug, creatorId: userId);
}
```

The local `ReportedIssue` is persisted first, then the GitHub issue is created. If GitHub is unreachable the exception is logged rather than thrown, the local record is still saved, and `ReportSent` stays `false` — so an outage costs you the sync, not the report.

`IssueType` decides the GitHub issue title: `Bug` or `Suggestion`.

### Collecting reports from the UI

[JC.Web](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.Web)'s `<bug-reporter>` tag helper renders a floating feedback widget that POSTs JSON to an endpoint you provide. Pass its `description` to `RecordIssue` and the two halves meet.

### Webhooks

`UseGithubWebhooks` maps a POST endpoint at `WebhookPath`, anonymous and excluded from API descriptions. Every delivery is authenticated by its `X-Hub-Signature-256` header, compared in fixed time against an HMAC-SHA256 of the raw body — an unsigned or mismatched delivery gets 401.

Handled deliveries update the local records so issue state and discussion stay in step. Pings are acknowledged, and deliveries with no issue object — pushes, and pull-request events, since `issue_comment` fires for those too — are acknowledged without action rather than failed, so GitHub does not flag the hook as broken.

### Updating an issue body

```csharp
await reports.UpdateIssueBody(issue, revisedText);
```

### Direct API access

`GitHelper` wraps the GitHub REST API with the configured token, API version and user agent, and a 30-second timeout. Inject it for calls the package does not model.

## Defaults

| Option | Default |
|--------|---------|
| `GithubApiUrl` | `https://api.github.com` |
| `GithubApiVersion` | `2022-11-28` |
| `GitHelperUserAgent` | `JC-Application` |
| `GithubRepoOwner` / `GithubRepoName` | Empty — set them in the callback |
| `EnableWebhooks` | `true` |
| `WebhookPath` | `/api/github/webhook` |
| `GitHelper` lifetime | Singleton |
| `BugReportService`, webhook service | Scoped |
| HTTP timeout | 30 seconds |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Github/Setup.md) — registration, options, webhook configuration
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Github/Guide.md) — reporting, querying issues and comments, webhook event handling
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Github/API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
