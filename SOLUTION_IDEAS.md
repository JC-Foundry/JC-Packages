# JC-Packages — Feature Ideas

> **Created:** 2026-03-05
> A collection of feature ideas and enhancements for each project in the JC-Packages solution.

---

## JC.Core

### 1. Pagination Support

The repository layer provides querying but has no built-in pagination. A `PaginatedResult<T>` model and extension method would save every consuming app from re-implementing this.

```csharp
public class PaginatedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

Could live as an extension on `IQueryable<T>`:

```csharp
public static async Task<PaginatedResult<T>> ToPaginatedAsync<T>(
    this IQueryable<T> query, int page, int pageSize, CancellationToken ct = default);
```

### 2. String Extensions

Common string operations that get rewritten in every project:

| Method | Description |
|--------|-------------|
| `Truncate(maxLength, suffix)` | Truncates with ellipsis: `"Hello World".Truncate(8)` → `"Hello..."` |
| `ToSlug()` | URL-friendly slug: `"My Blog Post!"` → `"my-blog-post"` |
| `NullIfEmpty()` | Returns `null` if string is empty/whitespace |
| `ToTitleCase()` | Proper title casing respecting common prepositions |
| `StripHtml()` | Removes HTML tags from a string |
| `Mask(visibleChars)` | Masks sensitive data: `"john@email.com".Mask(3)` → `"joh***"` |

### 3. Date/Time Extensions

Utility methods for common date operations:

| Method | Description |
|--------|-------------|
| `ToRelativeTime()` | `"2 hours ago"`, `"yesterday"`, `"3 days ago"` |
| `ToFriendlyDate()` | `"Monday 5 March 2026"` with culture support |
| `StartOfDay()` / `EndOfDay()` | Useful for date range queries |
| `StartOfWeek()` / `EndOfWeek()` | Week boundary calculations |
| `IsWeekend()` / `IsWeekday()` | Quick checks |
| `Age()` | Calculate age from a date of birth |

### 4. Specification Pattern

Complement the repository with composable query specifications. Allows consumers to build reusable, testable query logic without leaking `IQueryable` expressions everywhere:

```csharp
public abstract class Specification<T> where T : class
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public Specification<T> And(Specification<T> other) => ...;
    public Specification<T> Or(Specification<T> other) => ...;
    public Specification<T> Not() => ...;
}
```

Usage: `repo.GetAll(new ActiveUsersInTenantSpec(tenantId))`

### 5. Result Pattern

A lightweight `Result<T>` type to replace the common pattern of returning `null` for failure or throwing exceptions for expected failures:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public static Result<T> Success(T value) => ...;
    public static Result<T> Failure(string error) => ...;
}
```

Useful for service methods like `BugReportService.RecordIssue` where GitHub failures are expected but the local save succeeds — the caller could inspect what happened.

### 6. File/Storage Abstraction

