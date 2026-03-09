using JC.Communication.Logging.Models.Email;
using Microsoft.EntityFrameworkCore;

namespace JC.Communication.Logging.Data;

public interface IEmailDbContext
{
    DbSet<EmailLog> EmailLogs { get; set; }
    DbSet<EmailRecipientLog> EmailRecipientLogs { get; set; }
    DbSet<EmailContentLog> EmailContentLogs { get; set; }
    DbSet<EmailSentLog> EmailSentLogs { get; set; }
}