using JC.Communication.Email.Models;

namespace JC.Communication.Email.Helpers;

public static class AccountEmail
{
    /// <summary>Account email-confirmation link (registration, resend, external login, re-verify).</summary>
    public static (string Html, string Plain) ConfirmAccount(EmailBranding branding, string callbackUrl)
        => ConfirmAccount(EmailBodyBuilder.Create(branding, ConfirmAccountCaption), callbackUrl);
            

    public static (string Html, string Plain) ConfirmAccount(string brandName, string callbackUrl)
        => ConfirmAccount(EmailBodyBuilder.Create(brandName, ConfirmAccountCaption), callbackUrl);

    private const string ConfirmAccountCaption = "Confirm your account";
    private static (string Html, string Plain) ConfirmAccount(EmailBodyBuilder builder, string callbackUrl)
        => builder.Paragraph("Welcome! Please confirm your email address to activate your account.")
            .Button("Confirm my account", callbackUrl)
            .Footer("If you didn't create an account, you can safely ignore this email.")
            .Build();

    
    /// <summary>Password-reset link.</summary>
    public static (string Html, string Plain) ResetPassword(EmailBranding branding, string callbackUrl)
        => ResetPassword(EmailBodyBuilder.Create(branding, PasswordResetCaption), callbackUrl);
    
    public static (string Html, string Plain) ResetPassword(string brandName, string callbackUrl) 
        => ResetPassword(EmailBodyBuilder.Create(brandName, PasswordResetCaption), callbackUrl);
    
    private const string PasswordResetCaption = "Reset your password";
    private static (string Html, string Plain) ResetPassword(EmailBodyBuilder builder, string callbackUrl) 
        => builder.Paragraph("We received a request to reset your password. Use the button below to choose a new one.")
            .Button("Reset my password", callbackUrl)
            .Footer("If you didn't request this, you can safely ignore this email — your password won't change.")
            .Build();
    
    
    
    /// <summary>Confirmation link sent to a user's <em>new</em> address when they change their email.</summary>
    public static (string Html, string Plain) ConfirmEmailChange(EmailBranding branding, string callbackUrl)
        => ConfirmEmailChange(EmailBodyBuilder.Create(branding, EmailChangeCaption), callbackUrl);
    
    public static (string Html, string Plain) ConfirmEmailChange(string brandName, string callbackUrl) 
        => ConfirmEmailChange(EmailBodyBuilder.Create(brandName, EmailChangeCaption), callbackUrl);
    
    private const string EmailChangeCaption = "Confirm your new email";
    private static (string Html, string Plain) ConfirmEmailChange(EmailBodyBuilder builder, string callbackUrl)
        => builder.Paragraph("Please confirm this is your new email address to finish updating your account.")
            .Button("Confirm this email", callbackUrl)
            .Footer("If you didn't request an email change, please contact support.")
            .Build();
}