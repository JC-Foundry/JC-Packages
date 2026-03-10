namespace JC.Communication.Email.Models;

public enum EmailProvider
{
    /// <summary>
    /// Outputs email to logger using log level set.
    /// </summary>
    Console = -1,
    
    /// <summary>
    /// Sends email using OAuth authentication and Microsoft SMTP relay.
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