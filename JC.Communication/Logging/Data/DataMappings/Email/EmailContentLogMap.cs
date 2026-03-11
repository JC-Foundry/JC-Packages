using JC.Communication.Logging.Models.Email;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Logging.Data.DataMappings.Email;

public class EmailContentLogMap : IEntityTypeConfiguration<EmailContentLog>
{
    public void Configure(EntityTypeBuilder<EmailContentLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);

        builder.Property(e => e.PlainBody).IsRequired();

        builder.Property(e => e.EmailLogId).HasMaxLength(36);
        builder.HasIndex(e => e.EmailLogId).IsUnique();
        
        builder = LogModelMapping<EmailContentLog>.MapLogModel(builder);
    }
}
