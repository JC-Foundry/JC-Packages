using System.ComponentModel.DataAnnotations;

namespace JC.Communication.Web.Models;

/// <summary>
/// Input model for the contact form tag helper. Bind this to the form POST action
/// to receive email, subject, and message values.
/// </summary>
public class ContactInputModel
{
    /// <summary>Gets or sets the sender's email address.</summary>
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the message subject.</summary>
    [Required(ErrorMessage = "Subject is required.")]
    [MaxLength(256)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gets or sets the message body.</summary>
    [Required(ErrorMessage = "Message is required.")]
    [MaxLength(8192)]
    public string Message { get; set; } = string.Empty;
}
