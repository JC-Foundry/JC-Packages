# JC.Core — Guide

Covers repository pattern usage, soft-delete and restore, pagination, audit trail behaviour, transactions, and utility helpers. See [Setup](Setup.md) for registration.

## Repository pattern

### Basic CRUD

Inject `IRepositoryContext<T>` for typed repository access:

```csharp
public class ProductService(IRepositoryContext<Product> products)
{
    public async Task<Product> CreateAsync(string name, decimal price)
    {
        var product = new Product { Name = name, Price = price };
        return await products.AddAsync(product);
    }

    public async Task<Product?> GetAsync(int id)
    {
        return await products.GetByIdAsync(id);
    }

    public async Task<List<Product>> GetAllActiveAsync()
    {
        return await products.GetAllAsync(p => !p.IsDeleted);
    }

    public async Task UpdateAsync(Product product)
    {
        await products.UpdateAsync(product);
    }
}
```

Every `AddAsync` call automatically populates `CreatedById` and `CreatedUtc` on entities extending `AuditModel`. Every `UpdateAsync` populates `LastModifiedById` and `LastModifiedUtc`. The user ID comes from `IUserInfo.UserId` — if JC.Identity is registered, this is the authenticated user; otherwise it falls back to `IUserInfo.MissingUserInfoId` (`"<NONE>"`).

**Nuance:** The base `DataDbContext` passes `null` for `IUserInfo` to the audit service, so audit fields will always record `"<NONE>"` as the user. If you need real user tracking, use JC.Identity's `IdentityDataDbContext` which injects the authenticated user's details.

### Querying

`GetAll` returns an `IQueryable<T>` — the query is not executed until you materialise it:

```csharp
// Filtered query — returns IQueryable<T>, not yet executed
var expensiveProducts = products.GetAll(p => p.Price > 100);

// Filtered and ordered — still deferred
var sorted = products.GetAll(
    p => p.Price > 100,
    q => q.OrderByDescending(p => p.Price)
);

// Materialise with ToListAsync
var result = await sorted.ToListAsync();
```

`GetAllAsync` materialises immediately and returns `List<T>`:

```csharp
// Executes the query and returns a list
var activeProducts = await products.GetAllAsync(p => !p.IsDeleted);

// With ordering
var orderedProducts = await products.GetAllAsync(
    p => p.CategoryId == categoryId,
    q => q.OrderBy(p => p.Name)
);
```

Use `GetAll` when you want to compose further (e.g. pagination, projection). Use `GetAllAsync` when you want results straight away.

### Looking up by ID

```csharp
// Integer primary key
var product = await products.GetByIdAsync(42);

// String primary key (e.g. GUID-based entities)
var tenant = await tenants.GetByIdAsync("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

// Composite key
var orderItem = await orderItems.GetByIdAsync(orderId, productId);
```

All `GetByIdAsync` overloads use `DbSet<T>.FindAsync` internally, which checks the change tracker first before querying the database. If the entity is already tracked, no database query is made.

### AsQueryable

For full query control, use `AsQueryable()` to get the underlying `IQueryable<T>`:

```csharp
var topProducts = await products.AsQueryable()
    .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
    .OrderByDescending(p => p.SalesCount)
    .Take(10)
    .ToListAsync();
```

### Batching with saveNow

By default, every repository method calls `SaveChangesAsync` immediately. Pass `saveNow: false` to batch multiple operations into a single round-trip:

```csharp
public class OrderService(IRepositoryManager repositoryManager)
{
    public async Task PlaceOrderAsync(OrderDto dto)
    {
        var orders = repositoryManager.GetRepository<Order>();
        var orderItems = repositoryManager.GetRepository<OrderItem>();
        var stockLevels = repositoryManager.GetRepository<StockLevel>();

        await repositoryManager.BeginTransactionAsync();
        try
        {
            var order = new Order { CustomerId = dto.CustomerId, Status = OrderStatus.Pending };
            await orders.AddAsync(order, saveNow: false);

            foreach (var line in dto.Items)
            {
                await orderItems.AddAsync(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity
                }, saveNow: false);

                var stock = await stockLevels.GetByIdAsync(line.ProductId);
                stock!.Available -= line.Quantity;
                await stockLevels.UpdateAsync(stock, saveNow: false);
            }

            await repositoryManager.SaveChangesAsync();
            await repositoryManager.CommitTransactionAsync();
        }
        catch
        {
            await repositoryManager.RollbackTransactionAsync();
            throw;
        }
    }
}
```

