# JC.Communication: Email — Guide

Covers sending emails, constructing messages, composing branded HTML bodies, handling results, validation behaviour, and logging. See [Email setup](Email-Setup.md) for registration and configuration.

## Sending emails

### Using the default from address

Inject `IEmailService` and call the simple `SendAsync` overload. The from address is read from `Communication:Email:DefaultFromAddress` in your configuration:

```csharp
public class NotificationService(IEmailService emailService)
{
    public async Task<bool> SendWelcomeEmailAsync(string recipientEmail, string recipientName)
    {
        var recipients = new[]
        {
            new EmailRecipient(recipientEmail, recipientName)
        };

        var result = await emailService.SendAsync(
            recipients,
            "Welcome to Our Platform",
            "Thank you for signing up. We're glad to have you on board.");

        return result.Succeeded;
    }
}
```

This overload throws `InvalidOperationException` if `DefaultFromAddress` is not configured. Startup validation catches this for all providers, so you will not encounter the exception at runtime unless configuration changes after startup.

### Using a custom from address

When you need control over the sender address, construct an `EmailMessage` directly:

```csharp
public async Task<EmailSendResult> SendFromCustomAddressAsync(string fromAddress,
    string recipientEmail, string subject, string body)
{
    var message = new EmailMessage(
        fromAddress,
        body,
        subject,
        new EmailRecipient(recipientEmail));

    return await emailService.SendAsync(message);
}
```

**Microsoft provider:** the from address must correspond to a mailbox or shared mailbox that the Azure AD app has "Send As" permission for. Mismatched addresses are rejected by the Microsoft SMTP relay at runtime.

### HTML and plain text

When sending HTML email, provide both an HTML body and a plain text fallback. Email clients that do not support HTML will display the plain text version:

```csharp
var message = new EmailMessage(
    "noreply@example.com",
    htmlBody: "<h1>Order Confirmed</h1><p>Your order #1234 has been placed.</p>",
    plainBody: "Order Confirmed\n\nYour order #1234 has been placed.",
    subject: "Order Confirmation",
    new EmailRecipient("customer@example.com"));

var result = await emailService.SendAsync(message);
```

If you only provide a plain body (using the single-body constructor), the HTML body is automatically set to the same value:

```csharp
var message = new EmailMessage(
    "noreply@example.com",
    plainBody: "Your password has been reset.",
    subject: "Password Reset",
    new EmailRecipient("user@example.com"));

// message.HtmlBody == message.PlainBody
```

The simple `SendAsync` overload behaves the same way — when `htmlBody` is omitted or `null`, the HTML body defaults to the plain body:

```csharp
// HTML body defaults to plain body
await emailService.SendAsync(recipients, "Subject", "Plain text body");

// HTML body explicitly provided
await emailService.SendAsync(recipients, "Subject", "Plain text body",
    htmlBody: "<p>HTML body</p>");
```

### Multiple recipients, CC, and BCC

The full constructor accepts To, CC, and BCC recipient lists:

```csharp
var to = new List<EmailRecipient>
{
    new("alice@example.com", "Alice"),
    new("bob@example.com", "Bob")
};

var cc = new List<EmailRecipient>
{
    new("manager@example.com", "Team Manager")
};

var bcc = new List<EmailRecipient>
{
    new("audit@example.com")
};

var message = new EmailMessage(
    "noreply@example.com",
    htmlBody: "<p>Monthly report attached.</p>",
    plainBody: "Monthly report attached.",
    subject: "Monthly Report",
    to, cc, bcc);

await emailService.SendAsync(message);
```

The simple `SendAsync` overload also supports CC and BCC:

```csharp
await emailService.SendAsync(
    recipients: new[] { new EmailRecipient("alice@example.com") },
    subject: "Team Update",
    plainBody: "Please review the attached document.",
    ccRecipients: new[] { new EmailRecipient("manager@example.com") },
    bccRecipients: new[] { new EmailRecipient("audit@example.com") });
```

