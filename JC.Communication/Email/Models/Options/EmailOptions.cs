using System.Security.Authentication;

namespace JC.Communication.Email.Models.Options;

public class EmailOptions
{
    public EmailProvider Provider { get; set; } = EmailProvider.Microsoft;
    public EmailLoggingMode LoggingMode { get; set; } = EmailLoggingMode.ExcludeContent;

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    public int TimeoutMs { get; set; } = 30_000;

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