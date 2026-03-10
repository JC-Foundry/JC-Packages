# JC.Communication: Email — API reference

Complete reference for all public types in the email module. See [Email setup](Email-Setup.md) for registration and [Email guide](Email-Guide.md) for usage examples.

---

## IEmailService

**Namespace:** `JC.Communication.Email.Services`

Provides email sending capabilities. Inject via `IEmailService`. The concrete implementation is determined by the configured `EmailProvider` at registration.

### Methods

#### SendAsync(IEnumerable\<EmailRecipient\> recipients, string subject, string plainBody, string? htmlBody = null, IEnumerable\<EmailRecipient\>? ccRecipients = null, IEnumerable\<EmailRecipient\>? bccRecipients = null)

**Returns:** `Task<EmailSendResult>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `recipients` | `IEnumerable<EmailRecipient>` | — | The primary recipients of the email. |
| `subject` | `string` | — | The email subject line. |
| `plainBody` | `string` | — | The plain text body. |
| `htmlBody` | `string?` | `null` | Optional HTML body. When `null`, the HTML body defaults to `plainBody` via the `EmailMessage` constructor. |
| `ccRecipients` | `IEnumerable<EmailRecipient>?` | `null` | Optional carbon copy recipients. |
| `bccRecipients` | `IEnumerable<EmailRecipient>?` | `null` | Optional blind carbon copy recipients. |

Sends an email using the default from address read from `Communication:Email:DefaultFromAddress` configuration. Constructs an `EmailMessage` internally and delegates to the `SendAsync(EmailMessage, CancellationToken)` overload. Throws `InvalidOperationException` if the default from address is not configured.

#### SendAsync(EmailMessage message, CancellationToken cancellationToken = default)

**Returns:** `Task<EmailSendResult>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `EmailMessage` | — | The fully constructed email message to send. |
| `cancellationToken` | `CancellationToken` | `default` | Optional cancellation token. |

Validates the message via `EmailMessage.ValidateEmailMessage()` before attempting to send. If validation fails, returns a failed `EmailSendResult` with the validation errors as the error message and logs the attempt. If validation passes, sends the email via the configured provider and logs the result. Both successful and failed attempts are logged.

---

## EmailMessage

**Namespace:** `JC.Communication.Email.Models`

Represents a single outbound email message with sender, recipients, subject, and body content. Sealed class — cannot be inherited.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `NoSubject` | `string` | `"NO SUBJECT"` | Default subject used when no subject is provided or the subject is empty. |

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `FromAddress` | `string` | — | get | The sender's email address. |
| `ToAddresses` | `List<EmailRecipient>` | — | get | The primary recipients. Must contain at least one recipient. |
| `CcAddresses` | `List<EmailRecipient>` | `[]` | get | Carbon copy recipients. |
| `BccAddresses` | `List<EmailRecipient>` | `[]` | get | Blind carbon copy recipients. |
| `Subject` | `string` | — | get | The email subject line. Defaults to `NoSubject` if the provided subject is null or empty. |
| `PlainBody` | `string` | — | get | The plain text body of the email. |
| `HtmlBody` | `string` | — | get | The HTML body. Defaults to `PlainBody` if not explicitly provided or if the provided value is null or empty. |

### Constructors

#### EmailMessage(string from, string plainBody, string? subject = null, params IEnumerable\<EmailRecipient\> toAddresses)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `from` | `string` | — | The sender's email address. |
| `plainBody` | `string` | — | The plain text body. Also used as the HTML body. |
| `subject` | `string?` | `null` | The subject line. Defaults to `NoSubject` if null or empty. |
| `toAddresses` | `params IEnumerable<EmailRecipient>` | — | One or more recipients. |

Creates an email message with a plain text body. `HtmlBody` is set to the same value as `PlainBody`. Throws `ArgumentException` if no recipients are provided.

#### EmailMessage(string from, string htmlBody, string plainBody, string? subject = null, params IEnumerable\<EmailRecipient\> toAddresses)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `from` | `string` | — | The sender's email address. |
| `htmlBody` | `string` | — | The HTML body content. Falls back to `plainBody` if null or empty. |
| `plainBody` | `string` | — | The plain text body content. |
| `subject` | `string?` | `null` | The subject line. Defaults to `NoSubject` if null or empty. |
| `toAddresses` | `params IEnumerable<EmailRecipient>` | — | One or more recipients. |

