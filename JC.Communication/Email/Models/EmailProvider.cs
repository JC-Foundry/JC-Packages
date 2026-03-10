namespace JC.Communication.Email.Models;

public enum EmailProvider
{
    /// <summary>
    /// Outputs email to logger using log level set. Intended for development and testing.
    /// Always outputs email body content to the application logger regardless of <see cref="Options.EmailLoggingMode"/>.
    /// </summary>
    Console = -1,
    
    /// <summary>
    /// Sends email using OAuth authentication and Microsoft SMTP relay.
    /// The from address must correspond to a mailbox the Azure AD app has "Send As" permission for.
    /// </summary>
    Microsoft,
    
    /// <summary>
    /// Sends email using SMTP relay with username and password/api key authentication.
    /// </summary>
    SmtpRelay,
    
    /// <summary>
    /// Sends email directly using SMTP protocol.
    /// </summary>
    DirectSmtp
}