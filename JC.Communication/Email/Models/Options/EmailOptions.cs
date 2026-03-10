using System.Security.Authentication;
using Microsoft.Extensions.Logging;

namespace JC.Communication.Email.Models.Options;

/// <summary>
/// Configuration options for email communication. Configures the provider, logging mode,
/// and provider-specific settings such as SMTP host, port, and authentication.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// The email provider to use for sending. Defaults to <see cref="EmailProvider.Microsoft"/>.
    /// </summary>
    public EmailProvider Provider { get; set; } = EmailProvider.Microsoft;

    /// <summary>
    /// The logging mode for email communication. Defaults to <see cref="EmailLoggingMode.ExcludeContent"/>.
    /// Requires the generic <c>AddEmail&lt;TContext&gt;</c> overload to be used if not set to <see cref="EmailLoggingMode.None"/>.
    /// </summary>
    public EmailLoggingMode LoggingMode { get; set; } = EmailLoggingMode.ExcludeContent;

    /// <summary>
    /// Timeout in milliseconds for email send operations. Defaults to 30,000ms (30 seconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 30_000;


    //Console Options:

    /// <summary>
    /// The log level used by the console email provider when outputting email content. Defaults to <see cref="LogLevel.Information"/>.
    /// Ignored for all other provider types.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;


    //SMTP Relay Options:

    /// <summary>
    /// The SMTP server hostname. Required for <see cref="EmailProvider.SmtpRelay"/> and <see cref="EmailProvider.DirectSmtp"/> providers.
    /// Ignored for all other provider types.
    /// </summary>
    public string Host { get; set; } = "smtp.office365.com";

    /// <summary>
    /// The SMTP server port. Defaults to 587.
    /// Ignored for Console provider.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Whether to use SSL/TLS when connecting to the SMTP server. Defaults to true.
    /// Must be true for the Microsoft provider. Ignored for Console provider.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// The SSL protocol to use when connecting. Defaults to <see cref="SslProtocols.None"/> (lets the OS negotiate).
    /// Ignored for Console provider.
    /// </summary>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>
    /// Whether a username is required for SMTP relay authentication. Defaults to true.
    /// When false, authentication uses only the secret with "apikey" as the username, or skips authentication entirely if no secret is configured.
    /// Only applies to the <see cref="EmailProvider.SmtpRelay"/> provider.
    /// </summary>
    public bool UsernameRequired { get; set; } = true;


    //Configuration Keys:

    /// <summary>
    /// Configuration key prefix for all email settings: <c>Communication:Email:</c>.
    /// </summary>
    internal const string ConfigPrefix = "Communication:Email:";

    /// <summary>
    /// Configuration key for the default sender email address. Key: <c>Communication:Email:DefaultFromAddress</c>.
    /// </summary>
    public const string ConfigFromAddress = $"{ConfigPrefix}DefaultFromAddress";

    /// <summary>
    /// Configuration key for the default sender display name. Key: <c>Communication:Email:DefaultFromDisplayName</c>.
    /// </summary>
    public const string ConfigFromDisplayName = $"{ConfigPrefix}DefaultFromDisplayName";
}

/// <summary>
/// Determines how email send attempts are logged to the database.
/// </summary>
public enum EmailLoggingMode
{
    /// <summary>
    /// No logging. Email send attempts are not persisted.
    /// </summary>
    None,

    /// <summary>
    /// Logs metadata only (sender, recipients, subject, send result) without email body content.
    /// </summary>
    ExcludeContent,

    /// <summary>
    /// Logs all email data including body content.
    /// </summary>
    FullLog
}
