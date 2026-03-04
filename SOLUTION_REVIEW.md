# JC-Packages Solution - Comprehensive Review

> **Generated:** 2026-03-04
> **Target Framework:** .NET 9.0 (all projects)
> **Author:** jcraik

---

## Table of Contents

- [Solution Overview](#solution-overview)
- [Dependency Graph](#dependency-graph)
- [JC.Core](#jccore)
- [JC.Identity](#jcidentity)
- [JC.MySql](#jcmysql)
- [JC.SqlServer](#jcsqlserver)
- [JC.Web](#jcweb)
- [Cross-Cutting Concerns](#cross-cutting-concerns)
- [Observations & Recommendations](#observations--recommendations)

---

## Solution Overview

JC-Packages is a modular NuGet package suite providing reusable infrastructure for .NET 9.0 web applications. The solution is organised into five projects, each with a focused responsibility:

| Project | Purpose | Dependencies | Files |
|---------|---------|--------------|-------|
| **JC.Core** | Repository pattern, auditing, soft-delete, utilities | EF Core 9.0.11, Flurl.Http 4.0.2 | 15 .cs files |
| **JC.Identity** | ASP.NET Core Identity, multi-tenancy, middleware | JC.Core, Identity.EFC 9.0.11, Newtonsoft.Json 13.0.4 | 12 .cs files |
| **JC.MySql** | MySQL database provider registration | JC.Core, Pomelo.EFC.MySql 9.0.0 | 1 .cs file |
| **JC.SqlServer** | SQL Server database provider registration | JC.Core, EFC.SqlServer 9.0.11 | 1 .cs file |
| **JC.Web** | HTML helpers, dropdowns, QR codes, model state | JC.Core, QRCoder 1.7.0 | 5 .cs files |

All projects are version **1.0.0** and are configured as **packable** NuGet packages.

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

---

## JC.Core

**Package:** `JC.Core` v1.0.0
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
│   ├── DatabaseProvider.cs
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

#### `DatabaseProvider` (Enum)
Supported database providers:
- `SqlServer`
- `MySql`

#### `IDataDbContext` (Interface)
Contract for the data context, exposes:
- `DbSet<ReportedIssue> ReportedIssues`
- `DbSet<AuditEntry> AuditEntries`
- `Task<int> SaveChangesAsync(CancellationToken)`

#### `DataDbContext` (Class : DbContext, IDataDbContext)
Default EF Core DbContext implementation. Configures:
- **ReportedIssue:** PK on `Id`, `Description` required
- **AuditEntry:** PK on `Id`, `Action` required, `ActionData` unlimited length, `AuditDate` required, indexes on `UserId`, `TableName`, `AuditDate`

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
- **Profile:** `DisplayName`, `LastLoginUtc`, `IsEnabled`, `RequiresPasswordChange`, `EnforceTwoFactor`
- **System:** `IsSetup`
- **Authorization:** `Roles` (IReadOnlyList\<string>), `Claims` (IReadOnlyList\<Claim>)
- **Method:** `bool IsInRole(string role)`

#### `ReportedIssue` (Class)
Bug/suggestion tracking entity:

| Property | Type | Notes |
|----------|------|-------|
| `Id` | string | GUID, private set |
| `Type` | IssueType (enum) | `Suggestion` or `Bug` |
| `Description` | string | Required |
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
| `ActionData` | string? | JSON-serialized entity data |

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

All setters are **private** — state is only changed through the `Fill*` methods, ensuring consistency.

### Extensions

#### `EnumExtensions` (Static)

| Method | Description |
|--------|-------------|
| `GetAllOptions<T>()` | Returns `List<(string Name, int Value)>` for all enum members |
| `ToDisplayName()` | Converts PascalCase/underscore enum names to human-readable display strings |
| `GetDescription()` | Returns `[Description]` attribute value or falls back to `ToDisplayName()` |
| `TryParse<T>(string?, T)` | Case-insensitive parse with default fallback |

#### `QueryExtensions` (Static)

| Method | Description |
|--------|-------------|
| `FilterDeleted<T>(IQueryable<T>, DeletedQueryType)` | Filters AuditModel entities by soft-delete status |

#### `ServiceCollectionExtensions` (Static)

| Method | Description |
|--------|-------------|
| `AddCore<TContext>()` | Registers all core services (BugReportService, AuditService, IDataDbContext, DbContext, IRepositoryManager, and repository contexts for ReportedIssue and AuditModel) |
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
| `ExtractRGB(string)` | Private - parses hex `#RRGGBB` to (R, G, B) tuple |

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
GitHub API integration using Flurl.Http:

| Method | Description |
|--------|-------------|
| Constructor | Configures FlurlClient with Bearer token, API version header, User-Agent |
| `RecordIssue(owner, repo, title, desc)` | Creates a GitHub issue, returns issue number |

**Nested response classes:** `NewCommentResponse`, `NewIssueResponse`

### Services

#### `AuditService`
**Dependencies:** `IDataDbContext`, `IUserInfo`

| Method | Description |
|--------|-------------|
| `LogAsync(AuditAction, tableName, data?)` | Creates AuditEntry with user info, serializes data as JSON |
| `LogCreateAsync(tableName, data?)` | Convenience for `AuditAction.Create` |
| `LogUpdateAsync(tableName, data?)` | Convenience for `AuditAction.Update` |
| `LogDeleteAsync(tableName, data?)` | Convenience for `AuditAction.Delete` |
| `LogRestoreAsync(tableName, data?)` | Convenience for `AuditAction.Restore` |

#### `BugReportService`
**Dependencies:** `IConfiguration`, `IDataDbContext`, `GitHelper`, `ILogger<BugReportService>`

| Method | Description |
|--------|-------------|
| `RecordIssue(description, issueType, creatorId?, creatorName?)` | Creates ReportedIssue entity, attempts to create GitHub issue (graceful failure), saves to DB |

### Repository Pattern

#### `IRepositoryContext<T>` (Interface)
Full generic CRUD interface with **23 methods**:

| Category | Methods |
|----------|---------|
| **Query** | `AsQueryable()`, `GetAll(predicate)`, `GetAll(predicate, orderBy)`, `GetAllAsync(predicate)`, `GetAllAsync(predicate, orderBy)`, `GetByIdAsync(int)`, `GetByIdAsync(string)`, `GetByIdAsync(params object[])` |
| **Create** | `AddAsync(entity)`, `AddAsync(IEnumerable)`, `AddRangeAsync(IEnumerable)` |
| **Update** | `UpdateAsync(entity)`, `UpdateAsync(IEnumerable)`, `UpdateRangeAsync(IEnumerable)` |
| **Soft Delete** | `SoftDeleteAsync(entity)`, `SoftDeleteAsync(IEnumerable)`, `SoftDeleteRangeAsync(IEnumerable)` |
| **Restore** | `RestoreAsync(entity)`, `RestoreAsync(IEnumerable)`, `RestoreRangeAsync(IEnumerable)` |
| **Hard Delete** | `DeleteAsync(entity)`, `DeleteAsync(IEnumerable)`, `DeleteRangeAsync(IEnumerable)` |

All write operations accept optional `userId` (defaults to current user) and `saveNow` (defaults to `true`).

#### `RepositoryContext<T>` (Implementation)
- Auto-detects if `T` extends `AuditModel` (uses `FillCreated`, `FillModified`, etc.)
- Falls back to reflection-based `IsDeleted` property detection for non-AuditModel entities
- All operations log errors and re-throw

#### `IRepositoryManager` / `RepositoryManager`
- **Repository cache:** `ConcurrentDictionary<Type, object>` for thread-safe lazy initialisation
- **Transaction management:** `BeginTransactionAsync()`, `CommitTransactionAsync()`, `RollbackTransactionAsync()`
- **Implements:** `IDisposable`, `IAsyncDisposable`

---

## JC.Identity

**Package:** `JC.Identity` v1.0.0
**Description:** Identity library providing ASP.NET Core Identity integration, multi-tenancy, middleware, and user management helpers.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.0.0 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.11 |
| Newtonsoft.Json | 13.0.4 |
| Microsoft.AspNetCore.App | Framework Reference |

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
Extends `UserClaimsPrincipalFactory<TUser, TRole>`. Overrides `GenerateClaimsAsync` to add all 12 custom claims from the user entity to the `ClaimsIdentity`.

#### `SystemRoles` (Static)
Built-in roles:
- **SystemAdmin** — Full system administrator with tenant management access
- **Admin** — Administrator with access to all features within their tenant

**Method:** `GetAllRoles<T>()` — Uses reflection to discover all `const string` role fields and their matching `*Desc` description fields from the type hierarchy. Designed to be extended by consuming applications.

### Data

#### `IdentityDataDbContext<TUser, TRole>`
Extends `IdentityDbContext<TUser, TRole, string>` and implements `IDataDbContext`.

**DbSets:** `ReportedIssues`, `AuditEntries`, `Tenants`

**OnModelCreating** configures:
- ReportedIssue (PK, required Description)
- AuditEntry (PK, required Action/AuditDate, unlimited ActionData, indexes)
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
| `Name` | string | Required |
| `Description` | string? | Optional |
| `Domain` | string? | Indexed |
| `MaxUsers` | uint? | User limit |
| `ExpiryDateUtc` | DateTime? | Tenant expiry |
| `Settings` | string | JSON, default `"[]"` |

**Methods:**
- `SetSettings(IEnumerable<TenantSettings>)` — Serializes settings to JSON
- `SetSetting(key, value, isActive)` — Add/update single setting
- `GetSettings()` — Deserializes JSON to `List<TenantSettings>`

#### `TenantSettings` (sealed)
Simple KVP: `Key` (string?), `Value` (string?), `IsActive` (bool)

### Middleware

#### `UserInfoMiddleware`
Populates `IUserInfo` from `ClaimsPrincipal` on first request per scope:
- Sets system user constants for unauthenticated requests
- Extracts all 12 custom claims + roles for authenticated requests
- Sets `IsSetup = true` to avoid re-processing

#### `IdentityMiddleware`
**Options:** `IdentityMiddlewareOptions`

Pipeline enforcement:
1. Skips static files (.css, .js, images, fonts, .map, .json, .xml)
2. Skips unauthenticated requests
3. Skips excluded paths (AccessDenied, Logout, Error routes)
4. **Checks `IsEnabled`** → redirects to AccessDenied if disabled
5. **Checks `RequiresPasswordChange`** (if enabled) → redirects to ChangePassword route
6. **Checks 2FA enforcement** (if enabled) → redirects to TwoFactor setup route

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
| `UseIdentity()` | Full pipeline: Authentication → UserInfo → Authorization → IdentityMiddleware |
| `ConfigureAdminAndRolesAsync<TUser, TRoles, TRole>()` | Seeds roles + default admin in one call |
| `SeedRolesAsync<TRoles, TRole>()` | Creates missing roles from `SystemRoles.GetAllRoles<TRoles>()` |
| `SeedDefaultAdminAsync<TUser, TRole>()` | Creates admin user from config (`Admin:Username`, `Admin:Email`, `Admin:Password`, etc.), optionally creates default tenant |

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

**Package:** `JC.MySql` v1.0.0
**Description:** MySQL database provider extensions for JC.Core using Pomelo.EntityFrameworkCore.MySql.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.0.0 |
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

**Package:** `JC.SqlServer` v1.0.0
**Description:** SQL Server database provider extensions for JC.Core using Microsoft.EntityFrameworkCore.SqlServer.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.0.0 |
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

**Package:** `JC.Web` v1.0.0
**Description:** Web helpers for ASP.NET Core including dropdown builders, HTML tag builder, model state wrapper, and QR code generation.

### NuGet Dependencies

| Package | Version |
|---------|---------|
| JC.Core | 1.0.0 |
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

Wraps `ModelStateDictionary` with automatic key prefixing:

| Constructor Param | Purpose |
|-------------------|---------|
| `modelState` | The ModelStateDictionary to wrap |
| `prefix` | Key prefix (default: `"Input."`) |
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
| `QrCodeHelper(format, pixelsPerModule, eccLevel)` | Custom configuration |

| Method | Description |
|--------|-------------|
| `GenerateQrCode(content)` | Returns SVG string or base64 PNG data URI |

**Enum `QrCodeFormat`:** `Svg` (0), `Base64` (1)

**Constant:** `Base64ImgPrefix = "data:image/png;base64,"`

---

## Cross-Cutting Concerns

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
services.AddCore<MyDbContext>();                           // JC.Core
services.AddIdentity<MyUser, MyRole>();                    // JC.Identity
services.AddMySqlDatabase<MyDbContext>(config, "MyApp");   // JC.MySql (or JC.SqlServer)

app.UseIdentity();                                         // JC.Identity middleware
await app.ConfigureAdminAndRolesAsync<MyUser, MyRoles, MyRole>();
await app.Services.MigrateDatabaseAsync<MyDbContext>();
```

### Multi-Tenancy Flow

1. Entities implement `IMultiTenancy` with `TenantId` property
2. `IdentityDataDbContext.OnModelCreating()` calls `ApplyTenantQueryFilters()`
3. Global query filter auto-scopes all queries to current user's `TenantId`
4. `SystemAdmin` role users can bypass via `AllTenants()` extension
5. `SeedDefaultAdminAsync()` can optionally create a default tenant

---

## Observations & Recommendations

### Strengths

1. **Clean separation of concerns** — Each project has a focused responsibility with no circular dependencies
2. **Consistent patterns** — Repository pattern, DI registration, and extension methods follow a uniform style
3. **Comprehensive auditing** — Full CRUD audit trail with user tracking, timestamps, and JSON data serialization
4. **Flexible multi-tenancy** — Global query filters with admin bypass and tenant settings system
5. **Well-designed soft delete** — Private setters on AuditModel ensure state consistency
6. **Thread-safe repository caching** — `ConcurrentDictionary` in `RepositoryManager`
7. **Packable architecture** — All projects configured as NuGet packages for reuse across applications

### Potential Issues

1. **Typo in `IdentityMiddlewareOptions`** — Line 15 references `LogoutRolute` instead of `LogoutRoute` in the `ExcludedPaths` property (verify this is corrected)

2. **Mixed JSON serializers** — `JC.Core` uses `System.Text.Json` (via `JsonSerializer.Serialize` in AuditService) while `JC.Identity` uses `Newtonsoft.Json` (for Tenant settings). Consider standardising on one serializer to reduce dependency surface

3. **No test projects** — The solution has no unit or integration test projects. Given the complexity of the repository pattern, multi-tenancy filters, and middleware pipeline, automated tests would significantly increase confidence

4. **`BugReportService` constructor** — Requires `GitHelper` to be registered in DI, but `GitHelper` is a plain class needing manual URL/API key construction. There's no visible registration of `GitHelper` in `ServiceCollectionExtensions.AddCore()` — consuming apps must register it themselves

5. **Database provider enum unused** — `DatabaseProvider` enum (`SqlServer`, `MySql`) is defined but doesn't appear to be referenced anywhere in the codebase. The actual provider selection is handled by which package (JC.MySql or JC.SqlServer) is used

6. **`ChangePasswordRoute` default** — Points to `/Identity/Account/Manage/SetPassword` which is the "set password" page (for external login users without a password), not "change password". Verify this is the intended route

7. **`CountryHelper` error handling** — Silently catches all exceptions during `RegionInfo` creation. While this prevents failures from invalid cultures, it could mask unexpected issues

8. **No `ILogger` in middleware** — Both `UserInfoMiddleware` and `IdentityMiddleware` lack logging. Failed claim parsing or unexpected states would be invisible

### Suggestions

1. **Add a test project** (`JC.Core.Tests`, etc.) with unit tests for the repository, audit, and multi-tenancy logic
2. **Consider standardising on `System.Text.Json`** to eliminate the Newtonsoft.Json dependency in JC.Identity
3. **Add logging to middleware** for debugging authentication/authorisation issues in consuming applications
4. **Register `GitHelper` in `AddCore()`** or document that consuming apps must register it
5. **Consider removing or utilising `DatabaseProvider` enum** — it currently serves no purpose
6. **Add XML documentation** to public APIs to improve IntelliSense for consumers of the NuGet packages
