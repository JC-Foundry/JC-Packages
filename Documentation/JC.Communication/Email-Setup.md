# JC.Communication: Email — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Communication`:

```xml
<ProjectReference Include="path/to/JC.Communication/JC.Communication.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

// Register email with the Microsoft provider, database logging, and default options
builder.Services.AddEmail<AppDbContext>(builder.Configuration);
```

### Configuration — `appsettings.json`

The Microsoft provider requires Azure AD credentials and a sender address:

```json
{
  "Communication": {
    "Email": {
      "TenantId": "your-azure-tenant-id",
      "ClientId": "your-azure-client-id",
      "ClientSecret": "your-azure-client-secret",
      "DefaultFromAddress": "noreply@yourdomain.com",
      "DefaultFromDisplayName": "My Application"
    }
  }
}
```

`DefaultFromDisplayName` is optional. If not set, the from address is used as the display name.

### DbContext

Your `DbContext` must implement `IEmailDbContext` and apply the email data mappings:

```csharp
public class AppDbContext : DataDbContext, IEmailDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<EmailRecipientLog> EmailRecipientLogs { get; set; }
    public DbSet<EmailContentLog> EmailContentLogs { get; set; }
    public DbSet<EmailSentLog> EmailSentLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyEmailMappings();

        // ...existing code...
    }

    // ...existing code...
}
```

### Defaults

When called with no configure callback, `AddEmail<TContext>` registers:

| Registration | Lifetime | Description |
|-------------|----------|-------------|
| `IOptions<EmailOptions>` | Singleton | Email configuration options |
| `IEmailService` → `MicrosoftEmailService` | Scoped | Email sending via Microsoft 365 OAuth SMTP relay |
| `EmailLogService` | Scoped | Persists email send attempts to the database |
| `IEmailDbContext` → `TContext` | Scoped | Your DbContext as the email data context |
| `IRepositoryContext<EmailLog>` | Scoped | Repository for email log entries |
| `IRepositoryContext<EmailRecipientLog>` | Scoped | Repository for recipient log entries |
| `IRepositoryContext<EmailContentLog>` | Scoped | Repository for content log entries |
| `IRepositoryContext<EmailSentLog>` | Scoped | Repository for send result log entries |

Default option values:

| Option | Default | Description |
|--------|---------|-------------|
| `Provider` | `EmailProvider.Microsoft` | The email provider to use |
| `LoggingMode` | `EmailLoggingMode.ExcludeContent` | Logs metadata (sender, recipients, subject, result) but not email body content |
| `TimeoutMs` | `30000` | SMTP send timeout in milliseconds |
| `Host` | `"smtp.office365.com"` | SMTP server hostname — validated at startup for SMTP-based providers |
| `Port` | `587` | SMTP server port |
| `EnableSsl` | `true` | Use StartTLS when connecting — must be `true` for Microsoft provider |
| `SslProtocol` | `SslProtocols.None` | SSL/TLS protocol version — `None` lets the OS negotiate |
| `LogLevel` | `LogLevel.Information` | Console provider only — the log level used when outputting email content |
| `UsernameRequired` | `true` | SMTP relay only — whether a username is required for authentication |

Startup validation checks for missing configuration values and invalid options. If required config keys are absent or the host/port are invalid, the application fails at startup with a descriptive error rather than at runtime.

## 2. Full configuration

### Registration overloads

There are two registration methods:

**`AddEmail<TContext>`** — registers email with database logging. Your DbContext must implement `IEmailDbContext`.

```csharp
builder.Services.AddEmail<AppDbContext>(builder.Configuration, options =>
{
    options.Provider = EmailProvider.Microsoft;
    options.LoggingMode = EmailLoggingMode.ExcludeContent;
    options.TimeoutMs = 30_000;
    options.Host = "smtp.office365.com";
    options.Port = 587;
    options.EnableSsl = true;
    options.SslProtocol = SslProtocols.None;
});
```

**`AddEmail`** (non-generic) — registers email without database logging. `LoggingMode` must be set to `None` or an `InvalidOperationException` is thrown at startup.

```csharp
builder.Services.AddEmail(builder.Configuration, options =>
{
    options.Provider = EmailProvider.Microsoft;
    options.LoggingMode = EmailLoggingMode.None;
    options.Host = "smtp.office365.com";
    options.Port = 587;
});
```

