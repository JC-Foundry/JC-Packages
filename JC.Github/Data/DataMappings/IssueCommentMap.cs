using JC.Core.Data.DataMappings;
using JC.Github.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JC.Github.Data.DataMappings;

public class IssueCommentMap : IEntityTypeConfiguration<IssueComment>
{
    public void Configure(EntityTypeBuilder<IssueComment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(36);

        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.Author).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.IssueNumber);
        builder.HasIndex(e => e.CommentId).IsUnique();
    }
}
