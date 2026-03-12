using JC.Communication.Messaging.Models;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Communication.Messaging.Models.Options;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JC.Communication.Messaging.Services;

public class MessagingValidationService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly MessagingOptions _options;

    public MessagingValidationService(IRepositoryManager repos,
        IOptions<MessagingOptions> options,
        IUserInfo userInfo)
    {
        _repos = repos;
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


    internal async Task<bool> CheckForDefaultChat(IEnumerable<string> participantUserIds)
    {
        var participantIdList = participantUserIds.ToList();
        if(!participantIdList.Contains(_userInfo.UserId))
            participantIdList.Add(_userInfo.UserId);

        return await _repos.GetRepository<ChatThread>()
            .AsQueryable().AnyAsync(t => t.IsDefaultThread
                                         && t.Participants.Count == participantIdList.Count
                                         && t.Participants.All(p =>
                                             participantIdList.Contains(p.UserId)));
    }

    internal bool IsThreadDirectMessage(List<ChatParticipant> participants)
        => (participants.Count == 2 && participants.Select(p => p.UserId).Contains(_userInfo.UserId)) 
           || participants.Count == 1;
    

    internal async Task<ChatThreadValidationResponse> ValidateAndPrepareChatThread(ChatThread thread, List<ChatParticipant> participants,
        string? errors = null)
    {
        if (string.IsNullOrWhiteSpace(thread.Name))
            errors = AppendError(errors, "Chat name is required.");
        
        thread.IsGroupThread = !IsThreadDirectMessage(participants);
        thread.IsDefaultThread = !await CheckForDefaultChat(participants.Select(p => p.UserId));
        
        return string.IsNullOrEmpty(errors)
            ? new ChatThreadValidationResponse(thread)
            : new ChatThreadValidationResponse(errors);
    }

    internal ChatThreadValidationResponse ValidateChatThread(ChatThread thread, ChatThread updatedThread, string? errors = null)
    {
        if(thread.IsDefaultThread != updatedThread.IsDefaultThread)
            errors = AppendError(errors, "You cannot change the default chat state.");
        
        if(thread.IsGroupThread != updatedThread.IsGroupThread)
            errors = AppendError(errors, "You cannot change whether the chat is a group chat.");
        
        return string.IsNullOrEmpty(errors)
            ? new ChatThreadValidationResponse(thread)
            : new ChatThreadValidationResponse(errors);
    }
    

    internal ParticipantValidationResponse ValidateAndPrepareParticipants(string threadId, List<ChatParticipant> participants, 
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
        
        if(participants.Count > 2 && _options.DisableGroups)
            errors = AppendError(errors, "Groups are disabled.");
        
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
            ? new ParticipantValidationResponse(participants)
            : new ParticipantValidationResponse(errors);
    }
}