using System.Security.Authentication;
using Microsoft.Extensions.Logging;

namespace JC.Communication.Email.Models.Options;

public class EmailOptions
{
    /// <summary>
    /// Required provider for email communication.
    /// </summary>
    public EmailProvider Provider { get; set; } = EmailProvider.Microsoft;
    
    /// <summary>
    /// Required logging mode for email communication.
    /// </summary>
    public EmailLoggingMode LoggingMode { get; set; } = EmailLoggingMode.ExcludeContent;
    
    /// <summary>
    /// Optional timeout for email communication.
    /// </summary>
    public int TimeoutMs { get; set; } = 30_000;

    
    //Console Options:
    
    /// <summary>
    /// Console log level for email communication. Ignored for all other types of providers.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    
    
    //SMTP Relay Options:
    
    /// <summary>
    /// Host for SMTP relay. Ignored for all other types of providers.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Port for SMTP relay. Ignored for all other types of providers.
    /// </summary>
    public int Port { get; set; } = 587;
    
    /// <summary>
    /// SSL for SMTP relay. Ignored for all other types of providers.
    /// </summary>
    public bool EnableSsl { get; set; } = true;
    
    /// <summary>
    /// SSL Protocol for SMTP relay. Ignored for all other types of providers.
    /// </summary>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>
    /// Whether the Username is required for SMTP relay. Ignored for all other types of providers.
    /// </summary>
    public bool UsernameRequired { get; set; } = true;
    

    //Configuration Keys:
    internal const string ConfigPrefix = "Communication:Email:";
    public const string ConfigFromAddress = $"{ConfigPrefix}DefaultFromAddress";
    public const string ConfigFromDisplayName = $"{ConfigPrefix}DefaultFromDisplayName";
}

public enum EmailLoggingMode
{
    None,
    ExcludeContent,
    FullLog
}