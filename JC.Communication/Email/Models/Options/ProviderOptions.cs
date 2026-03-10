namespace JC.Communication.Email.Models.Options;

public class MicrosoftOptions
{
    public const string TenantId = $"{EmailOptions.ConfigPrefix}TenantId";
    public const string ClientId = $"{EmailOptions.ConfigPrefix}ClientId";
    public const string ClientSecret = $"{EmailOptions.ConfigPrefix}ClientSecret";
}

public class SmtpRelayOptions
{
    public const string Username = $"{EmailOptions.ConfigPrefix}Username";
    
    //'Password', 'ApiKey', and 'Secret' config keys are checked, providing flexibility for consumer:
    public const string Password = $"{EmailOptions.ConfigPrefix}Password";
    public const string ApiKey = $"{EmailOptions.ConfigPrefix}ApiKey";
    public const string Secret = $"{EmailOptions.ConfigPrefix}Secret";
}