using JC.Github.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Github.Data.DataMappings;

public class ReportedIssueMap : IEntityTypeConfiguration<ReportedIssue>
{
    public void Configure(EntityTypeBuilder<ReportedIssue> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.Created).IsRequired();

        builder.HasIndex(e => e.ExternalId).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Closed);
    }
}
