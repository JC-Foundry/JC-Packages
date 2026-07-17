using JC.Core.Data.DataMappings;
using JC.FileStorage.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.FileStorage.Data.DataMappings;

public class SavedFileMap : IEntityTypeConfiguration<SavedFile>
{
    public void Configure(EntityTypeBuilder<SavedFile> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasMaxLength(36);

        builder.Property(f => f.TenantId).HasMaxLength(36);
        builder.Property(f => f.FileName).IsRequired().HasMaxLength(256);
        builder.Property(f => f.Extension).IsRequired().HasMaxLength(64);
        builder.Property(f => f.FolderName).IsRequired().HasMaxLength(256);
        
        builder.HasOne(f => f.Tenant)
            .WithMany()
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.SetNull);

        //Covers the lookup every read, save and delete goes through
        builder.HasIndex(f => new { f.TenantId, f.FolderName, f.FileName });

        builder = AuditModelMapping<SavedFile>.MapAuditModel(builder);
    }
}