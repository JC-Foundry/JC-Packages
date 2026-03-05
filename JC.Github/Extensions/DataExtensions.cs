using JC.Github.Data.DataMappings;
using Microsoft.EntityFrameworkCore;

namespace JC.Github.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> providing JC.Github entity configuration.
/// </summary>
public static class DataExtensions
{
    /// <summary>
    /// Applies all JC.Github entity mappings to the model builder.
    /// Call this from <c>OnModelCreating</c> in the consuming application's DbContext.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyGithubMappings(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ReportedIssueMap());
        modelBuilder.ApplyConfiguration(new IssueCommentMap());

        return modelBuilder;
    }
}
