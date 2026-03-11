using JC.Communication.Messaging.Models;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Communication.Messaging.Models.Options;
using JC.Core.Models;
using Microsoft.Extensions.Options;

namespace JC.Communication.Messaging.Services;

public class MessagingValidationService
{
    private readonly IUserInfo _userInfo;
    private readonly MessagingOptions _options;

    public MessagingValidationService(IOptions<MessagingOptions> options,
        IUserInfo userInfo)
    {
        _userInfo = userInfo;
        _options = options.Value;
    }

    private string AppendError(string? errors, string err)
    {
        if (!string.IsNullOrWhiteSpace(errors))
            errors += Environment.NewLine;

        errors += err;
        return errors;
    }
    

    internal ParticipantValidationResponse<ChatParticipant> ValidateAndPrepareParticipants(string threadId, List<ChatParticipant> participants, 
        string? errors = null)
    {
        var containsUser = participants.Select(pt => pt.UserId).Contains(_userInfo.UserId);
        switch (containsUser)
        {
            case false:
                participants.Add(new ChatParticipant(threadId, _userInfo.UserId));
                break;
            case true when participants.Count == 1:
                errors = AppendError(errors, "You must include a participant that is not yourself");
                break;
        }

        var existingParticipants = new HashSet<string>();
        foreach (var p in participants)
        {
            p.ThreadId = threadId;
            p.CanSeeHistory = _options.ParticipantsSeeChatHistory;
            
            if (!existingParticipants.TryGetValue(p.UserId, out _))
            {
                existingParticipants.Add(p.UserId);
                continue;
            }
            
            errors = AppendError(errors, "Duplicate participants exist.");
            break;
        }

        return string.IsNullOrEmpty(errors)
            ? new ParticipantValidationResponse<ChatParticipant>(participants)
            : new ParticipantValidationResponse<ChatParticipant>(errors);
    }
}