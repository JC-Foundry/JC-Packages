using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

public class MessageModel
{
    public string MessageId { get; }
    public string ThreadId { get; }
    
    public string Message { get; }
    public string SenderUserId { get; }
    public DateTime SentAtUtc { get; }

    public MessageModel(ChatMessage message)
    {
        MessageId = message.Id;
        ThreadId = message.ThreadId;
        Message = message.Message;
        SenderUserId = message.SenderUserId;
        SentAtUtc = message.SentAtUtc;
    }
}