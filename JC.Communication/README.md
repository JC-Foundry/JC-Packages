# JC.Communication

Application communication for .NET, in three features registered independently — email, in-app notifications, and messaging. Take only the ones you need; none of them assumes the others.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Communication/JC.Communication.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- **JC.Identity** for notifications and messaging — both require `IUserInfo`, and registration throws without it
- A `DbContext` implementing the interfaces for the features you use: `IEmailDbContext`, `INotificationDbContext`, `IMessagingDbContext`

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

// Each is optional and independent
builder.Services.AddEmail<AppDbContext>(builder.Configuration);
builder.Services.AddNotifications<AppDbContext>();
builder.Services.AddMessaging<AppDbContext>();
```

### Data — `AppDbContext`

Apply the mappings for the features you registered — there is one method per feature, not a combined call:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyEmailMappings();
    modelBuilder.ApplyNotificationMappings();
    modelBuilder.ApplyMessagingMappings();
}
```

### Configuration — `appsettings.json`

Required for email. The keys below are the Microsoft provider's; other providers need different ones.

```json
{
  "Communication": {
    "Email": {
      "TenantId": "your-azure-tenant-id",
      "ClientId": "your-azure-client-id",
      "ClientSecret": "your-azure-client-secret",
      "DefaultFromAddress": "noreply@example.com",
      "DefaultFromDisplayName": "Example"
    }
  }
}
```

Configuration is validated at registration, so a missing key fails at start-up rather than on the first send.

## Feature areas

### Email

Four providers behind one `IEmailService`:

| Provider | Transport |
|----------|-----------|
| `Microsoft` | OAuth2 (MSAL) over the Microsoft 365 / Exchange Online SMTP relay |
| `SmtpRelay` | Authenticated SMTP relay, with password, API key or secret |
| `DirectSmtp` | Direct SMTP |
| `Console` | Writes to the logger — for development |

```csharp
public class SignupService(IEmailService email)
{
    public Task<EmailSendResult> WelcomeAsync(string address) =>
        email.SendAsync(
            [new EmailRecipient(address, "New User")],
            subject: "Welcome",
            plainBody: "Thanks for signing up.");
}
```

Sending returns an `EmailSendResult` rather than throwing, so a delivery failure is a value to handle, not an exception to catch.

### Attachments

```csharp
var message = new EmailMessage(from, htmlBody, plainBody, subject, recipients)
    .WithAttachment("invoice.pdf", pdfBytes)
    .WithAttachment(new EmailAttachment("terms.pdf", termsBytes, "application/pdf"));
```

Content type is inferred from the file name when omitted. Validation rejects path separators in a file name, empty content, and a combined size over `MaxTotalAttachmentBytes` — 18 MB by default, chosen so the base64-encoded message stays under the 25 MB cap Microsoft 365 and Gmail apply.

### Email logging

```csharp
builder.Services.AddEmail<AppDbContext>(builder.Configuration, options =>
{
    options.LoggingMode = EmailLoggingMode.FullLog;
});
```

`None` writes nothing, and is the only mode the non-generic `AddEmail` overload permits. The logging modes record recipients, send results and attachment metadata; only `FullLog` stores body content. Attachment metadata — name, type and size — is recorded under every mode, since it never contains the file itself.

### Notifications

Per-user in-app notifications with memory caching:

```csharp
public class OrderService(NotificationSender sender)
{
    public Task NotifyAsync(string userId) =>
        sender.SendNotification(userId, "Order shipped", "Your order is on its way.",
            type: NotificationType.Success);
}
```

Read them through `NotificationCache`, which is what the JC.Communication.Web tag helpers use. Per-notification icon and colour overrides live on `NotificationStyle`; the defaults for each `NotificationType` are decided by whichever UI package renders them.

### Messaging

Chat threads with participants, replies, per-thread metadata and read tracking:

```csharp
public class ChatService(ChatThreadService threads, ChatMessageService messages)
{
    public Task<List<ChatModel>> MineAsync() => threads.GetUserChats();

    public async Task<ChatMessageValidationResponse> SendAsync(string threadId, string body)
        => await messages.TrySendMessage(threadId, body, replyToId: null);
}
```

The `Try*` methods return a validation response rather than throwing, so a rejected message is a value to inspect — `IsValid` plus the reason.

### Retention

Each feature ships a cleanup job implementing `IBackgroundJob`, so retention is wired through [JC.BackgroundJobs](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.BackgroundJobs) rather than run here:

```csharp
builder.Services.ConfigureEmailBackgroundJobs(o => o.EmailLogRetentionMonths = 6);
builder.Services.AddHangfireJob<EmailLogCleanupJob<AppDbContext>>(o => o.Cron = "0 3 * * *");
```

## Defaults

| Option | Default |
|--------|---------|
| `EmailOptions.Provider` | `Microsoft` |
| `EmailOptions.LoggingMode` | Set per application; `None` is required by the non-generic overload |
| `EmailOptions.TimeoutMs` | 30,000 |
| `EmailOptions.MaxTotalAttachmentBytes` | 18 MB |
| `NotificationOptions.CacheDurationHours` | Validated to 1–72 |
| Notification and messaging services | Scoped |
| Email service | Scoped, one implementation selected by provider |

## Documentation

- Email — [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Email-Setup.md) · [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Email-Guide.md) · [API](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Email-API.md)
- Notifications — [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Notifications-Setup.md) · [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Notifications-Guide.md) · [API](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Notifications-API.md)
- Messaging — [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Messaging-Setup.md) · [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Messaging-Guide.md) · [API](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Messaging-API.md)

For Razor tag helpers rendering these features, see [JC.Communication.Web](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.Communication.Web).

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