This is the primary use case for `saveNow: false` — coordinating writes across multiple entity types in a single atomic operation. For batching a single entity type, prefer the range overloads (`AddRangeAsync`, `UpdateRangeAsync`, etc.) which handle this naturally.

**Nuance:** Audit fields (`CreatedById`, `CreatedUtc`, etc.) are populated at the point you call `AddAsync` or `UpdateAsync`, not when `SaveChangesAsync` runs. If there's a significant delay between the two, the audit timestamps will reflect when the repository method was called.

### Range operations

For adding, updating, or deleting multiple entities at once, use the range overloads:

```csharp
// Add a collection
var newProducts = new List<Product> { product1, product2, product3 };
var created = await products.AddRangeAsync(newProducts);

// Update a collection
foreach (var p in existingProducts)
    p.Price *= 1.1m; // 10% price increase

await products.UpdateRangeAsync(existingProducts);

// Soft-delete a collection
await products.SoftDeleteRangeAsync(discontinuedProducts);
```

The `AddAsync(IEnumerable<T>)` and `AddRangeAsync` overloads behave identically — both accept an `IEnumerable<T>` and return `List<T>`. Use whichever reads more clearly in context.

### Overriding the user ID

All write operations accept an optional `userId` parameter for audit purposes:

```csharp
// Attribute the change to a background job instead of the current user
await products.UpdateAsync(product, userId: "price-sync-job");

// Attribute creation to an import process
await products.AddAsync(product, userId: "csv-import");
```

This is useful for background services, data migrations, or any operation where `IUserInfo` either isn't available or doesn't represent the logical actor.

## Repository manager

### Accessing repositories

`IRepositoryManager` acts as a unit of work, providing access to all registered repositories:

```csharp
public class OrderService(IRepositoryManager repositoryManager)
{
    public async Task PlaceOrderAsync(Order order, List<OrderItem> items)
    {
        var orders = repositoryManager.GetRepository<Order>();
        var orderItems = repositoryManager.GetRepository<OrderItem>();

        await orders.AddAsync(order, saveNow: false);

        foreach (var item in items)
        {
            item.OrderId = order.Id;
            await orderItems.AddAsync(item, saveNow: false);
        }

        await repositoryManager.SaveChangesAsync();
    }
}
```

`GetRepository<T>()` resolves the `IRepositoryContext<T>` from DI on first access and caches it internally using a `ConcurrentDictionary`. Subsequent calls for the same entity type return the cached instance.

**Nuance:** You must register a repository context for every entity type you use — whether injecting `IRepositoryContext<T>` directly or accessing it via `GetRepository<T>()`. An unregistered type will throw at runtime. See [Setup](Setup.md) for `RegisterRepositoryContexts`.

### Transactions

For operations that must succeed or fail together, use explicit transactions:

```csharp
public class TransferService(IRepositoryManager repositoryManager)
{
    public async Task TransferAsync(int fromAccountId, int toAccountId, decimal amount)
    {
        var accounts = repositoryManager.GetRepository<Account>();

        await repositoryManager.BeginTransactionAsync();
        try
        {
            var from = await accounts.GetByIdAsync(fromAccountId);
            var to = await accounts.GetByIdAsync(toAccountId);

            from!.Balance -= amount;
            to!.Balance += amount;

            await accounts.UpdateAsync(from, saveNow: false);
            await accounts.UpdateAsync(to, saveNow: false);

            await repositoryManager.CommitTransactionAsync();
        }
        catch
        {
            await repositoryManager.RollbackTransactionAsync();
            throw;
        }
    }
}
```

