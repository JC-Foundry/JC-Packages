using JC.Communication.Logging.Models.Email;
using Microsoft.EntityFrameworkCore;

namespace JC.Communication.Logging.Data;

/// <summary>
/// Database context interface for email logging. Implement this on your application's DbContext
/// to enable email log persistence via the generic <c>AddEmail&lt;TContext&gt;</c> registration.
/// </summary>
public interface IEmailDbContext
{
    /// <summary>
    /// The email log entries containing sender and subject metadata.
    /// </summary>
    DbSet<EmailLog> EmailLogs { get; set; }

    /// <summary>
    /// The recipient log entries linked to email logs, including To, CC, and BCC recipients.
    /// </summary>
    DbSet<EmailRecipientLog> EmailRecipientLogs { get; set; }

    /// <summary>
    /// The content log entries containing email body content. Only populated when <see cref="Email.Models.Options.EmailLoggingMode.FullLog"/> is used.
    /// </summary>
    DbSet<EmailContentLog> EmailContentLogs { get; set; }

    /// <summary>
    /// The send result log entries containing success/failure status, provider, and error details.
    /// </summary>
    DbSet<EmailSentLog> EmailSentLogs { get; set; }
}
