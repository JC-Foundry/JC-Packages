using JC.Communication.Logging.Data.DataMappings.Email;
using JC.Communication.Logging.Data.DataMappings.Notifications;
using JC.Communication.Notifications.Data.DataMappings;
using Microsoft.EntityFrameworkCore;

namespace JC.Communication.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> providing JC.Communication entity configuration.
/// </summary>
public static class DataExtensions
{
    /// <summary>
    /// Applies all JC.Communication email logging entity mappings to the model builder.
    /// Call this from <c>OnModelCreating</c> in the consuming application's DbContext.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyEmailMappings(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EmailLogMap());
        modelBuilder.ApplyConfiguration(new EmailRecipientLogMap());
        modelBuilder.ApplyConfiguration(new EmailContentLogMap());
        modelBuilder.ApplyConfiguration(new EmailSentLogMap());

        return modelBuilder;
    }

    /// <summary>
    /// Applies all JC.Communication notification entity mappings to the model builder.
    /// Call this from <c>OnModelCreating</c> in the consuming application's DbContext.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyNotificationMappings(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NotificationMap());
        modelBuilder.ApplyConfiguration(new NotificationStyleMap());
        modelBuilder.ApplyConfiguration(new NotificationLogMap());

        return modelBuilder;
    }
}
