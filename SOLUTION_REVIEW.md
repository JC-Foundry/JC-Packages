# JC-Packages Solution - Comprehensive Review

> **Generated:** 2026-03-04
> **Updated:** 2026-03-05 (v1.0.2 review)
> **Target Framework:** .NET 9.0 (all projects)
> **Author:** jcraik

---

## Table of Contents

- [Solution Overview](#solution-overview)
- [Dependency Graph](#dependency-graph)
- [Configuration Requirements](#configuration-requirements)
- [JC.Core](#jccore)
- [JC.Identity](#jcidentity)
- [JC.MySql](#jcmysql)
- [JC.SqlServer](#jcsqlserver)
- [JC.Web](#jcweb)
- [Cross-Cutting Concerns](#cross-cutting-concerns)
- [Observations & Recommendations](#observations--recommendations)
- [Changelog](#changelog)

---

## Solution Overview

JC-Packages is a modular NuGet package suite providing reusable infrastructure for .NET 9.0 web applications. The solution is organised into five projects, each with a focused responsibility:

| Project | Purpose | Dependencies | Files |
|---------|---------|--------------|-------|
| **JC.Core** | Repository pattern, auditing, soft-delete, utilities | EF Core 9.0.11, Flurl.Http 4.0.2 | 14 .cs files |
| **JC.Identity** | ASP.NET Core Identity, multi-tenancy, middleware | JC.Core, Identity.EFC 9.0.11 | 12 .cs files |
| **JC.MySql** | MySQL database provider registration | JC.Core, Pomelo.EFC.MySql 9.0.0 | 1 .cs file |
| **JC.SqlServer** | SQL Server database provider registration | JC.Core, EFC.SqlServer 9.0.11 | 1 .cs file |
| **JC.Web** | HTML helpers, dropdowns, QR codes, model state | JC.Core, QRCoder 1.7.0 | 5 .cs files |

All projects are version **1.0.2** and are configured as **packable** NuGet packages.

**Build status:** Zero warnings, zero errors.

---

## Dependency Graph

```
JC.Core (foundation)
  ├── JC.Identity  (depends on JC.Core)
  ├── JC.MySql     (depends on JC.Core)
  ├── JC.SqlServer (depends on JC.Core)
  └── JC.Web       (depends on JC.Core)
```

JC.Core is the foundation package — all other projects depend on it. There are no circular dependencies and no cross-references between the leaf packages (Identity, MySql, SqlServer, Web).

**Note:** Each leaf project includes both a `ProjectReference` (for local development) and a `PackageReference` (for NuGet resolution when the local project isn't available). The `ProjectReference` takes precedence when both are present.

---

## Configuration Requirements

The following configuration keys are required by the packages:

### JC.Core (`AddCore<TContext>()`)

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

**Package:** `JC.Core` v1.0.2
**Description:** Core library providing repository pattern, auditing, soft-delete, and utility helpers for .NET applications.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| Flurl.Http | 4.0.2 |
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
│   ├── EnumExtensions.cs
│   ├── QueryExtensions.cs
│   ├── ServiceCollectionExtensions.cs
│   └── ServiceProviderExtensions.cs
├── Helpers/
│   ├── ColourHelper.cs
│   ├── ConstHelper.cs
│   ├── CountryHelper.cs
│   └── GitHelper.cs
├── Models/
│   ├── IUserInfo.cs
│   ├── ReportedIssue.cs
│   └── Auditing/
│       ├── AuditAction.cs
│       ├── AuditEntry.cs
│       └── AuditModel.cs
└── Services/
    ├── AuditService.cs
    ├── BugReportService.cs
    └── DataRepositories/
        ├── IRepositoryContext.cs
        ├── IRepositoryManager.cs
        ├── RepositoryContext.cs
        └── RepositoryManager.cs
```

### Data Layer

#### `IDataDbContext` (Interface)
Contract for the data context, exposes:
- `DbSet<ReportedIssue> ReportedIssues`
- `DbSet<AuditEntry> AuditEntries`
- `Task<int> SaveChangesAsync(CancellationToken)`

#### `DataDbContext` (Class : DbContext, IDataDbContext)
Default EF Core DbContext implementation. Configures:
- **ReportedIssue:** PK on `Id`, `Description` required
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

#### `ReportedIssue` (Class)
Bug/suggestion tracking entity:

| Property | Type | Notes |
|----------|------|-------|
| `Id` | string | GUID, private set |
| `Type` | IssueType (enum) | `Suggestion` or `Bug` |
| `Description` | required string | Required (enforced at compile time and DB level) |
| `Image` | byte[]? | Optional screenshot |
| `ReportSent` | bool | Sent to GitHub |
| `ExternalId` | int? | GitHub issue number |
| `Closed` | bool | Resolution status |
| `Created` | DateTime | Creation timestamp |
| `UserId` | string? | Reporter ID |
| `UserDisplay` | string? | Reporter display name |

#### `AuditAction` (Enum)
`Create` (0), `Update` (1), `Delete` (2), `Restore` (3)

#### `AuditEntry` (Class)
Audit trail record:

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

### Extensions

#### `EnumExtensions` (Static)

| Method | Description |
|--------|-------------|
| `GetAllOptions<T>()` | Returns `List<(string Name, int Value)>` for all enum members |
| `ToDisplayName()` | Converts PascalCase/underscore enum names to human-readable display strings (e.g. `MyValue` → `"My value"`, `Some_Thing` → `"Some thing"`) |
| `GetDescription()` | Returns `[Description]` attribute value or falls back to `ToDisplayName()` |
| `TryParse<T>(string?, T)` | Case-insensitive parse with default fallback |

#### `QueryExtensions` (Static)

| Method | Description |
|--------|-------------|
| `FilterDeleted<T>(IQueryable<T>, DeletedQueryType)` | Filters AuditModel entities by soft-delete status |

#### `ServiceCollectionExtensions` (Static)

| Method | Description |
|--------|-------------|
| `AddCore<TContext>(IConfiguration)` | Registers GitHelper (singleton from config), BugReportService, AuditService, IDataDbContext, DbContext, IRepositoryManager, and repository contexts for ReportedIssue and AuditModel |
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
| `ExtractRGB(string)` | Private — parses hex `#RRGGBB` to (R, G, B) tuple |

#### `ConstHelper` (Static)

| Method | Description |
|--------|-------------|
| `GetAllConsts<T>()` | Returns `Dictionary<string, object>` of all `const` fields in type T via reflection |

#### `CountryHelper` (Static)

| Method | Description |
|--------|-------------|
| `GetCountries()` | Returns `IReadOnlyList<Country>` from .NET CultureInfo data (cached, deduplicated, sorted) |
| `GetCountriesDictionary()` | Returns `Dictionary<string, string>` (Code → Name) |
| `GetCountryName(string code)` | Looks up country name by ISO 3166-1 alpha-2 code |
| `GetCountryCode(string name)` | Looks up country code by name |

**Nested:** `record Country(string Code, string Name)`

#### `GitHelper` (Class)
GitHub API integration using Flurl.Http. Registered as a **singleton** in `AddCore()` from `Github:Url` and `Github:ApiKey` configuration values.

| Method | Description |
|--------|-------------|
| Constructor | Configures FlurlClient with Bearer token, `X-GitHub-Api-Version: 2022-11-28` header, `User-Agent: JC.Core` |
| `RecordIssue(owner, repo, title, desc)` | Creates a GitHub issue, returns issue number |

**Nested response classes:** `NewCommentResponse`, `NewIssueResponse`

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

#### `BugReportService`
**Dependencies:** `IConfiguration`, `IDataDbContext`, `GitHelper`, `ILogger<BugReportService>`

| Method | Description |
|--------|-------------|
| `RecordIssue(description, issueType, creatorId?, creatorName?)` | Creates ReportedIssue entity, attempts to create GitHub issue (graceful failure with logging), saves to DB |

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

## JC.Identity

**Package:** `JC.Identity` v1.0.2
**Description:** Identity library providing ASP.NET Core Identity integration, multi-tenancy, middleware, and user management helpers.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.0.2 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.11 |
| Microsoft.AspNetCore.App | Framework Reference |

**Note:** All JSON serialisation now uses `System.Text.Json` (the Newtonsoft.Json dependency has been removed).

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

**DbSets:** `ReportedIssues`, `AuditEntries`, `Tenants`

**OnModelCreating** configures:
- ReportedIssue (PK, required Description)
- AuditEntry (PK, required Action/AuditDate, indexes on UserId, TableName, AuditDate)
- Tenant (PK, required Name, index on Domain)
- Calls `ApplyTenantQueryFilters()` for automatic multi-tenancy filtering

### Models

#### `BaseUser` (: IdentityUser, IMultiTenancy)
Extends ASP.NET Core IdentityUser with:

| Property | Type | Default |
|----------|------|---------|
| `TenantId` | string? | null |
| `Tenant` | Tenant? | null (navigation) |
| `DisplayName` | string? | null |
| `LastLoginUtc` | DateTime? | null |
| `IsEnabled` | bool | false |
| `RequirePasswordChange` | bool | false |

#### `BaseRole` (: IdentityRole)
Extends IdentityRole with:
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

**Note:** 2FA enforcement (`EnforceTwoFactor`) is a **global** option in `IdentityMiddlewareOptions`. When enabled, all users without 2FA configured are redirected. There is no per-user 2FA enforcement — this is by design for v1.0.2.

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

**Package:** `JC.MySql` v1.0.2
**Description:** MySQL database provider extensions for JC.Core using Pomelo.EntityFrameworkCore.MySql.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.0.2 |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.11 |

### Project Structure

```
JC.MySql/
└── Extension.cs
```

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

**Behaviour:**
- Retrieves connection string from `IConfiguration.GetConnectionString()`
- Throws `InvalidOperationException` if connection string not found
- Auto-detects MySQL server version via `ServerVersion.AutoDetect()`
- Configures migrations assembly
- Invokes optional custom MySQL options callback

---

## JC.SqlServer

**Package:** `JC.SqlServer` v1.0.2
**Description:** SQL Server database provider extensions for JC.Core using Microsoft.EntityFrameworkCore.SqlServer.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.0.2 |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.11 |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.11 |

### Project Structure

```
JC.SqlServer/
└── Extension.cs
```

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

**Behaviour:** Identical pattern to JC.MySql — retrieves connection string, validates, registers DbContext with SQL Server provider.

---

## JC.Web

**Package:** `JC.Web` v1.0.2
**Description:** Web helpers for ASP.NET Core including dropdown builders, HTML tag builder, model state wrapper, and QR code generation.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.0.2 |
| QRCoder | 1.7.0 |
| Microsoft.AspNetCore.App | Framework Reference |

### Project Structure

```
JC.Web/
├── Helpers/
│   ├── DropdownHelper.cs
│   ├── ModelStateWrapper.cs
│   └── HTML/
│       ├── HtmlHelper.cs
│       └── HtmlTagBuilder.cs
└── QrCodeHelper.cs
```

### `DropdownHelper` (Static)

| Method | Description |
|--------|-------------|
| `ToDropdownEntry(text, value, selected)` | Creates a single `SelectListItem` |
| `FromEnum<T>(selected?)` | Converts all enum values to dropdown items using `ToDisplayName()` |
| `FromCollection<T>(items, textSelector, valueSelector, selectedPredicate?)` | Generic collection → dropdown with custom selectors |
| `FromDictionary(items, selected?)` | Dictionary (Key=value, Value=text) → dropdown |
| `GetCountryDropdown(selected?)` | Pre-built country dropdown from `CountryHelper` |
| `WithPlaceholder(items, text, value)` | Extension: inserts placeholder item at index 0 (default: "Please select...") |

### `HtmlTagBuilder` (Class)

Fluent HTML tag builder with internal constructor:

| Method | Description |
|--------|-------------|
| `AddClass(className)` | Adds CSS class (ignores empty) |
| `AddAttribute(name, value)` | Adds/overwrites HTML attribute |
| `AddActiveAttribute()` | Adds "active" CSS class |
| `AddCurrentPageAttribute()` | Adds `aria-current="page"` |
| `AddDisabledClass()` | Adds "disabled" CSS class |
| `SetContent(content)` | Sets inner HTML content |
| `Build()` | Returns final HTML string |
| `implicit operator string` | Auto-converts to string via `Build()` |

Supports self-closing tags (e.g., `<input />`, `<br />`).

### `HtmlHelper` (Static)

| Method | Description |
|--------|-------------|
| `CreateElement(tag, content, isActive, isDisabled, attributes, classes)` | Generic element builder |
| `PaginationItem(content, isActive, isDisabled)` | `<li class="page-item">` with active/disabled states |
| `PaginationLink(text, href, buttonClass, isActive)` | `<a class="page-link">` with aria-current support |

### `ModelStateWrapper` (Class)

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

### `QrCodeHelper` (Class)

| Constructor | Description |
|-------------|-------------|
| `QrCodeHelper()` | SVG format, 10px/module, ECC level M |
| `QrCodeHelper(format, pixelsPerModule, eccLevel)` | Custom configuration (clamps pixelsPerModule to minimum 10 if ≤ 0) |

| Method | Description |
|--------|-------------|
| `GenerateQrCode(content)` | Returns SVG string or base64 PNG data URI. Throws `ArgumentException` for empty content. |

**Enum `QrCodeFormat`:** `Svg` (0), `Base64` (1)

**Constant:** `Base64ImgPrefix = "data:image/png;base64,"`

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
| **Builder** | `HtmlTagBuilder` fluent API |
| **Options Pattern** | `IdentityMiddlewareOptions` via `IOptions<T>` |

### Dependency Injection Registration

A consuming application would typically wire up:

```csharp
// Program.cs
services.AddCore<MyDbContext>(configuration);                // JC.Core (requires IConfiguration)
services.AddIdentity<MyUser, MyRole>();                      // JC.Identity
services.AddMySqlDatabase<MyDbContext>(config, "MyApp");     // JC.MySql (or JC.SqlServer)

app.UseIdentity();                                           // JC.Identity middleware
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

1. **Clean separation of concerns** — Each project has a focused responsibility with no circular dependencies
2. **Consistent patterns** — Repository pattern, DI registration, and extension methods follow a uniform style throughout
3. **Comprehensive auditing** — Full CRUD audit trail with user tracking, timestamps, and JSON data serialisation
4. **Flexible multi-tenancy** — Global query filters with admin bypass and tenant settings system
5. **Well-designed soft delete** — Private setters on AuditModel ensure state consistency; mutual clearing of delete/restore fields prevents stale data
6. **Thread-safe repository caching** — `ConcurrentDictionary` in `RepositoryManager`
7. **Packable architecture** — All projects configured as NuGet packages for reuse across applications
8. **Consistent serialisation** — Entire solution standardised on `System.Text.Json`
9. **CancellationToken support** — All repository async operations propagate cancellation tokens
10. **Middleware logging** — Both middleware classes log meaningful events at appropriate levels (debug for routine, info for redirects, warning for disabled users)
11. **DI-friendly design** — `TryAddScoped` throughout prevents double-registration conflicts when consuming apps customise services
12. **Clean build** — Zero warnings, zero errors

### Remaining Considerations

1. ~~**`CountryHelper` silent exception handling**~~ — **Resolved.** Broad catch retained (to prevent app crashes from unexpected failures) but now logs a warning with the culture name and exception via an optional `ILogger?` parameter on `GetCountries()`.

2. ~~**`AuditService` lacks `CancellationToken`**~~ — **Dismissed.** `CancellationToken` is valuable at boundaries (HTTP, background tasks, external APIs) and for potentially long-running operations. `AuditService` is a focused internal service writing a single row — the operation is too quick for cancellation to add practical value.

3. ~~**`BugReportService` exception type**~~ — **Resolved.** Changed from `ArgumentNullException` to `InvalidOperationException` with descriptive messages, matching the pattern used in `AddCore()` and `SeedDefaultAdminAsync()`.

4. ~~**`GitHelper.NewCommentResponse`**~~ — **Resolved.** Removed unused class. `NewIssueResponse` also made private since it's only used internally.

5. ~~**`ColourHelper` and `ConstHelper` are non-static classes with only static methods**~~ — **Resolved.** Both classes marked `static`.

6. ~~**`IUserInfo` has mutable setters on all properties**~~ — **Acknowledged.** Setters must remain on `IUserInfo` because `UserInfoMiddleware` writes through the interface. XML documentation documents all properties as "Gets" to signal read-only intent to consumers.

7. ~~**No XML documentation on public APIs**~~ — **Resolved.** Full XML documentation with `<summary>`, `<param>`, `<typeparam>`, `<returns>`, and `<exception>` tags added to all public types, methods, properties, and enum members across all five projects.

8. ~~**`HtmlTagBuilder` doesn't HTML-encode attribute values**~~ — **Resolved.** Attribute values are now encoded via `System.Net.WebUtility.HtmlEncode` in the `Build()` method.

9. ~~**`SeedDefaultAdminAsync` reads `tenantIdConfigKey` but ignores its value**~~ — **Resolved.** Removed the `tenantIdConfigKey` parameter entirely. When `setupTenancy` is true, the method now finds an existing "Default Tenant" or creates one if it doesn't exist.

### Reviewed & Dismissed

1. **No test projects** — Acknowledged. Testing is not a priority for this package suite at this time.

2. **`ChangePasswordRoute` default** — The route `/Identity/Account/Manage/SetPassword` is intentionally correct. After logging in with their current password, users are redirected to the "set password" page — requiring them to re-enter their current password would be poor UX.

---

## Changelog

### v1.0.2 (2026-03-05)

| Change | Details |
|--------|---------|
| **Full XML documentation** | All public types, methods, properties, and enum members across all five projects now have XML documentation with `<summary>`, `<param>`, `<returns>`, and `<exception>` tags |
| **`CountryHelper` logging** | `GetCountries()` now accepts an optional `ILogger?` parameter; exceptions during `RegionInfo` creation are logged as warnings instead of silently swallowed |
| **`BugReportService` exception type** | Changed from `ArgumentNullException` to `InvalidOperationException` with descriptive messages for missing config values, matching the pattern used throughout the solution |
| **Removed `GitHelper.NewCommentResponse`** | Unused response class removed; `NewIssueResponse` made private |
| **`ColourHelper` and `ConstHelper` made static** | Both classes now marked `static` to prevent accidental instantiation |
| **`HtmlTagBuilder` attribute encoding** | Attribute values are now HTML-encoded via `System.Net.WebUtility.HtmlEncode` to prevent malformed HTML and XSS |
| **`SeedDefaultAdminAsync` simplified** | Removed unused `tenantIdConfigKey` parameter; when `setupTenancy` is true, the method now finds an existing "Default Tenant" or creates one automatically |
| **Version bump** | All packages updated to v1.0.2; all JC.Core package references updated to require 1.0.2 |

### v1.0.1 (2026-03-04)

| Change | Details |
|--------|---------|
| **GitHelper registered in DI** | `AddCore<TContext>()` now requires `IConfiguration` and registers `GitHelper` as a singleton from `Github:Url` and `Github:ApiKey` config values |
| **Standardised on System.Text.Json** | `Tenant` settings serialisation migrated from Newtonsoft.Json to System.Text.Json; Newtonsoft.Json package reference removed from JC.Identity |
| **Removed `DatabaseProvider` enum** | Unused enum (`SqlServer`, `MySql`) deleted from JC.Core — provider selection is handled by which package (JC.MySql or JC.SqlServer) is referenced |
| **Added CancellationToken support** | All async methods on `IRepositoryContext<T>` and `RepositoryContext<T>` now accept and propagate `CancellationToken` (defaults to `default` for backwards compatibility) |
| **Added middleware logging** | `UserInfoMiddleware` logs debug-level messages for user population; `IdentityMiddleware` logs warnings for disabled users and info for password change / 2FA redirects |
| **Removed `EnforceTwoFactor` from `IUserInfo`** | Property was never populated from claims or written by the claims factory — 2FA enforcement is handled exclusively via the global `IdentityMiddlewareOptions.EnforceTwoFactor` setting |
| **Removed `HasMaxLength(-1)` from AuditEntry.ActionData** | SQL Server-specific convention that could produce unexpected behaviour with MySQL/Pomelo; `string?` properties default to unbounded text in both providers |
| **Simplified `ColourHelper.HoverColour`** | Removed redundant `Math.Min/Max` clamping — the lightening formula mathematically cannot produce values outside the 0–255 range for valid hex inputs |
| **`ReportedIssue.Description` marked `required`** | Enforces non-null at compile time, eliminating CS8618 warning |
| **Version bump** | All packages updated to v1.0.1; all JC.Core package references updated to require 1.0.1 |