`CommitTransactionAsync` calls `SaveChangesAsync` internally before committing, so you don't need to save manually. `RollbackTransactionAsync` discards all pending changes.

**Nuance:** Only one transaction can be active at a time per `IRepositoryManager` instance. Calling `BeginTransactionAsync` while a transaction is already open does not throw — it starts a new one. Calling `CommitTransactionAsync` or `RollbackTransactionAsync` without an active transaction throws `InvalidOperationException`.

## Soft-delete and restore

### Soft-deleting

```csharp
await products.SoftDeleteAsync(product);
```

Sets `IsDeleted = true`, `DeletedById`, and `DeletedUtc`. The entity remains in the database. Any previous restore fields (`RestoredById`, `RestoredUtc`) are cleared.

### Restoring

```csharp
await products.RestoreAsync(product);
```

Sets `IsDeleted = false`, `RestoredById`, and `RestoredUtc`. Clears the deletion fields (`DeletedById`, `DeletedUtc`).

### Hard-deleting

```csharp
var success = await products.DeleteAsync(product);
```

Permanently removes the entity from the database. Returns `true` if the deletion succeeded. Unlike soft-delete, this is irreversible.

**Nuance:** Hard-delete does not accept a `userId` parameter — the current user is still recorded in the audit trail, but the entity itself is gone.

### Querying by soft-delete status

Use `FilterDeleted` to control which records are returned:

```csharp
// Only active (non-deleted) records — the most common case
var active = await products.AsQueryable()
    .FilterDeleted(DeletedQueryType.OnlyActive)
    .ToListAsync();

// Only soft-deleted records (e.g. for a "recycle bin" view)
var deleted = await products.AsQueryable()
    .FilterDeleted(DeletedQueryType.OnlyDeleted)
    .ToListAsync();

// All records regardless of status
var all = await products.AsQueryable()
    .FilterDeleted(DeletedQueryType.All)
    .ToListAsync();
```

**Nuance:** `FilterDeleted` has a generic constraint requiring `T : AuditModel`. It won't compile for entities that don't extend `AuditModel`. For non-`AuditModel` entities with an `IsDeleted` property, the repository's soft-delete operations still work (detected via reflection), but you'll need a manual `.Where(x => !x.IsDeleted)` filter instead.

**Nuance:** `FilterDeleted` is never applied automatically. Queries without it return both active and deleted records. If you always want active-only results, apply `FilterDeleted(DeletedQueryType.OnlyActive)` consistently.

### Soft-delete with non-AuditModel entities

The repository detects any `bool IsDeleted` property via reflection, so soft-delete works on any entity:

```csharp
public class Comment
{
    public int Id { get; set; }
    public string Text { get; set; }
    public bool IsDeleted { get; set; } // Detected by reflection
}

// This works — sets IsDeleted = true
await comments.SoftDeleteAsync(comment);
```

However, only `AuditModel` entities get the full audit fields (`DeletedById`, `DeletedUtc`, `RestoredById`, `RestoredUtc`). Non-`AuditModel` entities just have their `IsDeleted` flag toggled.

## Pagination

### From a queryable

```csharp
var page = await products.AsQueryable()
    .Where(p => !p.IsDeleted)
    .OrderBy(p => p.Name)
    .ToPagedListAsync(pageNumber: 1, pageSize: 20);
```

`ToPagedListAsync` executes two queries: a `COUNT(*)` for the total and a `Skip/Take` for the page data.

### Using the result

```csharp
public async Task<IActionResult> Index(int page = 1)
{
    var products = await _products.AsQueryable()
        .FilterDeleted(DeletedQueryType.OnlyActive)
        .OrderBy(p => p.Name)
        .ToPagedListAsync(page, pageSize: 25);

    // products.Items       — IReadOnlyList<Product> for this page
    // products.TotalCount  — total matching records across all pages
    // products.TotalPages  — calculated from TotalCount / PageSize
    // products.PageNumber  — current page (1-based)
    // products.PageSize    — items per page
    // products.HasNextPage — true if more pages exist
    // products.HasPreviousPage — true if not on the first page
    // products.IsFirstPage — true if on page 1
    // products.IsLastPage  — true if on the last page

    return View(products);
}
```

