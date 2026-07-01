namespace JC.Communication.Email.Models;

public class EmailBranding
{
    public string BrandName { get; set; }

    public EmailBranding(string brandName)
    {
        BrandName = brandName.Trim();
    }

    public EmailBranding(EmailBranding branding)
    {
        BrandName = branding.BrandName;
        BrandStart = branding.BrandStart;
        BrandEnd = branding.BrandEnd;
        HeaderCaption = branding.HeaderCaption;
        PageBg = branding.PageBg;
        CardBg = branding.CardBg;
        TextColour = branding.TextColour;
        SubtleColour = branding.SubtleColour;
        MutedColour = branding.MutedColour;
        RuleColour = branding.RuleColour;
        QuoteBg = branding.QuoteBg;
        QuoteBorder = branding.QuoteBorder;
        PlainRule = branding.PlainRule;
    }
    
    // Brand palette
    public string BrandStart { get; set; } = "#0d6efd";
    public string BrandEnd { get; set; } = "#0dcaf0";
    public string HeaderCaption { get; set; } = "#eaf6f9";
    public string PageBg { get; set; } = "#f0f2f5";
    public string CardBg { get; set; } = "#ffffff";
    public string TextColour { get; set; } = "#1f2933";
    public string SubtleColour { get; set; } = "#52606d";
    public string MutedColour { get; set; } = "#7b8794";
    public string RuleColour { get; set; } = "#d9dee3";
    public string QuoteBg { get; set; } = "#f5f7fa";
    public string QuoteBorder { get; set; } = "#c8d0d8";
    
    public string PlainRule { get; set; } = "----------------------------------------";
}