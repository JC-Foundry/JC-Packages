using JC.FileStorage.Data.DataMappings;
using Microsoft.EntityFrameworkCore;

namespace JC.FileStorage.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> providing JC.FileStorage entity configuration.
/// </summary>
public static class DataExtensions
{
    /// <summary>
    /// Applies all JC.FileStorage entity mappings to the model builder.
    /// Call this from <c>OnModelCreating</c> in the consuming application's DbContext.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyFileStorageMappings(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SavedFileMap());

        return modelBuilder;
    }
}