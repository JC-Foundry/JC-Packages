using JC.Communication.Messaging.Models.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace JC.Communication.Messaging.Data;

public interface IMessagingDbContext
{
    DbSet<ChatThread> ChatThreads { get; set; }
    DbSet<ChatMessage> ChatMessages { get; set; }
    DbSet<ChatParticipant> ChatParticipants { get; set; }
    DbSet<ChatMetadata> ChatMetadata { get; set; }
}