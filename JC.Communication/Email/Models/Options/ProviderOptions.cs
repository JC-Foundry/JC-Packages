namespace JC.Communication.Email.Models.Options;

/// <summary>
/// Configuration keys for the Microsoft OAuth email provider.
/// Values are read from <c>Communication:Email:</c> configuration section.
/// </summary>
public class MicrosoftOptions
{
    /// <summary>
    /// Configuration key for the Azure AD tenant ID. Key: <c>Communication:Email:TenantId</c>.
    /// </summary>
    public const string TenantId = $"{EmailOptions.ConfigPrefix}TenantId";

    /// <summary>
    /// Configuration key for the Azure AD application (client) ID. Key: <c>Communication:Email:ClientId</c>.
    /// </summary>
    public const string ClientId = $"{EmailOptions.ConfigPrefix}ClientId";

    /// <summary>
    /// Configuration key for the Azure AD client secret. Key: <c>Communication:Email:ClientSecret</c>.
    /// </summary>
    public const string ClientSecret = $"{EmailOptions.ConfigPrefix}ClientSecret";
}

/// <summary>
/// Configuration keys for the SMTP relay email provider.
/// Values are read from <c>Communication:Email:</c> configuration section.
/// Multiple secret key names are supported (Password, ApiKey, Secret) to provide flexibility for the consumer.
/// </summary>
public class SmtpRelayOptions
{
    /// <summary>
    /// Configuration key for the SMTP username. Key: <c>Communication:Email:Username</c>.
    /// Only required when <see cref="EmailOptions.UsernameRequired"/> is true.
    /// </summary>
    public const string Username = $"{EmailOptions.ConfigPrefix}Username";

    /// <summary>
    /// Configuration key for the SMTP password. Key: <c>Communication:Email:Password</c>.
    /// Checked first when resolving the authentication secret.
    /// </summary>
    public const string Password = $"{EmailOptions.ConfigPrefix}Password";

    /// <summary>
    /// Configuration key for the SMTP API key. Key: <c>Communication:Email:ApiKey</c>.
    /// Checked second when resolving the authentication secret.
    /// </summary>
    public const string ApiKey = $"{EmailOptions.ConfigPrefix}ApiKey";

    /// <summary>
    /// Configuration key for the SMTP secret. Key: <c>Communication:Email:Secret</c>.
    /// Checked last when resolving the authentication secret.
    /// </summary>
    public const string Secret = $"{EmailOptions.ConfigPrefix}Secret";
}