`PagedList<T>` implements `IReadOnlyList<T>`, so you can iterate it directly, index into it, or use it in `foreach` loops:

```csharp
@foreach (var product in Model) // Model is PagedList<Product>
{
    <p>@product.Name — @product.Price</p>
}

<nav>
    @if (Model.HasPreviousPage)
    {
        <a href="?page=@(Model.PageNumber - 1)">Previous</a>
    }
    @if (Model.HasNextPage)
    {
        <a href="?page=@(Model.PageNumber + 1)">Next</a>
    }
</nav>
```

### In-memory pagination

For collections already in memory, use the synchronous overload:

```csharp
var allItems = GetCachedProducts(); // IEnumerable<Product>
var page = allItems.ToPagedList(pageNumber: 2, pageSize: 10);
```

The in-memory overload materialises the collection once, counts it, then paginates. There's also a synchronous `IQueryable<T>` overload if you need sync database pagination.

### Edge cases

- **Page number too high:** If `pageNumber` exceeds `TotalPages`, it auto-clamps to the last valid page rather than returning an empty result.
- **Page number too low:** `pageNumber < 1` throws `ArgumentOutOfRangeException`.
- **Page size too low:** `pageSize < 1` throws `ArgumentOutOfRangeException`.
- **Empty results:** If the query returns zero records, `TotalPages` is 0, `PageNumber` is clamped to 1, and `Items` is empty.

## Audit trail

### How it works

Every call to `SaveChangesAsync` on a `DataDbContext` (or any subclass) automatically creates audit trail entries. The audit service inspects EF Core's change tracker and records:

- **Create** — all non-null property values as JSON
- **Update** — only modified properties with their old and new values
- **SoftDelete** — detected when `IsDeleted` changes from `false` to `true`
- **Restore** — detected when `IsDeleted` changes from `true` to `false`
- **Delete** — permanent deletion, with old and new values for modified properties

This happens transparently — there is no additional code to write. Any entity tracked by EF Core (except `AuditEntry` itself) is audited.

### Querying the audit trail

Audit entries are stored in the `AuditEntries` DbSet on your context:

```csharp
public class AuditController(AppDbContext context) : Controller
{
    public async Task<IActionResult> History(string tableName)
    {
        var entries = await context.AuditEntries
            .Where(a => a.TableName == tableName)
            .OrderByDescending(a => a.AuditDate)
            .Take(50)
            .ToListAsync();

        return View(entries);
    }

    public async Task<IActionResult> UserActivity(string userId)
    {
        var entries = await context.AuditEntries
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AuditDate)
            .ToPagedListAsync(pageNumber: 1, pageSize: 20);

        return View(entries);
    }
}
```

### AuditEntry structure

Each `AuditEntry` records:

| Property | Description |
|----------|-------------|
| `Id` | GUID-based primary key |
| `Action` | `AuditAction` enum — `Create`, `Update`, `SoftDelete`, `Delete`, `Restore` |
| `AuditDate` | UTC timestamp of when the action occurred |
| `UserId` | The user who performed the action |
| `UserName` | The user's display name |
| `TableName` | The database table affected |
| `ActionData` | JSON-serialised change data |

### Reading ActionData

For **Create** actions, `ActionData` contains all non-null property values:

```json
{"Id": 42, "Name": "Widget", "Price": 9.99, "CategoryId": 3, "CreatedById": "user-1"}
```

For **Update**, **SoftDelete**, **Delete**, and **Restore** actions, `ActionData` contains only modified properties with before/after values:

```json
{"Price": {"From": 9.99, "To": 12.99}, "LastModifiedById": {"From": "user-1", "To": "user-2"}}
```