A simple `IFileStorageService` interface for file upload/download operations. Consuming apps could implement it for local disk, Azure Blob, or S3:

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string? folder = null, CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string path, CancellationToken ct = default);
    Task<bool> DeleteAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
}
```

### 7. Email Service Abstraction

Similar to `IFileStorageService` — a thin contract for sending emails that consuming apps can implement with SendGrid, SMTP, etc.:

```csharp
public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public class EmailMessage
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsHtml { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
}
```

### 8. Audit Query Service

`AuditService` writes audit entries but there's no service for *reading* them. An `AuditQueryService` would provide:

| Method | Description |
|--------|-------------|
| `GetByEntityAsync(tableName, entityId)` | Full audit history for a specific entity |
| `GetByUserAsync(userId, dateRange?)` | All actions by a specific user |
| `GetRecentAsync(count)` | Most recent audit entries |
| `GetByActionAsync(action, dateRange?)` | All entries of a specific action type |

---

## JC.Identity

### 1. AdminService

A utility service wrapping common `UserManager` operations that administrators need. Saves every consuming app from writing the same boilerplate:

| Method | Description |
|--------|-------------|
| `UpdateDisplayNameAsync(userId, name)` | Updates a user's display name |
| `ResetPasswordAsync(userId, newPassword)` | Admin-initiated password reset (generates token + resets) |
| `ForcePasswordChangeAsync(userId)` | Sets `RequirePasswordChange = true` |
| `EnableUserAsync(userId)` / `DisableUserAsync(userId)` | Toggle `IsEnabled` |
| `ToggleTwoFactorAsync(userId)` | Enable/disable 2FA |
| `UnlockUserAsync(userId)` | Reset lockout and access failed count |
| `GetUserSummaryAsync(userId)` | Returns a DTO with user info, roles, tenant, last login |
| `AssignToTenantAsync(userId, tenantId)` | Move a user to a different tenant |
| `UpdateRolesAsync(userId, roles)` | Replaces a user's role set |

### 2. TenantService

CRUD operations for tenant management, complementing the raw repository:

| Method | Description |
|--------|-------------|
| `CreateTenantAsync(name, description, domain?)` | Creates a new tenant with audit trail |
| `UpdateTenantAsync(id, ...)` | Updates tenant properties |
| `DeleteTenantAsync(id)` | Soft-deletes tenant (with validation that no active users remain) |
| `GetTenantUsersAsync(tenantId)` | Lists all users in a tenant |
| `GetTenantUserCountAsync(tenantId)` | Count for quota checking |
| `IsAtCapacityAsync(tenantId)` | Checks `MaxUsers` limit |
| `HasExpiredAsync(tenantId)` | Checks `ExpiryDateUtc` |
| `GetSettingAsync(tenantId, key)` | Shorthand for reading a single setting |

### 3. Login History / Activity Tracking

Track user login events for security and compliance:

```csharp
public class LoginEvent
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public LoginResult Result { get; set; } // Success, FailedPassword, LockedOut, Disabled
}
```

Could hook into ASP.NET Core Identity events or be exposed as a middleware/service.

### 4. User Invitation System

For multi-tenant applications where admins invite users rather than open registration:

| Method | Description |
|--------|-------------|
| `CreateInvitationAsync(email, tenantId, roles)` | Generates a time-limited invitation token |
| `ValidateInvitationAsync(token)` | Checks token validity and expiry |
| `AcceptInvitationAsync(token, user)` | Creates the user with pre-assigned tenant and roles |
| `RevokeInvitationAsync(token)` | Cancels a pending invitation |
| `GetPendingInvitationsAsync(tenantId)` | Lists outstanding invitations for a tenant |

### 5. SignInManager Extensions

Wrap common sign-in workflows that consuming apps repeat:

| Method | Description |
|--------|-------------|
| `SignInAndTrackAsync(user, password, rememberMe)` | Signs in, updates `LastLoginUtc`, logs the event |
| `SignOutAndTrackAsync(user)` | Signs out and logs the event |
| `RefreshClaimsAsync(user)` | Forces a claims refresh without sign-out (useful after profile updates) |

### 6. Per-Tenant Feature Flags

Extend `TenantSettings` into a proper feature flag system:

```csharp
public interface ITenantFeatureService
{
    bool IsEnabled(string featureKey);
    T? GetValue<T>(string settingKey);
}
```

Registered as scoped (reads from `IUserInfo.TenantId`), so consuming apps can simply inject it and check features without manual tenant lookup.

---

## JC.Web

### 1. Pagination Tag Helper / HTML Helper

`HtmlHelper` already has `PaginationItem` and `PaginationLink`, but a full pagination component would tie it all together:

```csharp
public static string Pagination(PaginatedResult<T> result, Func<int, string> pageUrl,
    int maxVisiblePages = 5, string? containerClass = null);
```

Generates the complete `<nav><ul class="pagination">...</ul></nav>` with first/last/prev/next links, ellipsis for large page ranges, and active/disabled states.

### 2. Toast / Notification Helper

A `TempData`-based flash message system:

```csharp
public static class ToastHelper
{
    public static void AddToast(this ITempDataDictionary tempData, string message, ToastType type);
    public static List<Toast> GetToasts(this ITempDataDictionary tempData);
}

