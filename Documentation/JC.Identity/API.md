# JC.Identity — API reference

Every public and protected type and member in JC.Identity. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Setup.md), not here. That covers `AddIdentity`, `AddIdentityServices`, `UseIdentity`, `ConfigureAdminAndRolesAsync`, `SeedRolesAsync` and `SeedDefaultAdminAsync`.
>
> The authority-agnostic runtime — `UserInfoBase`, `UserInfoExtensions`, `IdentityRules`, `DefaultClaims` and `IdentityHelper` — belongs to JC.Identity.Shared and is documented in [its API reference](../JC.Identity.Shared/API.md).

## Models

### BaseUser

**Namespace:** `JC.Identity.Models`

The local ASP.NET Identity user entity. Extends `Microsoft.AspNetCore.Identity.IdentityUser` and implements `JC.Core.Models.IApplicationUser`, so the record can be read by code that has no reference to ASP.NET Identity.

Inherited `IdentityUser` members — `Id`, `UserName`, `Email`, `EmailConfirmed`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnabled`, `LockoutEnd`, `AccessFailedCount`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp` and the rest — are not re-documented here; they behave exactly as ASP.NET Identity defines them, and together with the properties below they satisfy `IApplicationUser`.

`BaseUser` does **not** implement `IMultiTenancy`, and must not. A global query filter on the user entity breaks `UserManager` and `SignInManager`, because authentication resolves a user before any tenant scope exists.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `TenantId` | `string?` | `null` | get; set; | The tenant this user belongs to. Persisted, with a maximum length of 36. Carries no query filter. |
| `IdentityTenantId` | `string?` | `null` | get; set; | `IApplicationUser`'s tenant member. Reads and writes `TenantId`, and is `[NotMapped]` — a second way to reach the same column, not a column of its own, so no migration is needed to expose it. |
| `DisplayName` | `string?` | `null` | get; set; | The user's display name. Maximum length 256. |
| `LastLoginUtc` | `DateTime?` | `null` | get; set; | When the user last signed in. Not maintained by this package; the consuming application sets it. |
| `RegistrationUtc` | `DateTime?` | `null` | get; set; | When the user registered. Set by `SeedDefaultAdminAsync` for the accounts it creates. |
| `IsEnabled` | `bool` | `true` | get; set; | Whether the account is enabled. Defaults to `true`, so a newly constructed user is usable without being explicitly enabled. |
| `RequirePasswordChange` | `bool` | `false` | get; set; | Whether the user must change their password before continuing. Projected onto `IUserInfo.RequiresPasswordChange`, which is what the account rules read. |

### BaseRole

**Namespace:** `JC.Identity.Models`

The local ASP.NET Identity role entity. Extends `Microsoft.AspNetCore.Identity.IdentityRole`, adding a description. Inherited members are not re-documented.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Description` | `string?` | `null` | get; set; | An optional description of the role's purpose. Populated from the matching `{Name}Desc` constant when a role is seeded. |

### UserInfo

**Namespace:** `JC.Identity.Models`

The local ASP.NET Identity `IUserInfo`. Extends `UserInfoBase`, whose members are documented in the [JC.Identity.Shared API reference](../JC.Identity.Shared/API.md#userinfobase); this type adds only the constructors that project a `BaseUser`.

Registered as the scoped `IUserInfo` by `AddIdentity` unless a different implementation is supplied.

#### Constructors

##### UserInfo()

Initialises an unpopulated instance for dependency injection to activate. The claims middleware fills it in per request.

Leaves `Authority` at `IdentityAuthority.None`. The projection stamps the authority only once a principal is authenticated, so an anonymous request does not name an authority that never ran.

##### UserInfo(BaseUser user, IEnumerable&lt;string?&gt; roles)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `BaseUser` | — | The user entity to project. |
| `roles` | `IEnumerable<string?>` | — | The user's role names. Null and empty entries are discarded. |

Projects the user through the base constructor, then sets the two members the base deliberately leaves alone: `TenantId` from `BaseUser.TenantId`, because for local Identity the tenant owning the record and the user's application tenant are the same value; and `Authority` to `IdentityAuthority.Local`, which this package can state outright.

##### UserInfo(BaseUser user, IEnumerable&lt;BaseRole&gt; roles)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `BaseUser` | — | The user entity to project. |
| `roles` | `IEnumerable<BaseRole>` | — | The user's role entities. |

Projects each role's `Name` and delegates to the constructor above, so behaviour is identical.

## Services

### DefaultClaimsPrincipalFactory&lt;TUser, TRole&gt;

**Namespace:** `JC.Identity.Authentication`

Claims principal factory extending ASP.NET Identity's `UserClaimsPrincipalFactory<TUser, TRole>` with every `DefaultClaims` value read from the user entity. Registered as `IUserClaimsPrincipalFactory<TUser>` by `AddIdentityServices`, replacing the framework default.

Local sign-in only by construction — it takes `UserManager` and `RoleManager`, so it mints claims rather than receiving them. An external authority supplies its own claims instead.

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TUser` | `BaseUser` | The user entity type. |
| `TRole` | `BaseRole` | The role entity type. |

#### Constructor

##### DefaultClaimsPrincipalFactory(UserManager&lt;TUser&gt; userManager, RoleManager&lt;TRole&gt; roleManager, IOptions&lt;IdentityOptions&gt; options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userManager` | `UserManager<TUser>` | — | Passed to the base factory. |
| `roleManager` | `RoleManager<TRole>` | — | Passed to the base factory. |
| `options` | `IOptions<IdentityOptions>` | — | Passed to the base factory, which reads the configured claim types from it. |

