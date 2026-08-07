using JC.Communication.Logging.Models.Email;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Logging.Data.DataMappings.Email;

public class EmailAttachmentLogMap : IEntityTypeConfiguration<EmailAttachmentLog>
{
    public void Configure(EntityTypeBuilder<EmailAttachmentLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);

        builder.Property(e => e.FileName).IsRequired().HasMaxLength(512);
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(256);

        builder.Property(e => e.EmailLogId).HasMaxLength(36);

        // Not unique: an email may carry any number of attachments.
        builder.HasIndex(e => e.EmailLogId);

        builder = LogModelMapping<EmailAttachmentLog>.MapLogModel(builder);
    }
}