### Providers

#### Microsoft — OAuth SMTP relay

Sends email via Microsoft 365 / Exchange Online using OAuth2 authentication (MSAL). This is the default provider.

```csharp
builder.Services.AddEmail<AppDbContext>(builder.Configuration, options =>
{
    options.Provider = EmailProvider.Microsoft;
    options.Host = "smtp.office365.com";
    options.Port = 587;
    options.EnableSsl = true;
});
```

Configuration — `appsettings.json`:

```json
{
  "Communication": {
    "Email": {
      "TenantId": "your-azure-tenant-id",
      "ClientId": "your-azure-client-id",
      "ClientSecret": "your-azure-client-secret",
      "DefaultFromAddress": "noreply@yourdomain.com",
      "DefaultFromDisplayName": "My Application"
    }
  }
}
```

| Key | Required | Description |
|-----|----------|-------------|
| `Communication:Email:TenantId` | Yes | Azure AD tenant ID |
| `Communication:Email:ClientId` | Yes | Azure AD application (client) ID |
| `Communication:Email:ClientSecret` | Yes | Azure AD client secret |
| `Communication:Email:DefaultFromAddress` | Yes | Default sender email address — used by the simple `SendAsync` overload |
| `Communication:Email:DefaultFromDisplayName` | No | Sender display name — falls back to the from address if not set |

The Azure AD app registration must have the `SMTP.SendAsApp` permission, and the from address must correspond to a mailbox or shared mailbox the app has "Send As" permission for. Mismatched addresses will be rejected by the Microsoft SMTP relay at runtime. `EnableSsl` must be `true` — the Microsoft provider throws at construction if SSL is disabled.

#### SMTP relay — username/password or API key authentication

Sends email via a third-party SMTP relay (e.g. SendGrid, Mailgun, Amazon SES) using standard SMTP authentication.

```csharp
builder.Services.AddEmail<AppDbContext>(builder.Configuration, options =>
{
    options.Provider = EmailProvider.SmtpRelay;
    options.Host = "smtp.sendgrid.net";
    options.Port = 587;
    options.EnableSsl = true;
    options.UsernameRequired = true;
});
```

Configuration — `appsettings.json` (username + password):

```json
{
  "Communication": {
    "Email": {
      "DefaultFromAddress": "noreply@yourdomain.com",
      "Username": "apikey",
      "Password": "your-sendgrid-api-key"
    }
  }
}
```

| Key | Required | Description |
|-----|----------|-------------|
| `Communication:Email:DefaultFromAddress` | Yes | Default sender email address |
| `Communication:Email:Username` | When `UsernameRequired = true` | SMTP username |
| `Communication:Email:Password` | At least one secret required | SMTP password — checked first |
| `Communication:Email:ApiKey` | At least one secret required | API key — checked second |
| `Communication:Email:Secret` | At least one secret required | Generic secret — checked last |

The secret keys (`Password`, `ApiKey`, `Secret`) provide flexibility for different providers. At least one must be configured. They are checked in order: `Password` → `ApiKey` → `Secret`, and the first non-empty value is used.

**API key-only authentication** — for relays that authenticate with just an API key and no separate username:

```csharp
options.Provider = EmailProvider.SmtpRelay;
options.UsernameRequired = false;
```

```json
{
  "Communication": {
    "Email": {
      "DefaultFromAddress": "noreply@yourdomain.com",
      "ApiKey": "your-api-key"
    }
  }
}
```

When `UsernameRequired` is `false`, only the API key (or secret) is needed — no separate username is required. The SMTP protocol still requires a username field, so `"apikey"` is sent as a placeholder. At least one secret (Password, ApiKey, or Secret) is always required — startup validation will fail if none are configured.

#### Direct SMTP — no authentication

Sends email directly via SMTP without authentication. Useful for internal mail servers or development environments where the SMTP server does not require credentials.

```csharp
builder.Services.AddEmail<AppDbContext>(builder.Configuration, options =>
{
    options.Provider = EmailProvider.DirectSmtp;
    options.Host = "mail.internal.local";
    options.Port = 25;
    options.EnableSsl = false;
});
```

Configuration — `appsettings.json`:

```json
{
  "Communication": {
    "Email": {
      "DefaultFromAddress": "noreply@yourdomain.com"
    }
  }
}
```

