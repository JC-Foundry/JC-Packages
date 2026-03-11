using JC.Communication.Logging.Models.Email;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Logging.Data.DataMappings.Email;

public class EmailLogMap : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);

        builder.Property(e => e.FromAddress).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(1024);

        builder.HasMany(e => e.EmailRecipientLogs)
            .WithOne(r => r.EmailLog)
            .HasForeignKey(r => r.EmailLogId);

        builder.HasOne(e => e.EmailContentLog)
            .WithOne(c => c.EmailLog)
            .HasForeignKey<EmailContentLog>(c => c.EmailLogId);

        builder.HasMany(e => e.EmailSentLogs)
            .WithOne(s => s.EmailLog)
            .HasForeignKey(s => s.EmailLogId);
        
        builder = LogModelMapping<EmailLog>.MapLogModel(builder);
    }
}
