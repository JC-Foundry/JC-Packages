using JC.Communication.Logging.Models.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Logging.Data.DataMappings.Email;

public class EmailRecipientLogMap : IEntityTypeConfiguration<EmailRecipientLog>
{
    public void Configure(EntityTypeBuilder<EmailRecipientLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);

        builder.Property(e => e.Address).IsRequired().HasMaxLength(256);
        builder.Property(e => e.RecipientLogType).IsRequired().HasConversion<int>();

        builder.Property(e => e.EmailLogId).HasMaxLength(36);
        builder.HasIndex(e => e.EmailLogId);
    }
}
