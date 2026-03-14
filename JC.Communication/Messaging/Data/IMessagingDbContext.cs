using JC.Communication.Messaging.Models.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace JC.Communication.Messaging.Data;

/// <summary>
/// Defines the DbSet properties required by the messaging module.
/// Implement this interface on the consuming application's <see cref="DbContext"/>
/// to provide the necessary tables for chat threads, messages, participants, and metadata.
/// </summary>
public interface IMessagingDbContext
{
    /// <summary>Gets or sets the chat threads table.</summary>
    DbSet<ChatThread> ChatThreads { get; set; }

    /// <summary>Gets or sets the chat messages table.</summary>
    DbSet<ChatMessage> ChatMessages { get; set; }

    /// <summary>Gets or sets the chat participants table.</summary>
    DbSet<ChatParticipant> ChatParticipants { get; set; }

    /// <summary>Gets or sets the chat metadata table.</summary>
    DbSet<ChatMetadata> ChatMetadata { get; set; }
}