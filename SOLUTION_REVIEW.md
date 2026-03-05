# JC-Packages Solution - Comprehensive Review

> **Generated:** 2026-03-04
> **Updated:** 2026-03-05 (v1.1.0 review)
> **Target Framework:** .NET 9.0 (all projects)
> **Author:** jcraik

---

## Table of Contents

- [Solution Overview](#solution-overview)
- [Dependency Graph](#dependency-graph)
- [Configuration Requirements](#configuration-requirements)
- [JC.Core](#jccore)
- [JC.Github](#jcgithub)
- [JC.Identity](#jcidentity)
- [JC.MySql](#jcmysql)
- [JC.SqlServer](#jcsqlserver)
- [JC.Web](#jcweb)
- [Cross-Cutting Concerns](#cross-cutting-concerns)
- [Observations & Recommendations](#observations--recommendations)
- [Changelog](#changelog)

---

## Solution Overview

JC-Packages is a modular NuGet package suite providing reusable infrastructure for .NET 9.0 web applications. The solution is organised into six projects, each with a focused responsibility:

| Project | Version | Purpose | Key Dependencies | Source Files |
|---------|---------|---------|------------------|-------------|
| **JC.Core** | 1.1.0 | Repository pattern, auditing, soft-delete, pagination, utilities | EF Core 9.0.11 | 19 .cs files |
| **JC.Github** | 1.0.0 | GitHub issue tracking and bug reporting | JC.Core, Flurl.Http 4.0.2 | 4 .cs files |
| **JC.Identity** | 1.1.0 | ASP.NET Core Identity, multi-tenancy, middleware | JC.Core, Identity.EFC 9.0.11 | 12 .cs files |
| **JC.MySql** | 1.1.0 | MySQL database provider registration + health checks | JC.Core, Pomelo.EFC.MySql 9.0.0 | 1 .cs file |
| **JC.SqlServer** | 1.1.0 | SQL Server database provider registration + health checks | JC.Core, EFC.SqlServer 9.0.11 | 1 .cs file |
| **JC.Web** | 1.1.0 | HTML builders, tag helpers, dropdowns, QR codes | JC.Core, QRCoder 1.7.0 | 10 .cs files |

All projects are configured as **packable** NuGet packages.

**Build status:** Zero warnings, zero errors.

---

## Dependency Graph

```
JC.Core (foundation)
  ├── JC.Github    (depends on JC.Core)
  ├── JC.Identity  (depends on JC.Core)
  ├── JC.MySql     (depends on JC.Core)
  ├── JC.SqlServer (depends on JC.Core)
  └── JC.Web       (depends on JC.Core)
```

JC.Core is the foundation package — all other projects depend on it. There are no circular dependencies and no cross-references between the leaf packages.

**Note:** Each leaf project includes both a `ProjectReference` (for local development) and a `PackageReference` (for NuGet resolution when the local project isn't available). The `ProjectReference` takes precedence when both are present.

---

## Configuration Requirements

### JC.Github (`AddGithub<TContext>()`)

| Key | Required | Used By |
|-----|----------|---------|
| `Github:Url` | Yes | `GitHelper` — GitHub API base URL |
| `Github:ApiKey` | Yes | `GitHelper` — GitHub API bearer token |
| `Github:Owner` | Yes | `BugReportService` — Repository owner |
| `Github:Repo` | Yes | `BugReportService` — Repository name |

### JC.Identity (`SeedDefaultAdminAsync()`)

| Key | Required | Default Config Key |
|-----|----------|--------------------|
| `Admin:Username` | Yes | Configurable via parameter |
| `Admin:Email` | Yes | Configurable via parameter |
| `Admin:Password` | Yes | Configurable via parameter |
| `Admin:DisplayName` | No | Falls back to "System Administrator" |

### JC.MySql / JC.SqlServer

| Key | Required | Default |
|-----|----------|---------|
| `ConnectionStrings:DefaultConnection` | Yes | Connection string name is configurable |

---

## JC.Core

**Package:** `JC.Core` v1.1.0
**Description:** Core library providing repository pattern, auditing, soft-delete, pagination, and utility helpers for .NET applications.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| Microsoft.EntityFrameworkCore | 9.0.11 |
| Microsoft.EntityFrameworkCore.Relational | 9.0.11 |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.11 |

### Project Structure

```
JC.Core/
├── Data/
│   ├── DataDbContext.cs
│   └── IDataDbContext.cs
├── Enums/
│   └── DeletedQueryType.cs
├── Extensions/
│   ├── DateTimeExtensions.cs
│   ├── EnumExtensions.cs
│   ├── PaginationExtensions.cs
│   ├── QueryExtensions.cs
│   ├── ServiceCollectionExtensions.cs
│   ├── ServiceProviderExtensions.cs
│   └── StringExtensions.cs
├── Helpers/
│   ├── ColourHelper.cs
│   ├── ConstHelper.cs
│   ├── CountryHelper.cs
│   └── PaginationHelper.cs
├── Models/
│   ├── IUserInfo.cs
│   ├── Auditing/
│   │   ├── AuditAction.cs
│   │   ├── AuditEntry.cs
│   │   └── AuditModel.cs
│   └── Pagination/
│       ├── IPagination.cs
│       └── PagedList.cs
└── Services/
    ├── AuditService.cs
    └── DataRepositories/
        ├── IRepositoryContext.cs
        ├── IRepositoryManager.cs
        ├── RepositoryContext.cs
        └── RepositoryManager.cs
```

### Data Layer

#### `IDataDbContext` (Interface)
Contract for the data context, exposes:
- `DbSet<AuditEntry> AuditEntries`
- `Task<int> SaveChangesAsync(CancellationToken)`

#### `DataDbContext` (Class : DbContext, IDataDbContext)
Default EF Core DbContext implementation. Configures:
- **AuditEntry:** PK on `Id`, `Action` required, `AuditDate` required, indexes on `UserId`, `TableName`, `AuditDate`

### Enums

#### `DeletedQueryType`
| Value | Description |
|-------|-------------|
| `All` (0) | Include all records |
| `OnlyActive` (1) | Exclude soft-deleted records |
| `OnlyDeleted` (2) | Only soft-deleted records |

### Models

#### `IUserInfo` (Interface)
Comprehensive user contract with 20+ properties covering:
- **Identity:** `UserId`, `Username`
- **Contact:** `Email`, `EmailConfirmed`, `PhoneNumber`, `PhoneNumberConfirmed`
- **Security:** `TwoFactorEnabled`, `LockoutEnabled`, `LockoutEnd`, `AccessFailedCount`
- **Tenant:** `TenantId`, `MultiTenancyEnabled`
- **Profile:** `DisplayName`, `LastLoginUtc`, `IsEnabled`, `RequiresPasswordChange`
- **System:** `IsSetup`
- **Authorisation:** `Roles` (IReadOnlyList\<string>), `Claims` (IReadOnlyList\<Claim>)
- **Method:** `bool IsInRole(string role)`

#### `AuditAction` (Enum)
`Create` (0), `Update` (1), `Delete` (2), `Restore` (3)

#### `AuditEntry` (Class)

| Property | Type | Notes |
|----------|------|-------|
| `Id` | string | GUID |
| `Action` | AuditAction | CRUD+Restore |
| `AuditDate` | DateTime | When action occurred |
| `UserId` | string? | Who performed it |
| `UserName` | string? | Display name |
| `TableName` | string? | Affected table |
| `ActionData` | string? | JSON-serialised entity data (System.Text.Json) |

#### `AuditModel` (Class)
Base class for auditable entities. Provides automatic audit field population:

| Property | Type | Set By |
|----------|------|--------|
| `CreatedById` | string? | `FillCreated(userId)` |
| `CreatedUtc` | DateTime | `FillCreated(userId)` |
| `LastModifiedById` | string? | `FillModified(userId)` |
| `LastModifiedUtc` | DateTime | `FillModified(userId)` |
| `DeletedById` | string? | `FillDeleted(userId)` |
| `DeletedUtc` | DateTime? | `FillDeleted(userId)` |
| `IsDeleted` | bool | `FillDeleted` / `FillRestored` |
| `RestoredById` | string? | `FillRestored(userId)` |
| `RestoredUtc` | DateTime? | `FillRestored(userId)` |

All setters are **private** — state is only changed through the `Fill*` methods, ensuring consistency. `FillDeleted` clears restore fields; `FillRestored` clears delete fields.

### Pagination

#### `IPagination<T>` (Interface : IReadOnlyList\<T>)

| Property | Type | Notes |
|----------|------|-------|
| `Items` | IReadOnlyList\<T> | The page items |
| `PageNumber` | int | Current page (1-based) |
| `PageSize` | int | Items per page |
| `TotalCount` | int | Total items across all pages |
| `TotalPages` | int | Computed |
| `HasPreviousPage` | bool | Default implementation |
| `HasNextPage` | bool | Default implementation |
| `IsFirstPage` | bool | Default implementation |
| `IsLastPage` | bool | Default implementation |

#### `PagedList<T>` (Implementation)
Implements `IPagination<T>` and `IReadOnlyList<T>`. Constructor validates `pageNumber >= 1` and `pageSize >= 1`.

#### `PaginationHelper` (Static, Internal)
Internal helper with skip/take logic and page validation.

#### `PaginationExtensions` (Static)

| Method | Description |
|--------|-------------|
| `ToPagedList<T>(IEnumerable, page, pageSize)` | In-memory pagination |
| `ToPagedListAsync<T>(IQueryable, page, pageSize, ct)` | Async EF Core pagination |
| `ToPagedList<T>(IQueryable, page, pageSize)` | Synchronous queryable pagination |

### Extensions

#### `EnumExtensions` (Static)

| Method | Description |
|--------|-------------|
| `GetAllOptions<T>()` | Returns `List<(string Name, int Value)>` for all enum members |
| `ToDisplayName()` | Converts PascalCase/underscore enum names to human-readable display strings |
| `GetDescription()` | Returns `[Description]` attribute value or falls back to `ToDisplayName()` |
| `TryParse<T>(string?, T)` | Case-insensitive parse with default fallback |

#### `StringExtensions` (Static, Partial)

| Method | Description |
|--------|-------------|
| `Truncate(maxLength, suffix)` | Truncates with suffix (default `"..."`): `"Hello World".Truncate(8)` → `"Hello..."` |
| `ToSlug()` | URL-friendly slug: `"My Blog Post!"` → `"my-blog-post"`. Uses source-generated regex |
| `ToTitleCase(culture?)` | Culture-aware title casing via `TextInfo.ToTitleCase` |
| `Mask(visibleChars)` | Masks with asterisks: `"john@email.com".Mask(3)` → `"joh***********"` |

#### `DateTimeExtensions` (Static)

| Method | Description |
|--------|-------------|
| `ToRelativeTime()` | Human-readable relative time: `"5 minutes ago"`, `"yesterday"`, `"in 3 days"`, `"just now"`. Handles past and future |
| `ToFriendlyDate(culture?)` | Full date format: `"Wednesday 5 March 2026"` |
| `Age()` | Whole years from date of birth, accounts for birthday not yet occurred |

#### `QueryExtensions` (Static)

| Method | Description |
|--------|-------------|
| `FilterDeleted<T>(IQueryable<T>, DeletedQueryType)` | Filters AuditModel entities by soft-delete status |

#### `ServiceCollectionExtensions` (Static)

| Method | Description |
|--------|-------------|
| `AddCore<TContext>()` | Registers AuditService, IDataDbContext, DbContext, IRepositoryManager, and repository contexts for AuditModel |
| `RegisterRepositoryContext<T>()` | Registers a single `IRepositoryContext<T>` / `RepositoryContext<T>` pair |
| `RegisterRepositoryContexts(params Type[])` | Registers multiple repository contexts via reflection |

#### `ServiceProviderExtensions` (Static)

| Method | Description |
|--------|-------------|
| `MigrateDatabaseAsync<TContext>()` | Applies pending EF Core migrations asynchronously |

### Helpers

#### `ColourHelper` (Static)

| Method | Description |
|--------|-------------|
| `HoverColour(string hex)` | Lightens a hex colour by 40% for hover states |
| `FontColour(string hex)` | Returns `#000000` or `#ffffff` based on luminance (WCAG-style threshold at 0.5) |

#### `ConstHelper` (Static)

| Method | Description |
|--------|-------------|
| `GetAllConsts<T>()` | Returns `Dictionary<string, object>` of all `const` fields in type T via reflection |

#### `CountryHelper` (Static)

| Method | Description |
|--------|-------------|
| `GetCountries(ILogger?)` | Returns `IReadOnlyList<Country>` from .NET CultureInfo data (cached, deduplicated, sorted). Logs warnings for failed cultures |
| `GetCountriesDictionary()` | Returns `Dictionary<string, string>` (Code → Name) |
| `GetCountryName(string code)` | Looks up country name by ISO 3166-1 alpha-2 code |
| `GetCountryCode(string name)` | Looks up country code by name |

**Nested:** `record Country(string Code, string Name)`

### Services

#### `AuditService`
**Dependencies:** `IDataDbContext`, `IUserInfo`
**Serialiser:** `System.Text.Json`

| Method | Description |
|--------|-------------|
| `LogAsync(AuditAction, tableName, data?)` | Creates AuditEntry with user info, serialises data as JSON |
| `LogCreateAsync(tableName, data?)` | Convenience for `AuditAction.Create` |
| `LogUpdateAsync(tableName, data?)` | Convenience for `AuditAction.Update` |
| `LogDeleteAsync(tableName, data?)` | Convenience for `AuditAction.Delete` |
| `LogRestoreAsync(tableName, data?)` | Convenience for `AuditAction.Restore` |

### Repository Pattern

#### `IRepositoryContext<T>` (Interface)
Full generic CRUD interface with **23 methods**:

| Category | Methods |
|----------|---------|
| **Query** | `AsQueryable()`, `GetAll(predicate)`, `GetAll(predicate, orderBy)`, `GetAllAsync(predicate, ct)`, `GetAllAsync(predicate, orderBy, ct)`, `GetByIdAsync(int, ct)`, `GetByIdAsync(string, ct)`, `GetByIdAsync(params object[])` |
| **Create** | `AddAsync(entity, userId?, saveNow?, ct)`, `AddAsync(IEnumerable, userId?, saveNow?, ct)`, `AddRangeAsync(IEnumerable, userId?, saveNow?, ct)` |
| **Update** | `UpdateAsync(entity, userId?, saveNow?, ct)`, `UpdateAsync(IEnumerable, userId?, saveNow?, ct)`, `UpdateRangeAsync(IEnumerable, userId?, saveNow?, ct)` |
| **Soft Delete** | `SoftDeleteAsync(entity, userId?, saveNow?, ct)`, `SoftDeleteAsync(IEnumerable, userId?, saveNow?, ct)`, `SoftDeleteRangeAsync(IEnumerable, userId?, saveNow?, ct)` |
| **Restore** | `RestoreAsync(entity, userId?, saveNow?, ct)`, `RestoreAsync(IEnumerable, userId?, saveNow?, ct)`, `RestoreRangeAsync(IEnumerable, userId?, saveNow?, ct)` |
| **Hard Delete** | `DeleteAsync(entity, saveNow?, ct)`, `DeleteAsync(IEnumerable, saveNow?, ct)`, `DeleteRangeAsync(IEnumerable, saveNow?, ct)` |

All write operations accept optional `userId` (defaults to current user), `saveNow` (defaults to `true`), and `CancellationToken` (defaults to `default`).

#### `RepositoryContext<T>` (Implementation)
- Auto-detects if `T` extends `AuditModel` (uses `FillCreated`, `FillModified`, etc.)
- Falls back to reflection-based `IsDeleted` property detection for non-AuditModel entities
- All operations log errors via `ILogger<RepositoryContext<T>>` and re-throw
- All async operations propagate `CancellationToken` to EF Core

#### `IRepositoryManager` / `RepositoryManager`
- **Repository cache:** `ConcurrentDictionary<Type, object>` for thread-safe lazy initialisation
- **Transaction management:** `BeginTransactionAsync(ct)`, `CommitTransactionAsync(ct)`, `RollbackTransactionAsync(ct)`
- **Implements:** `IDisposable`, `IAsyncDisposable`

---

## JC.Github

**Package:** `JC.Github` v1.0.0
**Description:** GitHub integration for JC.Core providing bug report and issue tracking services.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.1.0 |
| Flurl.Http | 4.0.2 |
| Microsoft.EntityFrameworkCore | 9.0.11 |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.11 |

### Project Structure

```
JC.Github/
├── Data/
│   └── IGithubDbContext.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Helpers/
│   └── GitHelper.cs
├── Models/
│   └── ReportedIssue.cs
└── Services/
    └── BugReportService.cs
```

### Data

#### `IGithubDbContext` (Interface)
- `DbSet<ReportedIssue> ReportedIssues`

### Models

#### `ReportedIssue` (Class)

| Property | Type | Notes |
|----------|------|-------|
| `Id` | string | GUID, private set |
| `Type` | IssueType (enum) | `Suggestion` or `Bug` |
| `Description` | required string | Required |
| `Image` | byte[]? | Optional screenshot |
| `ReportSent` | bool | Sent to GitHub |
| `ExternalId` | int? | GitHub issue number |
| `Closed` | bool | Resolution status |
| `Created` | DateTime | Creation timestamp |
| `UserId` | string? | Reporter ID |
| `UserDisplay` | string? | Reporter display name |

### Helpers

#### `GitHelper` (Class)
GitHub API integration using Flurl.Http. Registered as a **singleton** in `AddGithub()` from `Github:Url` and `Github:ApiKey` configuration values.

| Method | Description |
|--------|-------------|
| Constructor | Configures FlurlClient with Bearer token, `X-GitHub-Api-Version: 2022-11-28` header, `User-Agent: JC.Core` |
| `RecordIssue(owner, repo, title, desc)` | Creates a GitHub issue, returns issue number |

**Nested:** Private `NewIssueResponse` class for JSON deserialisation.

### Services

#### `BugReportService`
**Dependencies:** `IConfiguration`, `IGithubDbContext`, `GitHelper`, `ILogger<BugReportService>`

| Method | Description |
|--------|-------------|
| `RecordIssue(description, issueType, creatorId?, creatorName?)` | Creates ReportedIssue entity, attempts to create GitHub issue (graceful failure with logging), saves to DB |

### Extensions

#### `ServiceCollectionExtensions` (Static)

| Method | Description |
|--------|-------------|
| `AddGithub<TContext>()` | Registers `GitHelper` (singleton from config), `BugReportService`, `IGithubDbContext`, and repository context for `ReportedIssue`. Throws `InvalidOperationException` if config values missing |

---

## JC.Identity

**Package:** `JC.Identity` v1.1.0
**Description:** Identity library providing ASP.NET Core Identity integration, multi-tenancy, middleware, and user management helpers.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.1.0 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.11 |
| Microsoft.AspNetCore.App | Framework Reference |

**Note:** All JSON serialisation uses `System.Text.Json`.

### Project Structure

```
JC.Identity/
├── Authentication/
│   ├── DefaultClaims.cs
│   ├── DefaultClaimsPrincipalFactory.cs
│   └── SystemRoles.cs
├── Data/
│   └── IdentityDataDbContext.cs
├── Extensions/
│   ├── ApplicationBuilderExtensions.cs
│   ├── QueryExtensions.cs
│   ├── ServiceCollectionExtensions.cs
│   └── Options/
│       └── IdentityMiddlewareOptions.cs
├── Middleware/
│   ├── IdentityMiddleware.cs
│   └── UserInfoMiddleware.cs
└── Models/
    ├── BaseRole.cs
    ├── BaseUser.cs
    ├── UserInfo.cs
    └── MultiTenancy/
        ├── IMultiTenancy.cs
        └── Tenant.cs
```

### Authentication

#### `DefaultClaims` (Static Constants)
12 custom claim types: `email_confirmed`, `phone_number`, `phone_number_confirmed`, `two_factor_enabled`, `lockout_enabled`, `lockout_end`, `access_failed_count`, `tenant_id`, `display_name`, `last_login_utc`, `is_enabled`, `require_password_change`

#### `DefaultClaimsPrincipalFactory<TUser, TRole>`
Extends `UserClaimsPrincipalFactory<TUser, TRole>`. Overrides `GenerateClaimsAsync` to add all 12 custom claims from the user entity to the `ClaimsIdentity`. Uses primary constructor syntax.

#### `SystemRoles` (Static)
Built-in roles:
- **SystemAdmin** — Full system administrator with tenant management access
- **Admin** — Administrator with access to all features within their tenant

**Method:** `GetAllRoles<T>()` — Uses reflection to discover all `const string` role fields and their matching `*Desc` description fields from the type hierarchy. Designed to be extended by consuming applications (e.g. `class AppRoles : SystemRoles`).

### Data

#### `IdentityDataDbContext<TUser, TRole>`
Extends `IdentityDbContext<TUser, TRole, string>` and implements `IDataDbContext`.

**DbSets:** `AuditEntries`, `Tenants`

**OnModelCreating** configures:
- AuditEntry (PK, required Action/AuditDate, indexes on UserId, TableName, AuditDate)
- Tenant (PK, required Name, index on Domain)
- Calls `ApplyTenantQueryFilters()` for automatic multi-tenancy filtering

### Models

#### `BaseUser` (: IdentityUser, IMultiTenancy)

| Property | Type | Default |
|----------|------|---------|
| `TenantId` | string? | null |
| `Tenant` | Tenant? | null (navigation) |
| `DisplayName` | string? | null |
| `LastLoginUtc` | DateTime? | null |
| `IsEnabled` | bool | false |
| `RequirePasswordChange` | bool | false |

#### `BaseRole` (: IdentityRole)
- `Description` (string?) — Role description

#### `UserInfo` (: IUserInfo)
Runtime user state populated from claims. Includes:
- System constants: `SYSTEM_USER_ID`, `SYSTEM_USER_NAME`, `SYSTEM_USER_EMAIL`
- Unknown constants: `UNKNOWN_USER_ID`, `UNKNOWN_USER_NAME`, `UNKNOWN_USER_EMAIL`
- All IUserInfo properties with sensible defaults
- `IsInRole(string)` — Checks both `Roles` list and role claims

### Multi-Tenancy

#### `IMultiTenancy` (Interface)
Contract: `TenantId` (string?) and `Tenant` (Tenant?) navigation property.

#### `Tenant` (sealed : AuditModel)

| Property | Type | Notes |
|----------|------|-------|
| `Id` | string | GUID default |
| `Name` | required string | Required |
| `Description` | string? | Optional |
| `Domain` | string? | Indexed |
| `MaxUsers` | uint? | User limit |
| `ExpiryDateUtc` | DateTime? | Tenant expiry |
| `Settings` | string | JSON (System.Text.Json), default `"[]"`, private set |

**Methods:**
- `SetSettings(IEnumerable<TenantSettings>)` — Serialises settings to JSON
- `SetSetting(key, value, isActive)` — Add/update single setting
- `GetSettings()` — Deserialises JSON to `List<TenantSettings>`

#### `TenantSettings` (sealed)
Simple KVP: `Key` (string?), `Value` (string?), `IsActive` (bool)

### Middleware

#### `UserInfoMiddleware`
**Logging:** `ILogger<UserInfoMiddleware>` — logs debug messages for unauthenticated requests and successful user population.

Populates `IUserInfo` from `ClaimsPrincipal` on first request per scope:
- Sets system user constants for unauthenticated requests
- Extracts all 12 custom claims + roles for authenticated requests
- Sets `IsSetup = true` to avoid re-processing

#### `IdentityMiddleware`
**Logging:** `ILogger<IdentityMiddleware>` — logs warnings for disabled user access, info for password change and 2FA redirects.
**Options:** `IdentityMiddlewareOptions`

Pipeline enforcement:
1. Skips static files (.css, .js, images, fonts, .map, .json, .xml)
2. Skips unauthenticated requests
3. Skips excluded paths (AccessDenied, Logout, Error routes)
4. **Checks `IsEnabled`** → redirects to AccessDenied if disabled (logged as warning)
5. **Checks `RequiresPasswordChange`** (if enabled) → redirects to ChangePassword route (logged as info)
6. **Checks 2FA enforcement** (if enabled via global option) → redirects to TwoFactor setup route (logged as info)

#### `IdentityMiddlewareOptions`

| Property | Type | Default |
|----------|------|---------|
| `RequirePasswordChange` | bool | `true` |
| `ChangePasswordRoute` | string | `/Identity/Account/Manage/SetPassword` |
| `EnforceTwoFactor` | bool | `false` |
| `TwoFactorRoute` | string | `/Identity/Account/Manage/EnableAuthenticator` |
| `AccessDeniedRoute` | string | `/Identity/Account/AccessDenied` |
| `LogoutRoute` | string | `/Identity/Account/Logout` |
| `ErrorRoute` | string | `/Error` |
| `ExcludedPaths` | string[] | Computed: [AccessDeniedRoute, LogoutRoute, ErrorRoute] |

### Extensions

#### `ApplicationBuilderExtensions`

| Method | Description |
|--------|-------------|
| `UseUserInfo()` | Adds `UserInfoMiddleware` to pipeline |
| `UseIdentityMiddleware()` | Adds `IdentityMiddleware` to pipeline |
| `UseIdentity()` | Full pipeline: Authentication → UserInfo → Authorisation → IdentityMiddleware |
| `ConfigureAdminAndRolesAsync<TUser, TRoles, TRole>()` | Seeds roles + default admin in one call |
| `SeedRolesAsync<TRoles, TRole>()` | Creates missing roles from `SystemRoles.GetAllRoles<TRoles>()` |
| `SeedDefaultAdminAsync<TUser, TRole>()` | Creates admin user from config, optionally creates default tenant |

#### `ServiceCollectionExtensions`

| Method | Description |
|--------|-------------|
| `AddIdentity<TUser, TRole, TUserInfo>(options?)` | Full registration: auth, custom UserInfo type, claims factory, tenant repository |
| `AddIdentity<TUser, TRole>(options?)` | Shorthand using default `UserInfo` implementation |
| `AddIdentity<TUser, TRole>(IdentityBuilder, options?)` | Extension on `IdentityBuilder` |

#### `QueryExtensions`

| Method | Description |
|--------|-------------|
| `AllTenants<T>(IQueryable, IUserInfo)` | SystemAdmins bypass tenant query filters via `IgnoreQueryFilters()` |
| `ApplyTenantQueryFilters(ModelBuilder, IUserInfo)` | Applies global query filter: matches `TenantId` or allows null tenant for users without tenant |

---

## JC.MySql

**Package:** `JC.MySql` v1.1.0
**Description:** MySQL database provider extensions for JC.Core using Pomelo.EntityFrameworkCore.MySql.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.1.0 |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 |
| AspNetCore.HealthChecks.MySql | 9.0.0 |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.11 |

### `ServiceCollectionExtensions` (Static)

**Two overloads of `AddMySqlDatabase`:**

1. **Non-generic:** Uses default `DataDbContext`
2. **Generic:** `AddMySqlDatabase<TContext>()` where `TContext : DbContext, IDataDbContext`

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `configuration` | IConfiguration | required |
| `migrationsAssembly` | string | required |
| `connectionStringName` | string | `"DefaultConnection"` |
| `mySqlOptions` | Action\<MySqlDbContextOptionsBuilder>? | null |
| `addHealthCheck` | bool | `false` |

**Behaviour:**
- Retrieves connection string from `IConfiguration.GetConnectionString()`
- Throws `InvalidOperationException` if connection string not found
- Auto-detects MySQL server version via `ServerVersion.AutoDetect()`
- Configures migrations assembly
- Invokes optional custom MySQL options callback
- When `addHealthCheck` is `true`, registers a MySQL health check via `AspNetCore.HealthChecks.MySql`

---

## JC.SqlServer

**Package:** `JC.SqlServer` v1.1.0
**Description:** SQL Server database provider extensions for JC.Core using Microsoft.EntityFrameworkCore.SqlServer.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.1.0 |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.11 |
| AspNetCore.HealthChecks.SqlServer | 9.0.0 |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.11 |

### `ServiceCollectionExtensions` (Static)

**Two overloads of `AddSqlServerDatabase`:**

1. **Non-generic:** Uses default `DataDbContext`
2. **Generic:** `AddSqlServerDatabase<TContext>()` where `TContext : DbContext, IDataDbContext`

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `configuration` | IConfiguration | required |
| `migrationsAssembly` | string | required |
| `connectionStringName` | string | `"DefaultConnection"` |
| `sqlServerOptions` | Action\<SqlServerDbContextOptionsBuilder>? | null |
| `addHealthCheck` | bool | `false` |

**Behaviour:** Identical pattern to JC.MySql — retrieves connection string, validates, registers DbContext with SQL Server provider. When `addHealthCheck` is `true`, registers a SQL Server health check via `AspNetCore.HealthChecks.SqlServer`.

---

## JC.Web

**Package:** `JC.Web` v1.1.0
**Description:** Web helpers for ASP.NET Core including dropdown builders, HTML tag builder, pagination tag helper, model state wrapper, and QR code generation.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.1.0 |
| QRCoder | 1.7.0 |
| Microsoft.AspNetCore.App | Framework Reference |

### Project Structure

```
JC.Web/
├── Helpers/
│   ├── DropdownHelper.cs
│   ├── ModelStateWrapper.cs
│   ├── QrCodeHelper.cs
│   └── HTML/
│       ├── AlertHelper.cs
│       ├── BreadcrumbBuilder.cs
│       ├── HtmlHelper.cs
│       ├── HtmlTagBuilder.cs
│       └── TableBuilder.cs
└── TagHelpers/
    ├── AlertTagHelper.cs
    ├── BreadcrumbTagHelper.cs
    └── PaginationTagHelper.cs
```

### HTML Builders

#### `HtmlTagBuilder` (Class)

Fluent HTML tag builder with internal constructor. Attribute values are HTML-encoded via `WebUtility.HtmlEncode`.

| Method | Description |
|--------|-------------|
| `AddClass(className)` | Adds CSS class (ignores empty) |
| `AddAttribute(name, value)` | Adds/overwrites HTML attribute (value HTML-encoded) |
| `AddActiveAttribute()` | Adds "active" CSS class |
| `AddCurrentPageAttribute()` | Adds `aria-current="page"` |
| `AddDisabledClass()` | Adds "disabled" CSS class |
| `SetContent(content)` | Sets inner HTML content |
| `Build()` | Returns final HTML string |
| `implicit operator string` | Auto-converts to string via `Build()` |

Supports self-closing tags (e.g., `<input />`, `<br />`).

#### `HtmlHelper` (Static)

| Method | Description |
|--------|-------------|
| `CreateElement(tag, content, isActive, isDisabled, attributes, classes)` | Generic element builder |
| `PaginationItem(content, isActive, isDisabled)` | `<li class="page-item">` with active/disabled states |
| `PaginationLink(text, href, buttonClass, isActive)` | `<a class="page-link">` with aria-current support |

#### `AlertHelper` (Static)

DRY design — single private `Alert(message, cssClass, dismissible)` method; public methods pass the Bootstrap class:

| Method | Description |
|--------|-------------|
| `Success(message, dismissible)` | `alert-success` alert |
| `Warning(message, dismissible)` | `alert-warning` alert |
| `Error(message, dismissible)` | `alert-danger` alert |
| `Info(message, dismissible)` | `alert-info` alert |
| `ForType(AlertType, message, dismissible)` | Renders by enum value |

**Enum `AlertType`:** `Success`, `Warning`, `Error`, `Info`

Dismissible alerts (default) include `alert-dismissible fade show` classes and a Bootstrap close button.

#### `BreadcrumbBuilder` (Class)

Fluent builder for Bootstrap 5 breadcrumb navigation. Last item is always rendered as active with `aria-current="page"`. All labels and URLs are HTML-encoded. Implicit string conversion.

| Method | Description |
|--------|-------------|
| `Add(label, url?)` | Adds a breadcrumb item. Items with URL get `<a>` tags |
| `Build()` | Returns complete `<nav><ol class="breadcrumb">...</ol></nav>` |

#### `TableBuilder<T>` (Class)

Generic fluent builder for rendering Bootstrap HTML tables. All cell content is HTML-encoded to prevent XSS. Column CSS classes apply to both `<th>` and `<td>`.

| Method | Description |
|--------|-------------|
| `AddColumn(header, Func<T, string?>, cssClass?)` | Adds a column with string value selector |
| `AddColumn(header, Func<T, object?>, cssClass?)` | Adds a column with object selector (`.ToString()`) |
| `Build(items, tableClass?)` | Returns complete `<table>` HTML. Default class: `"table"` |

### Other Helpers

#### `DropdownHelper` (Static)

| Method | Description |
|--------|-------------|
| `ToDropdownEntry(text, value, selected)` | Creates a single `SelectListItem` |
| `FromEnum<T>(selected?)` | Converts all enum values to dropdown items using `ToDisplayName()` |
| `FromCollection<T>(items, textSelector, valueSelector, selectedPredicate?)` | Generic collection → dropdown with custom selectors |
| `FromDictionary(items, selected?)` | Dictionary (Key=value, Value=text) → dropdown |
| `GetCountryDropdown(selected?)` | Pre-built country dropdown from `CountryHelper` |
| `WithPlaceholder(items, text, value)` | Extension: inserts placeholder item at index 0 (default: "Please select...") |

#### `ModelStateWrapper` (Class)

Wraps `ModelStateDictionary` with automatic key prefixing. Uses primary constructor syntax.

| Constructor Param | Purpose |
|-------------------|---------|
| `modelState` | The ModelStateDictionary to wrap |
| `prefix` | Key prefix (default: `"Input."`) — auto-appends `.` if missing |
| `ignorePrefix` | Set true for no prefix |

| Member | Description |
|--------|-------------|
| `IsValid` | Delegates to `ModelState.IsValid` |
| `this[key]` | Indexer: returns first error message for prefixed key, or empty string |
| `AddModelError(key, message)` | Adds error with prefix |
| `HasError(key)` | Checks if prefixed key has errors |
| `GetErrors(key)` | Returns all errors for prefixed key |
| `GetAllErrors()` | Returns `Dictionary<string, string[]>` of all errors |

#### `QrCodeHelper` (Class)

| Constructor | Description |
|-------------|-------------|
| `QrCodeHelper()` | SVG format, 10px/module, ECC level M |
| `QrCodeHelper(format, pixelsPerModule, eccLevel)` | Custom configuration (clamps pixelsPerModule to minimum 10 if <= 0) |

| Method | Description |
|--------|-------------|
| `GenerateQrCode(content)` | Returns SVG string or base64 PNG data URI. Throws `ArgumentException` for empty content |

**Enum `QrCodeFormat`:** `Svg` (0), `Base64` (1)

### Tag Helpers

#### `PaginationTagHelper`

Renders Bootstrap pagination from an `IPagination<T>` model.

```html
<pagination model="Model.Items" href-format="/items?page={0}" />
```

| Attribute | Type | Default |
|-----------|------|---------|
| `model` | IPagination\<object>? | required |
| `href-format` | string | `"?page={0}"` |
| `max-pages` | int | 5 |
| `previous-text` | string | `&laquo;` |
| `next-text` | string | `&raquo;` |
| `show-first-last` | bool | true |
| `container-class` | string? | null |

Features: first/last links, previous/next, page numbers with ellipsis, active/disabled states.

#### `AlertTagHelper`

Renders a Bootstrap 5 alert. Delegates to `AlertHelper.ForType()`.

```html
<alert type="Success" message="Saved successfully!" />
<alert type="Error" message="Something went wrong." dismissible="false" />
```

| Attribute | Type | Default |
|-----------|------|---------|
| `type` | AlertType | `Info` |
| `message` | string? | required |
| `dismissible` | bool | true |

#### `BreadcrumbTagHelper` + `CrumbTagHelper`

Nested tag helper pattern — children pass data to parent via `TagHelperContext.Items`.

```html
<breadcrumb>
  <crumb label="Home" href="/" />
  <crumb label="Products" href="/products" />
  <crumb label="Widget" />
</breadcrumb>
```

Last crumb is automatically rendered as the active page.

---

## Cross-Cutting Concerns

### JSON Serialisation

The entire solution uses **`System.Text.Json`** consistently:
- `AuditService` — serialises audit data
- `Tenant` — serialises/deserialises tenant settings

### Design Patterns Used

| Pattern | Where |
|---------|-------|
| **Repository** | `IRepositoryContext<T>` / `RepositoryContext<T>` |
| **Unit of Work** | `IRepositoryManager` / `RepositoryManager` (transaction management) |
| **Soft Delete** | `AuditModel.FillDeleted()` / `FillRestored()`, `FilterDeleted()` query extension |
| **Audit Trail** | `AuditModel` base class + `AuditService` for explicit logging |
| **Multi-Tenancy** | Global EF Core query filters via `IMultiTenancy` interface |
| **Claims-Based Identity** | Custom `DefaultClaimsPrincipalFactory` with 12 extended claims |
| **Middleware Pipeline** | `UserInfoMiddleware` → `IdentityMiddleware` for request processing |
| **Builder** | `HtmlTagBuilder`, `BreadcrumbBuilder`, `TableBuilder<T>` fluent APIs |
| **Options Pattern** | `IdentityMiddlewareOptions` via `IOptions<T>` |

### Dependency Injection Registration

A consuming application would typically wire up:

```csharp
// Program.cs
services.AddCore<MyDbContext>();                                                // JC.Core
services.AddGithub<MyDbContext>(configuration);                                // JC.Github
services.AddIdentity<MyUser, MyRole>();                                        // JC.Identity
services.AddMySqlDatabase<MyDbContext>(config, "MyApp", addHealthCheck: true);  // JC.MySql (or JC.SqlServer)

app.UseIdentity();                                                              // JC.Identity middleware
await app.ConfigureAdminAndRolesAsync<MyUser, MyRoles, MyRole>();
await app.Services.MigrateDatabaseAsync<MyDbContext>();
```

### Multi-Tenancy Flow

1. Entities implement `IMultiTenancy` with `TenantId` property
2. `IdentityDataDbContext.OnModelCreating()` calls `ApplyTenantQueryFilters()`
3. Global query filter auto-scopes all queries to current user's `TenantId`
4. Users without a tenant see only entities with `TenantId == null`
5. `SystemAdmin` role users can bypass via `AllTenants()` extension
6. `SeedDefaultAdminAsync()` can optionally create a default tenant

### Middleware Pipeline Order

```
Request → Authentication → UserInfoMiddleware → Authorisation → IdentityMiddleware → Application
```

The order matters:
- `UserInfoMiddleware` runs after authentication so `ClaimsPrincipal` is available
- `UserInfoMiddleware` runs before authorisation so `IUserInfo` is populated for policy evaluation
- `IdentityMiddleware` runs last to enforce business rules (disabled accounts, password change, 2FA)

---

## Observations & Recommendations

### Strengths

1. **Clean separation of concerns** — Six projects, each with a focused responsibility, no circular dependencies
2. **Consistent patterns** — Repository pattern, DI registration, and extension methods follow a uniform style throughout
3. **Comprehensive auditing** — Full CRUD audit trail with user tracking, timestamps, and JSON data serialisation
4. **Flexible multi-tenancy** — Global query filters with admin bypass and tenant settings system
5. **Well-designed soft delete** — Private setters on AuditModel ensure state consistency; mutual clearing of delete/restore fields prevents stale data
6. **Thread-safe repository caching** — `ConcurrentDictionary` in `RepositoryManager`
7. **Packable architecture** — All projects configured as NuGet packages for reuse across applications
8. **Consistent serialisation** — Entire solution standardised on `System.Text.Json`
9. **CancellationToken support** — All repository async operations propagate cancellation tokens
10. **Middleware logging** — Both middleware classes log meaningful events at appropriate levels
11. **DI-friendly design** — `TryAddScoped` throughout prevents double-registration conflicts
12. **Clean build** — Zero warnings, zero errors
13. **XSS prevention** — `HtmlTagBuilder` encodes attribute values; `TableBuilder` encodes all cell content; `BreadcrumbBuilder` encodes labels and URLs
14. **Rich utility extensions** — String (truncate, slug, mask, title case), DateTime (relative time, friendly date, age), enum (display name, description, parse), and pagination extensions eliminate boilerplate in consuming apps
15. **Comprehensive tag helpers** — Pagination, alerts, and breadcrumbs all available as both programmatic helpers and declarative Razor tag helpers
16. **Health check support** — Optional database health check registration in both MySql and SqlServer packages

### Reviewed & Dismissed

1. **No test projects** — Acknowledged. Testing is not a priority for this package suite at this time.
2. **`ChangePasswordRoute` default** — The route `/Identity/Account/Manage/SetPassword` is intentionally correct.

---

## Changelog

### v1.1.0 (2026-03-05)

| Change | Project | Details |
|--------|---------|---------|
| **New project: JC.Github** | JC.Github | Extracted GitHub integration (GitHelper, BugReportService, ReportedIssue, IGithubDbContext) from JC.Core into a standalone package |
| **Pagination system** | JC.Core | `IPagination<T>`, `PagedList<T>`, `PaginationHelper`, `PaginationExtensions` (`ToPagedList`, `ToPagedListAsync`) |
| **Pagination tag helper** | JC.Web | `<pagination>` tag helper with first/last/prev/next, ellipsis, configurable max pages |
| **String extensions** | JC.Core | `Truncate`, `ToSlug` (source-generated regex), `ToTitleCase`, `Mask` |
| **DateTime extensions** | JC.Core | `ToRelativeTime` (past + future), `ToFriendlyDate`, `Age` |
| **AlertHelper + tag helper** | JC.Web | DRY `AlertHelper` (Success/Warning/Error/Info) + `<alert>` tag helper with dismissible support |
| **BreadcrumbBuilder + tag helper** | JC.Web | Fluent `BreadcrumbBuilder` + nested `<breadcrumb>`/`<crumb>` tag helpers with auto-active last item |
| **TableBuilder** | JC.Web | Generic `TableBuilder<T>` with HTML-encoded cells, column CSS classes, string and object selectors |
| **Health check registration** | JC.MySql, JC.SqlServer | Optional `addHealthCheck` parameter (default `false`) using `AspNetCore.HealthChecks.MySql`/`.SqlServer` v9.0.0 |
| **Flurl.Http removed from JC.Core** | JC.Core | Moved to JC.Github; JC.Core no longer has HTTP client dependencies |
| **Version bump** | All | JC.Core, JC.Identity, JC.Web → 1.1.0; JC.MySql, JC.SqlServer → 1.1.0; JC.Github → 1.0.0 |

### v1.0.2 (2026-03-05)

| Change | Details |
|--------|---------|
| **Full XML documentation** | All public types, methods, properties, and enum members across all projects |
| **`CountryHelper` logging** | `GetCountries()` now accepts an optional `ILogger?` parameter |
| **`BugReportService` exception type** | Changed from `ArgumentNullException` to `InvalidOperationException` |
| **Removed `GitHelper.NewCommentResponse`** | Unused class removed; `NewIssueResponse` made private |
| **`ColourHelper` and `ConstHelper` made static** | Prevent accidental instantiation |
| **`HtmlTagBuilder` attribute encoding** | Attribute values HTML-encoded via `WebUtility.HtmlEncode` |
| **`SeedDefaultAdminAsync` simplified** | Removed unused `tenantIdConfigKey` parameter |

### v1.0.1 (2026-03-04)

| Change | Details |
|--------|---------|
| **GitHelper registered in DI** | `AddCore<TContext>()` registers `GitHelper` as singleton from config |
| **Standardised on System.Text.Json** | Newtonsoft.Json removed from JC.Identity |
| **Removed `DatabaseProvider` enum** | Unused; provider selection handled by package choice |
| **Added CancellationToken support** | All async repository methods propagate cancellation tokens |
| **Added middleware logging** | Debug/info/warning level logging in both middleware classes |
| **Removed `EnforceTwoFactor` from `IUserInfo`** | 2FA enforcement via global `IdentityMiddlewareOptions` only |
| **`ReportedIssue.Description` marked `required`** | Compile-time null safety |