**Nuance:** If JSON serialisation fails for an entity (e.g. circular references), `ActionData` is set to `null` rather than throwing. The audit entry is still created — you just won't have the change data.

### Two-phase audit for creates

Create actions are logged in two phases. During the first `SaveChangesAsync`, the entity doesn't yet have its database-generated ID. The audit service defers create entries, then after the main save completes, it logs them with the now-available IDs and calls `SaveChangesAsync` a second time. This means a single `SaveChangesAsync` call may result in two actual database writes when new entities are being created.

## DateTime extensions

### Relative time

```csharp
var createdAt = new DateTime(2026, 3, 8, 10, 0, 0, DateTimeKind.Utc);

createdAt.ToRelativeTime(); // "3 hours ago" (if current UTC time is 13:00)
```

Handles both past and future dates:

```csharp
DateTime.UtcNow.AddMinutes(-30).ToRelativeTime();  // "30 minutes ago"
DateTime.UtcNow.AddDays(-1).ToRelativeTime();       // "yesterday"
DateTime.UtcNow.AddDays(1).ToRelativeTime();         // "tomorrow"
DateTime.UtcNow.AddMonths(-3).ToRelativeTime();      // "3 months ago"
DateTime.UtcNow.AddSeconds(-10).ToRelativeTime();    // "just now"
```

**Nuance:** `ToRelativeTime` compares against `DateTime.UtcNow`. If you pass a local time without converting to UTC first, the relative calculation will be off by your timezone offset.

### Friendly date

```csharp
var date = new DateTime(2026, 3, 8);
date.ToFriendlyDate(); // "Sunday 8 March 2026" (using current culture)

// With explicit culture
date.ToFriendlyDate(new CultureInfo("fr-FR")); // "dimanche 8 mars 2026"
```

### Age calculation

```csharp
var dob = new DateTime(1990, 6, 15);
dob.Age(); // 35 (if today is 8 March 2026 — birthday hasn't occurred yet this year)
```

Correctly accounts for whether this year's birthday has already passed.

## String extensions

### Truncate

```csharp
"A long product description that goes on and on".Truncate(20);
// "A long product de..."

"Short".Truncate(20);
// "Short" (unchanged — within limit)

"Hello World".Truncate(8, suffix: "…");
// "Hello W…"
```

The suffix is counted in the total length. If `maxLength` is shorter than the suffix itself, the suffix is trimmed to fit.

### Slug

```csharp
"Hello World!".ToSlug();           // "hello-world"
"My Blog Post (2026)".ToSlug();    // "my-blog-post-2026"
"  Multiple   Spaces  ".ToSlug();  // "multiple-spaces"
```

Lowercases, replaces non-alphanumeric characters with hyphens, collapses consecutive hyphens, and trims leading/trailing hyphens.

### Title case

```csharp
"hello world".ToTitleCase();                           // "Hello World"
"the quick brown fox".ToTitleCase(new CultureInfo("en-GB")); // "The Quick Brown Fox"
```

### Mask

```csharp
"john@example.com".Mask(4);  // "john*************"
"secret123".Mask(3);          // "sec******"
"ab".Mask(5);                  // "ab" (shorter than visibleChars — returned as-is)
```

Keeps the first `visibleChars` characters visible and replaces the rest with asterisks.

## Enum extensions

### Display name

Converts enum values to human-readable text, supporting PascalCase, SCREAMING_CASE, and acronym prefixes:

```csharp
public enum OrderStatus
{
    PendingApproval,       // PascalCase
    InProgress,
    CompletedSuccessfully,
    XMLExport,             // Acronym prefix
    PENDING_APPROVAL       // SCREAMING_CASE
}

OrderStatus.PendingApproval.ToDisplayName();       // "Pending approval"
OrderStatus.InProgress.ToDisplayName();             // "In progress"
OrderStatus.CompletedSuccessfully.ToDisplayName(); // "Completed successfully"
OrderStatus.XMLExport.ToDisplayName();              // "XML Export"
OrderStatus.PENDING_APPROVAL.ToDisplayName();      // "Pending Approval"
```