Creates an email message with separate HTML and plain text bodies. Chains to the plain-body constructor, then sets `HtmlBody` to `htmlBody` if it is not null or empty, otherwise keeps `PlainBody`. Throws `ArgumentException` if no recipients are provided.

#### EmailMessage(string from, string htmlBody, string plainBody, string subject, IEnumerable\<EmailRecipient\> toAddresses, IEnumerable\<EmailRecipient\> ccAddresses, IEnumerable\<EmailRecipient\> bccAddresses)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `from` | `string` | — | The sender's email address. |
| `htmlBody` | `string` | — | The HTML body content. Falls back to `plainBody` if null or empty. |
| `plainBody` | `string` | — | The plain text body content. |
| `subject` | `string` | — | The subject line. |
| `toAddresses` | `IEnumerable<EmailRecipient>` | — | The primary recipients. |
| `ccAddresses` | `IEnumerable<EmailRecipient>` | — | Carbon copy recipients. |
| `bccAddresses` | `IEnumerable<EmailRecipient>` | — | Blind carbon copy recipients. |

Creates an email message with separate HTML and plain text bodies, including CC and BCC recipients. Chains to the two-body constructor, then populates `CcAddresses` and `BccAddresses`. Throws `ArgumentException` if no primary recipients are provided.

### Methods

#### ValidateEmailMessage()

**Returns:** `string?`

Validates the email message and returns all errors as a newline-separated string, or `null` if the message is valid. Checks are performed in order:

1. `FromAddress` is required — must not be empty or whitespace.
2. `FromAddress` must contain `@`.
3. `PlainBody` is required — must not be empty or whitespace.
4. All recipient addresses (To, CC, BCC) must not be empty/whitespace and must contain `@`. Invalid addresses are listed in the error.
5. Duplicate addresses across To, CC, and BCC are detected using case-insensitive comparison. Duplicates are listed in the error.

All errors are collected and returned together. Catches `NullReferenceException` internally as a safety net for null address collections.

#### ToSafeLog()

**Returns:** `(EmailLog Log, List<EmailRecipientLog> Recipients)`

Creates log entities excluding email body content. Builds an `EmailLog` with `FromAddress` and `Subject`, and an `EmailRecipientLog` for each recipient in To, CC, and BCC with the appropriate `RecipientLogType`.

#### ToFullLog()

**Returns:** `(EmailLog Log, List<EmailRecipientLog> Recipients, EmailContentLog ContentLog)`

Creates log entities including email body content. Extends `ToSafeLog` with an `EmailContentLog` containing the plain body. The HTML body (`HtmlBodyRaw`) is only stored when it differs from the plain body — if they are equal, `HtmlBodyRaw` is set to `null`.

---

## EmailRecipient

**Namespace:** `JC.Communication.Email.Models`

Record representing an email recipient with an address and optional display name.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Address` | `string` | — | get; init | The recipient's email address. |
| `DisplayName` | `string?` | `null` | get; init | Optional display name. When set, the email is rendered as `"DisplayName" <Address>`. When `null`, the address is used as the display name. |

---

## EmailSendResult

**Namespace:** `JC.Communication.Email.Models`

Represents the result of an email send attempt.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Succeeded` | `bool` | `true` | get | Whether the email was sent successfully. |
| `Provider` | `EmailProvider` | — | get | The email provider that handled the send attempt. |
| `SentAtUtc` | `DateTime` | `DateTime.UtcNow` | get | UTC timestamp of the send attempt. |
| `ServerResponse` | `string?` | `null` | get | The SMTP server response string on success. `null` on failure or for the console provider. |
| `ErrorMessage` | `string?` | `null` | get | The error description on failure. `null` on success. |

### Constructors

#### EmailSendResult(EmailProvider provider, string? messageId = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `provider` | `EmailProvider` | — | The provider that handled the send. |
| `messageId` | `string?` | `null` | Optional SMTP server response string. Stored as `ServerResponse`. |

Creates a successful send result. `SentAtUtc` defaults to `DateTime.UtcNow`.

#### EmailSendResult(DateTime sentAtUtc, EmailProvider provider, string? messageId = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `sentAtUtc` | `DateTime` | — | The UTC timestamp of the send. |
| `provider` | `EmailProvider` | — | The provider that handled the send. |
| `messageId` | `string?` | `null` | Optional SMTP server response string. Stored as `ServerResponse`. |

