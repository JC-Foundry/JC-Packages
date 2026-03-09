using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;

namespace JC.Communication.Logging.Models.Email;

public class EmailContentLog : AuditModel
{
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    public string EmailLogId { get; set; }
    [ForeignKey(nameof(EmailLogId))]
    public EmailLog EmailLog { get; set; }
    
    [Required]
    public string PlainBody { get; set; }
    public string? HtmlBodyRaw { get; set; }
    
    [NotMapped]
    public string HtmlBody => string.IsNullOrEmpty(HtmlBodyRaw) ? PlainBody : HtmlBodyRaw;
}