### Display names

`EmailRecipient` accepts an optional display name. When set, the email appears as `"Alice" <alice@example.com>` in the recipient's inbox. When omitted, the address is used as the display name:

```csharp
new EmailRecipient("alice@example.com", "Alice Smith")  // "Alice Smith" <alice@example.com>
new EmailRecipient("alice@example.com")                   // alice@example.com
```

The sender display name is configured separately via `Communication:Email:DefaultFromDisplayName`. If not set, the from address is used as the display name.

### Subjects

If you pass `null` or an empty string as the subject, it defaults to `"NO SUBJECT"`:

```csharp
var message = new EmailMessage("noreply@example.com", "Body text", subject: null,
    new EmailRecipient("user@example.com"));

// message.Subject == "NO SUBJECT"
```

## Composing branded email bodies

`EmailBodyBuilder` produces a matching plain-text body and HTML body from one set of section calls, so the two never drift. The HTML is wrapped in a branded, email-client-safe shell — a gradient header carrying your brand name and an optional caption, then the body. All text you pass is HTML-encoded inside the builder, so call sites can pass raw user input without escaping it themselves.

### Building a body

Inject `DefaultEmailBranding` to get the branding configured in [setup](Email-Setup.md#email-branding), create a builder, chain sections, and call `Build`:

```csharp
public class WelcomeMailer(IEmailService emailService, DefaultEmailBranding branding)
{
    public async Task<EmailSendResult> SendWelcomeAsync(string email, string name, string confirmUrl)
    {
        var (html, plain) = EmailBodyBuilder.Create(branding.Get(), caption: "Welcome")
            .Paragraph($"Hi {name}, thanks for joining.")
            .Paragraph("Confirm your email address to activate your account.")
            .Button("Confirm my account", confirmUrl)
            .Footer("If you didn't create an account, you can safely ignore this email.")
            .Build();

        var message = new EmailMessage(
            "noreply@example.com",
            htmlBody: html,
            plainBody: plain,
            subject: "Welcome",
            new EmailRecipient(email, name));

        return await emailService.SendAsync(message);
    }
}
```

**`Build` returns `(string Html, string Plain)` — in that order.** This matches the `EmailMessage(from, htmlBody, plainBody, ...)` constructor argument order, so the two bodies pass straight through. Deconstructing into `(html, plain)` as above keeps them aligned.

`DefaultEmailBranding.Get()` returns a fresh copy each call, so mutating it for one email never affects the shared branding.

### Sections

Each section method appends to both bodies and returns the builder for chaining:

| Method | HTML | Plain text |
|--------|------|-----------|
| `Paragraph(text, emphasis = false)` | A `<p>` block; with `emphasis: true`, a bold muted label. Blank lines split paragraphs, single newlines become `<br>` | The text, normalised |
| `Quote(text)` | A styled `<blockquote>` | A dash-fenced block |
| `Button(text, url)` | A gradient CTA button, with a plain fallback link beneath | `text: url` |
| `Divider()` | An `<hr>` | A dashed rule |
| `SignOff(text)` | A spaced closing `<p>` | The text |
| `Reference(code)` | A small muted reference line | `Reference: {code}` |
| `Footer(text)` | A small muted footer note | The text |

### Branding without configured defaults

If you only need a brand name and are happy with the default palette, pass a name string instead of an `EmailBranding`:

```csharp
var (html, plain) = EmailBodyBuilder.Create("My Application", caption: "Support")
    .Paragraph("Thanks for getting in touch.")
    .Build();
```

To override individual colours, construct an `EmailBranding` and set palette properties on it — see [Email branding](Email-Setup.md#email-branding).

### Account emails

`AccountEmail` ships ready-made bodies for the ASP.NET Identity account flows, composed via `EmailBodyBuilder` so every confirmation and reset mail shares your branded shell. Each method takes the Identity-generated callback URL and returns `(Html, Plain)`:

```csharp
public class IdentityMailer(IEmailService emailService, DefaultEmailBranding branding)
{
    public Task<EmailSendResult> SendConfirmationAsync(string email, string callbackUrl)
    {
        var (html, plain) = AccountEmail.ConfirmAccount(branding.Get(), callbackUrl);

        var message = new EmailMessage(
            "noreply@example.com",
            htmlBody: html,
            plainBody: plain,
            subject: "Confirm your account",
            new EmailRecipient(email));

        return emailService.SendAsync(message);
    }
}
```

Available flows: `ConfirmAccount`, `ResetPassword`, and `ConfirmEmailChange`. Each has an `EmailBranding` overload (shown above) and a brand-name-string overload that uses the default palette:

```csharp
var (html, plain) = AccountEmail.ResetPassword("My Application", callbackUrl);
```

## Handling results

### Checking success

Every `SendAsync` call returns an `EmailSendResult`. Check `Succeeded` to determine whether the email was sent:

```csharp
var result = await emailService.SendAsync(message);

if (result.Succeeded)
{
    logger.LogInformation("Email sent at {SentAt}. Server response: {Response}",
        result.SentAtUtc, result.ServerResponse);
}
else
{
    logger.LogWarning("Email failed: {Error}", result.ErrorMessage);
}
```

### Result properties

- `Succeeded` — `true` if the email was accepted by the SMTP server (or logged by the console provider).
- `Provider` — which `EmailProvider` handled the send attempt.
- `SentAtUtc` — UTC timestamp of the attempt.
- `ServerResponse` — the SMTP server's response string on success. `null` for the console provider or on failure.
- `ErrorMessage` — the error description on failure. `null` on success.

### Validation failures

If message validation fails, `SendAsync` returns a failed result without attempting to send. The `ErrorMessage` contains all validation errors separated by newlines:

```csharp
var message = new EmailMessage("", "Body", "Subject",
    new EmailRecipient("not-an-email"));

var result = await emailService.SendAsync(message);
// result.Succeeded == false
// result.ErrorMessage contains:
//   "From address is required."
//   "Invalid From address."
//   "Invalid recipient addresses: not-an-email"
```

Both successful and failed attempts (including validation failures) are logged to the database when database logging is enabled.

## Message validation

Every `SendAsync` call validates the message before attempting to send. Validation checks, in order:

1. **From address is required** — empty or whitespace-only from address.
2. **From address must contain '@'** — basic format check.
3. **Email body is required** — empty or whitespace-only plain body.
4. **Invalid recipient addresses** — any To, CC, or BCC address that is empty, whitespace, or missing '@'. All invalid addresses are listed in the error.
5. **Duplicate recipients** — any address appearing more than once across To, CC, and BCC (case-insensitive comparison). All duplicates are listed in the error.

All errors are collected and returned together — the validation does not stop at the first failure.

## Logging

### How logging works

Every send attempt — whether it succeeds, fails, or is rejected by validation — is passed to `EmailLogService`. The log service checks the configured `EmailLoggingMode` and persists the appropriate data within a database transaction.

If the logging transaction fails, the error is logged to the application logger but not thrown. A failed log write does not affect the email send result.

### What gets logged

| Logging mode | What is persisted |
|-------------|-------------------|
| `None` | Nothing — `EmailLogService` returns immediately |
| `ExcludeContent` | From address, subject, all recipients (with type), send result (success/failure, provider, timestamp, server response, error message) |
| `FullLog` | Everything in `ExcludeContent`, plus the plain text body and HTML body. The HTML body is only stored when it differs from the plain body |

See [Email setup — Logging modes](Email-Setup.md#logging-modes) for how to configure the logging mode.

### Console provider and logging

The console provider always outputs the email body (plain text) to the application logger, regardless of the logging mode. The logging mode only controls what is persisted to the database. If email body content is sensitive, be aware that console output will still contain it even with `ExcludeContent`.

## Next steps

- [Email setup](Email-Setup.md) — registration, providers, and configuration options.
- [Email API reference](Email-API.md)
