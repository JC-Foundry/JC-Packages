using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

/// <summary>
/// Read-only projection of a <see cref="ChatMessage"/> for consumption by the UI or API layer.
/// </summary>
public class MessageModel
{
    /// <summary>Gets the unique identifier of the message.</summary>
    public string MessageId { get; }

    /// <summary>Gets the ID of the thread this message belongs to.</summary>
    public string ThreadId { get; }

    /// <summary>Gets the message content.</summary>
    public string Message { get; }

    /// <summary>Gets the user ID of the message sender.</summary>
    public string SenderUserId { get; }

    /// <summary>Gets the UTC timestamp when the message was sent.</summary>
    public DateTime SentAtUtc { get; }

    /// <summary>
    /// Projects a <see cref="ChatMessage"/> entity into a read-only message model.
    /// </summary>
    /// <param name="message">The message entity to project.</param>
    public MessageModel(ChatMessage message)
    {
        MessageId = message.Id;
        ThreadId = message.ThreadId;
        Message = message.Message;
        SenderUserId = message.SenderUserId;
        SentAtUtc = message.SentAtUtc;
    }
}