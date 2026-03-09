namespace JC.Communication.Email.Models.Options;

public class MicrosoftOptions
{
    public const string TenantId = $"{EmailOptions.ConfigPrefix}TenantId";
    public const string ClientId = $"{EmailOptions.ConfigPrefix}ClientId";
    public const string ClientSecret = $"{EmailOptions.ConfigPrefix}ClientSecret";
}