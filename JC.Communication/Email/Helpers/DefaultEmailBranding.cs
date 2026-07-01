using JC.Communication.Email.Models;

namespace JC.Communication.Email.Helpers;

public class DefaultEmailBranding
{
    private readonly EmailBranding _branding;

    public DefaultEmailBranding(EmailBranding branding)
    {
        _branding = branding;
    }
    
    public EmailBranding Get() => new(_branding);
}