| Key | Required | Description |
|-----|----------|-------------|
| `Communication:Email:DefaultFromAddress` | Yes | Default sender email address |

Many ISPs block outbound port 25 traffic, so this provider may not work in all environments.

#### Console — development and testing

Outputs email content to the application logger instead of sending via SMTP. No SMTP configuration is required.

```csharp
builder.Services.AddEmail<AppDbContext>(builder.Configuration, options =>
{
    options.Provider = EmailProvider.Console;
    options.LogLevel = LogLevel.Information;
});
```

`DefaultFromAddress` is required and validated at startup to ensure the simple `SendAsync` overload works.

Configuration — `appsettings.json`:

```json
{
  "Communication": {
    "Email": {
      "DefaultFromAddress": "noreply@yourdomain.com"
    }
  }
}
```

| Key | Required | Description |
|-----|----------|-------------|
| `Communication:Email:DefaultFromAddress` | Yes | Default sender email address |

The console provider always outputs the email body (plain text) to the application logger, regardless of the `LoggingMode` setting. The logging mode only controls what is persisted to the database. If email body content is sensitive, be aware that console output will still contain it.

### Logging modes

| Mode | Database logging | Description |
|------|-----------------|-------------|
| `None` | No | No email data is persisted. Required when using the non-generic `AddEmail` overload |
| `ExcludeContent` | Yes — metadata only | Logs sender, recipients, subject, and send result. Does not store email body content |
| `FullLog` | Yes — everything | Logs all metadata plus the plain text and HTML body content |

```csharp
// Log everything including body content
builder.Services.AddEmail<AppDbContext>(builder.Configuration, options =>
{
    options.LoggingMode = EmailLoggingMode.FullLog;
});
```

```csharp
// No database logging — non-generic overload
builder.Services.AddEmail(builder.Configuration, options =>
{
    options.Provider = EmailProvider.SmtpRelay;
    options.LoggingMode = EmailLoggingMode.None;
    options.Host = "smtp.example.com";
    options.Port = 587;
});
```

Both successful and failed send attempts are logged, including validation failures.

### EmailOptions

| Property | Type | Default | Applies to | Description |
|----------|------|---------|------------|-------------|
| `Provider` | `EmailProvider` | `Microsoft` | All | The email provider to use |
| `LoggingMode` | `EmailLoggingMode` | `ExcludeContent` | All | Controls what is persisted to the database |
| `TimeoutMs` | `int` | `30000` | Microsoft, SmtpRelay, DirectSmtp | SMTP send timeout in milliseconds |
| `Host` | `string` | `"smtp.office365.com"` | Microsoft, SmtpRelay, DirectSmtp | SMTP server hostname — validated at startup |
| `Port` | `int` | `587` | Microsoft, SmtpRelay, DirectSmtp | SMTP server port (1–65535) — validated at startup |
| `EnableSsl` | `bool` | `true` | Microsoft, SmtpRelay, DirectSmtp | Use StartTLS when connecting. Must be `true` for Microsoft |
| `SslProtocol` | `SslProtocols` | `None` | Microsoft, SmtpRelay, DirectSmtp | SSL/TLS protocol version. `None` lets the OS negotiate the highest supported version |
| `LogLevel` | `LogLevel` | `Information` | Console | The log level used when outputting email content to the application logger |
| `UsernameRequired` | `bool` | `true` | SmtpRelay | Whether a username is required for SMTP authentication |

## 3. Apply migrations

If using database logging (`AddEmail<TContext>` with `LoggingMode` other than `None`), the package introduces four tables: `EmailLogs`, `EmailRecipientLogs`, `EmailContentLogs`, and `EmailSentLogs`.

Generate and apply the migration:

```bash
dotnet ef migrations add AddEmailLogging --project YourApp
dotnet ef database update --project YourApp
```

## 4. Verify

1. Run the application — startup should complete without configuration validation errors.
2. Inject `IEmailService` and call `SendAsync` with a test recipient — check the application logs for any errors.
3. If database logging is enabled, verify that records appear in the `EmailLogs` and `EmailSentLogs` tables.

## Next steps

- [Email guide](Email-Guide.md) — sending emails, message construction, validation, and error handling.
- [Email API reference](Email-API.md)