All three are forwarded to the base constructor unchanged.

#### Methods

##### GenerateClaimsAsync(TUser user)

**Returns:** `Task<ClaimsIdentity>`

**Access:** `protected override`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `TUser` | — | The user whose claims are being generated. |

Calls the base implementation, which produces the identifier, username and role claims under the types configured on `IdentityOptions.ClaimsIdentity`, then adds thirteen further claims using the fixed names on `DefaultClaims`: email confirmation, phone number and its confirmation, two-factor state, lockout state, lockout end, access failure count, tenant identifier, display name, last login, registration timestamp, enabled state, and the password-change requirement.

`LockoutEnd`, `LastLoginUtc` and `RegistrationUtc` are written in round-trip (`"O"`) format. A null value on any of the thirteen is written as an empty string rather than omitted, so every claim is always present on the principal.

## Helpers

### SystemRoles

**Namespace:** `JC.Identity.Authentication`

The built-in roles of the local ASP.NET Identity authority, and the reflection helper that discovers them. Designed to be extended by a consuming application — `class AppRoles : SystemRoles` — with role descriptions following the `{RoleName}Desc` naming convention.

Local to this package rather than shared with other identity authorities. An authority with its own administrative plane brings its own role structure, and those roles are a separate security domain that must not be mixed into an application's own authorisation roles.

#### Fields

| Field | Type | Value | Access | Description |
|-------|------|-------|--------|-------------|
| `SystemAdmin` | `const string` | `SystemAdmin` | public | Full system administrator with access to tenant management and assignment. |
| `SystemAdminDesc` | `const string` | Description text | public | The description paired with `SystemAdmin` when seeding. |
| `Admin` | `const string` | `Admin` | public | Administrator with access to all features within their tenant. |
| `AdminDesc` | `const string` | Description text | public | The description paired with `Admin` when seeding. |

#### Methods

##### GetAllRoles&lt;T&gt;()

**Returns:** `List<(string Role, string Description)>`

**Constraint:** `where T : SystemRoles`

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `T` | `SystemRoles` | The roles class to read, including anything it inherits. |

Reflects over `T` for public static fields, flattening the hierarchy so a derived class returns its own roles and the two inherited ones in a single call.

Only literal string fields are considered — a field must be `const`, since `static readonly` is not a literal and is skipped, as is any non-public or non-string field. Fields whose name ends in `Desc` are excluded from the results, because they are descriptions rather than roles.

For each remaining field, the role value is the field's constant value, falling back to the field name where the constant cannot be read. The description is taken from a field named `{FieldName}Desc` on the same type, or an empty string where none exists.

## Data

### IdentityDataDbContext&lt;TUser, TRole&gt;

**Namespace:** `JC.Identity.Data`

Identity-aware data context extending `IdentityDbContext<TUser, TRole, string>` and implementing `JC.Core.Data.IDataDbContext`. Configures the Identity model and the JC.Core audit trail.

Does **not** filter by tenant. A tenant-scoped application derives from this type, implements `JC.Tenancy.Data.ITenantScopedContext` and calls `ApplyTenantFilters` from its own `OnModelCreating` — which is what lets a single-tenant application skip JC.Tenancy entirely.

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TUser` | `BaseUser` | The user entity type. |
| `TRole` | `BaseRole` | The role entity type. |

#### Constructor

##### IdentityDataDbContext(DbContextOptions options, IUserInfo userInfo)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `DbContextOptions` | — | The options configuring the context. Non-generic, so a derived context passes its own `DbContextOptions<TContext>`. |
| `userInfo` | `IUserInfo` | — | The current user, used to attribute audit entries. |

Retains `userInfo`, and reads the application service provider from the options' `CoreOptionsExtension` so the audit service can resolve what it needs without a second injected dependency.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `AuditEntries` | `DbSet<AuditEntry>` | — | get; set; | The audit trail, satisfying `IDataDbContext`. Mapped by JC.Core's audit configuration. |

#### Methods

##### SaveChangesAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<int>`

**Access:** `public override`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Performs a two-phase audit save. It constructs a JC.Core `AuditService` over this context, the application services and the injected `IUserInfo`, and asks it to process the change tracker — which stamps audit fields and returns any create entries that cannot be written until the entities have keys.

The base `SaveChangesAsync` then runs. Where create entries are pending, they are processed and the base save runs a second time.

**Returns the row count from the first save only.** Rows written by the second save — the audit entries for newly created entities — are not included in the figure.

##### OnModelCreating(ModelBuilder modelBuilder)

**Returns:** `void`

**Access:** `protected override`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `modelBuilder` | `ModelBuilder` | — | The model builder being configured. |

Calls the base implementation to configure the Identity model, applies JC.Core's `AuditEntry` mapping, and constrains `TUser.TenantId` to a maximum length of 36.

A derived context overriding this must call `base.OnModelCreating(modelBuilder)` first, or the Identity model and the audit mapping are lost.

## Next steps

- [Setup](Setup.md) — registration, options and their defaults.
- [Guide](Guide.md) — usage, scenarios and nuances.
- [JC.Identity.Shared — API reference](../JC.Identity.Shared/API.md) — the shared runtime this package builds on.