public enum ToastType { Success, Info, Warning, Error }
```

Pair with a Razor partial view or View Component that renders Bootstrap toasts from `TempData`.

### 3. Breadcrumb Builder

A fluent breadcrumb builder for consistent navigation:

```csharp
public class BreadcrumbBuilder
{
    public BreadcrumbBuilder Add(string label, string? url = null);
    public string Build(); // Renders <nav aria-label="breadcrumb"><ol>...</ol></nav>
}
```

### 4. Table Builder

A fluent HTML table builder for consistent data display with sorting headers:

```csharp
public class TableBuilder<T>
{
    public TableBuilder<T> AddColumn(string header, Func<T, string> valueSelector, string? cssClass = null);
    public TableBuilder<T> AddSortableColumn(string header, string sortKey, Func<T, string> valueSelector);
    public string Build(IEnumerable<T> items);
}
```

### 5. Alert / Message Component

Static helper for rendering Bootstrap alerts consistently:

```csharp
public static class AlertHelper
{
    public static string Success(string message, bool dismissible = true);
    public static string Warning(string message, bool dismissible = true);
    public static string Error(string message, bool dismissible = true);
    public static string Info(string message, bool dismissible = true);
}
```

### 6. Form Validation CSS Helper

A helper that bridges `ModelStateWrapper` with client-side Bootstrap validation classes:

```csharp
public static string ValidationClass(this ModelStateWrapper state, string key);
// Returns "is-valid", "is-invalid", or "" based on model state
```

---

## JC.MySql / JC.SqlServer

### 1. Health Check Registration

Both packages could register EF Core database health checks alongside the DbContext:

```csharp
public static IServiceCollection AddMySqlDatabase<TContext>(..., bool addHealthCheck = true)
```

Uses `Microsoft.Extensions.Diagnostics.HealthChecks` — saves consuming apps from manually wiring up `AddDbContextCheck<TContext>()`.

### 2. Connection Resilience

Expose retry/resilience configuration for transient failures:

```csharp
// MySQL
mysql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);

// SQL Server
sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
```

Could be opt-in via the existing options callbacks, or enabled by default with a parameter to disable.

### 3. Read-Only Context Support

Register a second, read-only DbContext pointing to a read replica:

```csharp
public static IServiceCollection AddMySqlReadReplica<TContext>(
    this IServiceCollection services,
    IConfiguration configuration,
    string migrationsAssembly,
    string connectionStringName = "ReadOnlyConnection");
```

Useful for CQRS-style architectures where reads go to replicas.

---

## Cross-Project Ideas

### 1. JC.Logging (New Project)

A logging package providing:
- Structured logging conventions (consistent property names across all JC packages)
- Request/response logging middleware
- Performance logging (method execution time tracking)
- Log enrichment middleware (adds `UserId`, `TenantId`, `CorrelationId` to all log entries)

### 2. JC.Caching (New Project)

A caching abstraction layer:
- `ICacheService` interface with `GetOrSetAsync`, `RemoveAsync`, `RemoveByPrefixAsync`
- Implementations for in-memory (`IMemoryCache`) and distributed (`IDistributedCache`)
- Cache-aside decorator for `IRepositoryContext<T>` — wrap any repository with transparent caching
- Tenant-scoped cache keys (auto-prefixed with `TenantId`)

### 3. JC.BackgroundJobs (New Project)

A thin abstraction for background job scheduling:
- `IBackgroundJobService` with `EnqueueAsync`, `ScheduleAsync`, `RecurringAsync`
- Default in-process implementation using `IHostedService` / `Channel<T>`
- Designed so consuming apps can swap in Hangfire or similar without changing call sites

### 4. Soft-Delete Cascade

Currently soft-delete only marks individual entities. A cascade option would soft-delete related entities:

```csharp
Task<T> SoftDeleteCascadeAsync(T entity, string? userId = null, params Expression<Func<T, object>>[] navigations);
```

### 5. Data Export Helpers

Generic CSV/Excel export from `IQueryable<T>`:

```csharp
public static class DataExportExtensions
{
    public static async Task<byte[]> ToCsvAsync<T>(this IQueryable<T> query, CancellationToken ct = default);
    public static async Task<byte[]> ToExcelAsync<T>(this IQueryable<T> query, string sheetName = "Data", CancellationToken ct = default);
}
```