Creates a successful send result with an explicit timestamp.

#### EmailSendResult(string errorMsg, EmailProvider provider, string? messageId = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMsg` | `string` | — | The error message describing the failure. |
| `provider` | `EmailProvider` | — | The provider that handled the attempt. |
| `messageId` | `string?` | `null` | Optional SMTP server response string. Stored as `ServerResponse`. |

Creates a failed send result. Sets `Succeeded` to `false` and `ErrorMessage` to `errorMsg`. `SentAtUtc` defaults to `DateTime.UtcNow`.

#### EmailSendResult(string errorMsg, DateTime sentAtUtc, EmailProvider provider, string? messageId = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMsg` | `string` | — | The error message describing the failure. |
| `sentAtUtc` | `DateTime` | — | The UTC timestamp of the send attempt. |
| `provider` | `EmailProvider` | — | The provider that handled the attempt. |
| `messageId` | `string?` | `null` | Optional SMTP server response string. Stored as `ServerResponse`. |

Creates a failed send result with an explicit timestamp. Sets `Succeeded` to `false` and `ErrorMessage` to `errorMsg`.

---

## EmailProvider

**Namespace:** `JC.Communication.Email.Models`

Enum determining which email provider implementation is used.

| Member | Value | Description |
|--------|-------|-------------|
| `Console` | `-1` | Outputs email content to the application logger. Intended for development and testing. Always outputs the email body regardless of the logging mode. |
| `Microsoft` | `0` | Sends email via Microsoft 365 / Exchange Online using OAuth2 (MSAL) SMTP relay. The from address must correspond to a mailbox the Azure AD app has "Send As" permission for. |
| `SmtpRelay` | `1` | Sends email via a third-party SMTP relay using username/password or API key authentication. |
| `DirectSmtp` | `2` | Sends email directly via SMTP without authentication. |

---

## EmailOptions

**Namespace:** `JC.Communication.Email.Models.Options`

