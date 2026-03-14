using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Data.DataMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Communication.Messaging.Data.DataMappings;

public class ChatMetadataMap : IEntityTypeConfiguration<ChatMetadata>
{
    public void Configure(EntityTypeBuilder<ChatMetadata> builder)
    {
        builder.HasKey(m => m.ThreadId);
        builder.Property(m => m.ThreadId).HasMaxLength(36);

        builder.Property(m => m.Icon).HasMaxLength(256);
        builder.Property(m => m.ImgPath).HasMaxLength(512);
        builder.Property(m => m.ColourHex).HasMaxLength(7);
        builder.Property(m => m.ColourRgb).HasMaxLength(16);

        builder.Ignore(m => m.IsColourHex);
        builder.Ignore(m => m.IsColourRgb);

        builder = AuditModelMapping<ChatMetadata>.MapAuditModel(builder);
    }
}
