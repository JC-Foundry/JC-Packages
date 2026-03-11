using JC.Communication.Logging.Models.Email;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Logging.Data.DataMappings.Email;

public class EmailSentLogMap : IEntityTypeConfiguration<EmailSentLog>
{
    public void Configure(EntityTypeBuilder<EmailSentLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);

        builder.Property(e => e.Provider).IsRequired().HasConversion<int>();
        builder.Property(e => e.SentAtUtc).IsRequired();

        builder.Property(e => e.EmailLogId).HasMaxLength(36);
        builder.HasIndex(e => e.EmailLogId);
        
        builder = LogModelMapping<EmailSentLog>.MapLogModel(builder);
    }
}