Configuration options for email communication.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Provider` | `EmailProvider` | `Microsoft` | get; set; | The email provider to use for sending. |
| `LoggingMode` | `EmailLoggingMode` | `ExcludeContent` | get; set; | Controls what is persisted to the database. Must be `None` when using the non-generic `AddEmail` overload. |
| `TimeoutMs` | `int` | `30000` | get; set; | SMTP send timeout in milliseconds. Applies to Microsoft, SmtpRelay, and DirectSmtp providers. |
| `LogLevel` | `LogLevel` | `Information` | get; set; | The log level used by the console provider when outputting email content. Ignored for all other providers. |
| `Host` | `string` | `"smtp.office365.com"` | get; set; | SMTP server hostname. Validated at startup for SMTP-based providers. |
| `Port` | `int` | `587` | get; set; | SMTP server port. Must be between 1 and 65535. Validated at startup. |
| `EnableSsl` | `bool` | `true` | get; set; | Whether to use StartTLS when connecting. Must be `true` for the Microsoft provider. |
| `SslProtocol` | `SslProtocols` | `None` | get; set; | SSL/TLS protocol version. `None` lets the OS negotiate the highest supported version. |
| `UsernameRequired` | `bool` | `true` | get; set; | Whether a username is required for SMTP relay authentication. Only applies to the SmtpRelay provider. |

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `ConfigFromAddress` | `string` | `"Communication:Email:DefaultFromAddress"` | Configuration key for the default sender email address. |
| `ConfigFromDisplayName` | `string` | `"Communication:Email:DefaultFromDisplayName"` | Configuration key for the default sender display name. |

---

## EmailLoggingMode

**Namespace:** `JC.Communication.Email.Models.Options`

Enum controlling how email send attempts are logged to the database.

| Member | Value | Description |
|--------|-------|-------------|
| `None` | `0` | No logging. Email send attempts are not persisted. Required when using the non-generic `AddEmail` overload. |
| `ExcludeContent` | `1` | Logs metadata only — sender, recipients, subject, and send result. Does not store email body content. |
| `FullLog` | `2` | Logs all email data including body content. |

---

## MicrosoftOptions

**Namespace:** `JC.Communication.Email.Models.Options`

Configuration key constants for the Microsoft OAuth email provider.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `TenantId` | `string` | `"Communication:Email:TenantId"` | Configuration key for the Azure AD tenant ID. |
| `ClientId` | `string` | `"Communication:Email:ClientId"` | Configuration key for the Azure AD application (client) ID. |
| `ClientSecret` | `string` | `"Communication:Email:ClientSecret"` | Configuration key for the Azure AD client secret. |

---

## SmtpRelayOptions

**Namespace:** `JC.Communication.Email.Models.Options`

Configuration key constants for the SMTP relay email provider. Multiple secret key names are supported to provide flexibility for different relay services.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `Username` | `string` | `"Communication:Email:Username"` | Configuration key for the SMTP username. Only required when `EmailOptions.UsernameRequired` is `true`. |
| `Password` | `string` | `"Communication:Email:Password"` | Configuration key for the SMTP password. Checked first when resolving the authentication secret. |
| `ApiKey` | `string` | `"Communication:Email:ApiKey"` | Configuration key for the API key. Checked second when resolving the authentication secret. |
| `Secret` | `string` | `"Communication:Email:Secret"` | Configuration key for a generic secret. Checked last when resolving the authentication secret. |

---

## EmailLogService

**Namespace:** `JC.Communication.Logging.Services`

Handles persistence of email send attempts to the database. Inject via `EmailLogService`. Respects the configured `EmailLoggingMode`.

### Methods

#### LogAsync(EmailMessage message, EmailSendResult result, CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `EmailMessage` | — | The email message that was sent or attempted. |
| `result` | `EmailSendResult` | — | The result of the send attempt. |
| `cancellationToken` | `CancellationToken` | `default` | Optional cancellation token. |

Persists an email send attempt to the database within a transaction. Returns immediately without persisting if `EmailLoggingMode` is `None`.

When `ExcludeContent` is configured, creates an `EmailLog` (via `EmailMessage.ToSafeLog`), associated `EmailRecipientLog` entries, and an `EmailSentLog`. When `FullLog` is configured, additionally creates an `EmailContentLog` (via `EmailMessage.ToFullLog`).

All entities are added with `saveNow: false` and saved in a single `SaveChangesAsync` call within a transaction. If the transaction fails, the error is logged to the application logger and the transaction is rolled back. The exception is not thrown — a failed log write does not affect the email send result.

---

## IEmailDbContext

**Namespace:** `JC.Communication.Logging.Data`

Database context interface for email logging. Your application's `DbContext` must implement this interface when using the generic `AddEmail<TContext>` registration.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `EmailLogs` | `DbSet<EmailLog>` | get; set; | Email log entries containing sender and subject metadata. |
| `EmailRecipientLogs` | `DbSet<EmailRecipientLog>` | get; set; | Recipient log entries linked to email logs. |
| `EmailContentLogs` | `DbSet<EmailContentLog>` | get; set; | Content log entries containing email body content. Only populated when `FullLog` is used. |
| `EmailSentLogs` | `DbSet<EmailSentLog>` | get; set; | Send result log entries containing success/failure status and error details. |

---

## EmailLog

**Namespace:** `JC.Communication.Logging.Models.Email`

Persisted log entry for an outbound email. Contains sender and subject metadata with navigation properties to recipients, content, and send results. Extends `AuditModel`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for the email log entry. |
| `FromAddress` | `string` | — | get; set; | The sender's email address. Required. Max length 256. |
| `Subject` | `string` | — | get; set; | The email subject line. Required. Max length 1024. |
| `EmailRecipientLogs` | `ICollection<EmailRecipientLog>` | — | get; set; | Navigation property to the recipients associated with this email. |
| `EmailContentLog` | `EmailContentLog?` | — | get; set; | Navigation property to the email body content log. Only populated when `FullLog` is used. |
| `EmailSentLogs` | `ICollection<EmailSentLog>` | — | get; set; | Navigation property to the send attempt results. |

---

## EmailRecipientLog

**Namespace:** `JC.Communication.Logging.Models.Email`

Persisted log entry for an email recipient, categorised by `RecipientLogType`. Extends `AuditModel`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for the recipient log entry. |
| `EmailLogId` | `string` | — | get; set; | Foreign key to the parent `EmailLog`. |
| `EmailLog` | `EmailLog` | — | get; set; | Navigation property to the parent email log entry. |
| `Address` | `string` | — | get; set; | The recipient's email address. Required. Max length 256. |
| `DisplayName` | `string?` | — | get; set; | The recipient's display name, if provided. |
| `RecipientLogType` | `RecipientLogType` | `To` | get; set; | The type of recipient (To, CC, or BCC). Required. |

### Constructors

#### EmailRecipientLog()

Parameterless constructor for EF Core.

#### EmailRecipientLog(EmailRecipient recipient)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `recipient` | `EmailRecipient` | — | The email recipient to log. |

Sets `Address` and `DisplayName` from the recipient.

#### EmailRecipientLog(string emailLogId, EmailRecipient recipient)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `emailLogId` | `string` | — | The parent email log ID. |
| `recipient` | `EmailRecipient` | — | The email recipient to log. |

Sets `EmailLogId`, `Address`, and `DisplayName`.

#### EmailRecipientLog(EmailRecipient recipient, RecipientLogType logType)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `recipient` | `EmailRecipient` | — | The email recipient to log. |
| `logType` | `RecipientLogType` | — | The recipient type (To, CC, or BCC). |

Sets `Address`, `DisplayName`, and `RecipientLogType`.

#### EmailRecipientLog(string emailLogId, EmailRecipient recipient, RecipientLogType logType)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `emailLogId` | `string` | — | The parent email log ID. |
| `recipient` | `EmailRecipient` | — | The email recipient to log. |
| `logType` | `RecipientLogType` | — | The recipient type (To, CC, or BCC). |

Sets `EmailLogId`, `Address`, `DisplayName`, and `RecipientLogType`.

---

## RecipientLogType

**Namespace:** `JC.Communication.Logging.Models.Email`

Enum indicating the type of email recipient for logging purposes.

| Member | Value | Description |
|--------|-------|-------------|
| `To` | `0` | A primary (To) recipient. |
| `Cc` | `1` | A carbon copy (CC) recipient. |
| `Bcc` | `2` | A blind carbon copy (BCC) recipient. |

---

## EmailContentLog

**Namespace:** `JC.Communication.Logging.Models.Email`

Persisted log entry for email body content. Only created when `FullLog` is used. One-to-one relationship with `EmailLog`. Extends `AuditModel`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for the content log entry. |
| `EmailLogId` | `string` | — | get; set; | Foreign key to the parent `EmailLog`. |
| `EmailLog` | `EmailLog` | — | get; set; | Navigation property to the parent email log entry. |
| `PlainBody` | `string` | — | get; set; | The plain text body of the email. Required. |
| `HtmlBodyRaw` | `string?` | — | get; set; | The raw HTML body. `null` if the HTML body was identical to the plain body. |
| `HtmlBody` | `string` | — | get (not mapped) | Resolved HTML body. Returns `HtmlBodyRaw` if set, otherwise falls back to `PlainBody`. Not persisted to the database. |

---

## EmailSentLog

**Namespace:** `JC.Communication.Logging.Models.Email`

Persisted log entry for an email send attempt result. Linked to an `EmailLog`. Multiple entries per email log support retry scenarios. Extends `AuditModel`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for the send result log entry. |
| `EmailLogId` | `string` | — | get; set; | Foreign key to the parent `EmailLog`. |
| `EmailLog` | `EmailLog` | — | get; set; | Navigation property to the parent email log entry. |
| `Succeeded` | `bool` | — | get; set; | Whether the send attempt succeeded. |
| `Provider` | `EmailProvider` | — | get; set; | The email provider that handled the send attempt. Required. |
| `SentAtUtc` | `DateTime` | — | get; set; | UTC timestamp of the send attempt. Required. |
| `ServerResponse` | `string?` | — | get; set; | The SMTP server response string on success. `null` on failure or if not available. |
| `ErrorMessage` | `string?` | — | get; set; | The error message if the send failed. `null` on success. |

### Constructors

#### EmailSentLog()

Parameterless constructor for EF Core.

#### EmailSentLog(EmailSendResult result)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `result` | `EmailSendResult` | — | The send result to log. |

Copies `Succeeded`, `Provider`, `SentAtUtc`, `ServerResponse`, and `ErrorMessage` from the result.

#### EmailSentLog(string emailLogId, EmailSendResult result)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `emailLogId` | `string` | — | The parent email log ID. |
| `result` | `EmailSendResult` | — | The send result to log. |

Sets `EmailLogId` and copies all properties from the result.