Handles PascalCase, SCREAMING_CASE (underscores become spaces), and acronyms (keeps consecutive uppercase letters together).

### Description attribute

Reads the `[Description]` attribute if present, otherwise falls back to `ToDisplayName()`:

```csharp
public enum PaymentMethod
{
    [Description("Credit or debit card")]
    Card,

    [Description("Bank transfer (BACS)")]
    BankTransfer,

    DirectDebit // No description attribute
}

PaymentMethod.Card.GetDescription();         // "Credit or debit card"
PaymentMethod.BankTransfer.GetDescription(); // "Bank transfer (BACS)"
PaymentMethod.DirectDebit.GetDescription();  // "Direct debit" (falls back to ToDisplayName)
```

### Safe parsing

```csharp
var status = EnumExtensions.TryParse<OrderStatus>("inprogress");
// OrderStatus.InProgress (case-insensitive)

var unknown = EnumExtensions.TryParse<OrderStatus>("invalid", defaultValue: OrderStatus.PendingApproval);
// OrderStatus.PendingApproval (fallback)

var empty = EnumExtensions.TryParse<OrderStatus>(null);
// OrderStatus.PendingApproval (default(OrderStatus))
```

**Nuance:** `TryParse` is a static method, not an extension method. Call it as `EnumExtensions.TryParse<T>(value)`.

### Listing all options

```csharp
var options = default(OrderStatus).GetAllOptions();
// [(Name: "PendingApproval", Value: 0), (Name: "InProgress", Value: 1), ...]
```

Useful for populating dropdowns or select lists. Call on `default(T)` since the instance value is ignored.

## Helpers

### ColourHelper

Generates UI colour variants from a hex colour:

```csharp
// Lighten a colour for hover states (40% lighter)
ColourHelper.HoverColour("#3498DB"); // "#70B7E6"

// Determine black or white font colour for a background
ColourHelper.FontColour("#3498DB"); // "#ffffff" (white — dark background)
ColourHelper.FontColour("#F1C40F"); // "#000000" (black — light background)
```

`FontColour` uses the W3C relative luminance formula (`0.2126R + 0.7152G + 0.0722B`). Returns black if luminance exceeds 0.5, white otherwise.

**Nuance:** Both methods expect a `#RRGGBB` hex string. They do not validate input — passing a malformed string will throw.

### CountryHelper

Provides ISO 3166-1 country data derived from .NET's `CultureInfo` and `RegionInfo`:

```csharp
// Get all countries (cached after first call)
var countries = CountryHelper.GetCountries();
// [Country("AF", "Afghanistan"), Country("AL", "Albania"), ...]

// For dropdown binding — Dictionary<Code, Name>
var dropdown = CountryHelper.GetCountriesDictionary();
// {"AF": "Afghanistan", "AL": "Albania", ...}

// Look up by code
CountryHelper.GetCountryName("GB"); // "United Kingdom"

// Look up by name
CountryHelper.GetCountryCode("United Kingdom"); // "GB"
```

Results are cached after the first call. The list is derived from all specific cultures in the runtime, so the available countries depend on the .NET runtime's culture data.

**Nuance:** You can pass an `ILogger` to `GetCountries()` to log warnings for cultures that fail to create a `RegionInfo`. This is optional — without it, failed cultures are silently skipped.

### ConstHelper

Discovers all `const` fields on a type using reflection:

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";
}

var consts = ConstHelper.GetAllConsts<AppRoles>();
// {
//   "SystemAdmin": "SystemAdmin",
//   "SystemAdminDesc": "Full system administrator...",
//   "Admin": "Admin",
//   "AdminDesc": "Administrator with access...",
//   "Editor": "Editor",
//   "EditorDesc": "Can create and edit content."
// }
```

Finds `const` fields at all access levels (public, private, protected) including inherited fields. Excludes `static readonly` fields — only true compile-time constants.

This is used internally by JC.Identity's role seeding, but is available for any scenario where you need to discover constants on a type at runtime.
