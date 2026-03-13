using JC.Communication.Messaging.Models;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Communication.Messaging.Models.Options;
using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Messaging.Services;

public class ChatParticipantService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ChatThreadService _threadService;
    private readonly MessagingValidationService _validationService;
    private readonly ILogger<ChatParticipantService> _logger;

    public ChatParticipantService(IRepositoryManager repos,
        IUserInfo userInfo,
        ChatThreadService threadService,
        MessagingValidationService validationService,
        ILogger<ChatParticipantService> logger)
    {
        _repos = repos;
        _userInfo = userInfo;
        _threadService = threadService;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Adds a single participant to a chat thread. Validates the full participant set (existing + new),
    /// restores any previously soft-deleted participants, and adds genuinely new ones within a transaction.
    /// </summary>
    /// <param name="threadId">The ID of the thread to add the participant to.</param>
    /// <param name="userId">The user ID of the participant to add.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ParticipantValidationResponse> TryAddParticipantToChat(string threadId, string userId)
        => await TryAddParticipantToChat(threadId, new ChatParticipant(threadId, userId));

    /// <summary>
    /// Adds a single participant to a chat thread. Validates the full participant set (existing + new),
    /// restores any previously soft-deleted participants, and adds genuinely new ones within a transaction.
    /// </summary>
    /// <param name="threadId">The ID of the thread to add the participant to.</param>
    /// <param name="participant">The participant entity to add.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ParticipantValidationResponse> TryAddParticipantToChat(string threadId, ChatParticipant participant)
        => await TryAddParticipantsToChat(threadId, participant);

    /// <summary>
    /// Adds multiple participants to a chat thread. Validates the full participant set (existing + new),
    /// restores any previously soft-deleted participants, and adds genuinely new ones within a transaction.
    /// </summary>
    /// <param name="threadId">The ID of the thread to add participants to.</param>
    /// <param name="participants">The user IDs of the participants to add.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ParticipantValidationResponse> TryAddParticipantsToChat(string threadId, params IEnumerable<string> participants)
        => await TryAddParticipantsToChat(threadId, participants.Select(p => new ChatParticipant(threadId, p)));

    /// <summary>
    /// Adds multiple participants to a chat thread. Validates the full participant set (existing + new),
    /// restores any previously soft-deleted participants, and adds genuinely new ones within a transaction.
    /// </summary>
    /// <param name="threadId">The ID of the thread to add participants to.</param>
    /// <param name="participantParams">The participant entities to add.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ParticipantValidationResponse> TryAddParticipantsToChat(string threadId,
        params IEnumerable<ChatParticipant> participantParams)
    {
        var thread = await _threadService.GetChatModelById(threadId);
        if(thread == null) 
            return new ParticipantValidationResponse("Cannot find chat thread to add participants to.");
        
        var participants = participantParams.ToList();
        participants.AddRange(thread.Participants.Select(p => new ChatParticipant(threadId, p.UserId)));
        
        var response = _validationService.ValidateAndPrepareParticipants(threadId, participants);
        if(!response.IsValid) return response;

        //Remove the existing participants from the list
        var existingUserIds = thread.Participants.Select(p => p.UserId).ToList();
        participants = response.ValidatedParticipants
            .Where(p => !existingUserIds.Contains(p.UserId))
            .ToList();

        var restoreParticipants = await _repos.GetRepository<ChatParticipant>()
            .AsQueryable().FilterDeleted(DeletedQueryType.OnlyDeleted)
            .Where(p => participants.Any(pt => pt.UserId == p.UserId) 
                        && p.ThreadId == threadId)
            .ToListAsync();
        foreach (var p in restoreParticipants)
        {
            p.JoinedAtUtc = DateTime.UtcNow;
        }
        
        var restoreUserIds = restoreParticipants.Select(r => r.UserId).ToList();
        var addParticipants = participants
            .Where(p => restoreUserIds.All(id => id != p.UserId))
            .ToList();
        
        await _repos.BeginTransactionAsync();
        try
        {
            if(restoreParticipants.Count > 0)
                await _repos.GetRepository<ChatParticipant>()
                    .RestoreRangeAsync(restoreParticipants, saveNow: false);
            
            if(addParticipants.Count > 0)
                await _repos.GetRepository<ChatParticipant>()
                    .AddRangeAsync(addParticipants, saveNow: false);
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return response;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to add participants to chat thread");
            return new ParticipantValidationResponse("Failed to add participants to chat thread");
        }
    }


    /// <summary>
    /// Removes a single participant from a chat thread. The current user cannot remove themselves —
    /// use <see cref="TryLeaveGroupChat"/> instead. Validates the removal set and the post-removal
    /// participant state to ensure the thread remains valid.
    /// </summary>
    /// <param name="threadId">The ID of the thread to remove the participant from.</param>
    /// <param name="userId">The user ID of the participant to remove.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success (with remaining participants) or containing validation errors.</returns>
    public async Task<ParticipantValidationResponse> TryRemoveParticipantFromChat(string threadId, string userId)
        => await TryRemoveParticipantFromChat(threadId, new ChatParticipant(threadId, userId));

    /// <summary>
    /// Removes a single participant from a chat thread. The current user cannot remove themselves —
    /// use <see cref="TryLeaveGroupChat"/> instead. Validates the removal set and the post-removal
    /// participant state to ensure the thread remains valid.
    /// </summary>
    /// <param name="threadId">The ID of the thread to remove the participant from.</param>
    /// <param name="participant">The participant entity to remove.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success (with remaining participants) or containing validation errors.</returns>
    public async Task<ParticipantValidationResponse> TryRemoveParticipantFromChat(string threadId, ChatParticipant participant)
        => await TryRemoveParticipantsFromChat(threadId, participant);

    /// <summary>
    /// Removes multiple participants from a chat thread. The current user cannot remove themselves —
    /// use <see cref="TryLeaveGroupChat"/> instead. Validates the removal set and the post-removal
    /// participant state to ensure the thread remains valid.
    /// </summary>
    /// <param name="threadId">The ID of the thread to remove participants from.</param>
    /// <param name="participants">The user IDs of the participants to remove.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success (with remaining participants) or containing validation errors.</returns>
    public async Task<ParticipantValidationResponse> TryRemoveParticipantsFromChat(string threadId, params IEnumerable<string> participants)
        => await TryRemoveParticipantsFromChat(threadId, participants.Select(p => new ChatParticipant(threadId, p)));

    /// <summary>
    /// Removes multiple participants from a chat thread. The current user cannot remove themselves —
    /// use <see cref="TryLeaveGroupChat"/> instead. Validates the removal set and the post-removal
    /// participant state to ensure the thread remains valid.
    /// </summary>
    /// <param name="threadId">The ID of the thread to remove participants from.</param>
    /// <param name="participantParams">The participant entities to remove.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success (with remaining participants) or containing validation errors.</returns>
    public async Task<ParticipantValidationResponse> TryRemoveParticipantsFromChat(string threadId,
        params IEnumerable<ChatParticipant> participantParams)
    {
        var thread = await _threadService.GetChatModelById(threadId);
        if(thread == null)
            return new ParticipantValidationResponse("Cannot find chat thread to remove participants from.");

        var participants = participantParams.ToList();

        //Guard against removing yourself — use LeaveGroupChat instead
        if (participants.Any(p => p.UserId == _userInfo.UserId))
            return new ParticipantValidationResponse("You cannot remove yourself from a chat.");

        var existingParticipants = thread.Participants
            .Select(p => new ChatParticipant(threadId, p.UserId))
            .ToList();
        
        //Ensures ALL participants are validated and prepared (soft delete is update under the hood)
        var response = _validationService.ValidateAndPrepareParticipants(threadId, participants, addCurrentUser: false);
        if (!response.IsValid) return response;
        
        participants = response.ValidatedParticipants;
        var toRemove = existingParticipants
            .Where(p => participants.Any(pt => pt.UserId == p.UserId))
            .ToList();

        //Validate the post-removal participant state
        var remaining = existingParticipants
            .Where(p => toRemove.All(r => r.UserId != p.UserId))
            .ToList();

        response = _validationService.ValidateAndPrepareParticipants(threadId, remaining);
        if (!response.IsValid) return response;

        await _repos.GetRepository<ChatParticipant>()
            .SoftDeleteRangeAsync(toRemove);
        return response;
    }


    /// <summary>
    /// Allows the current user to leave a group chat. Cannot be used on direct messages.
    /// </summary>
    /// <param name="threadId">The ID of the group chat thread to leave.</param>
    /// <returns><c>true</c> if the user successfully left the group chat; <c>false</c> if the thread was not found, is not a group chat, or the user is not a participant.</returns>
    public async Task<bool> TryLeaveGroupChat(string threadId)
    {
        var thread = await _threadService.GetChatModelById(threadId);
        if (thread == null)
            return false;

        if (!thread.IsGroupChat)
            return false;

        var participant = await _repos.GetRepository<ChatParticipant>()
            .AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(p => p.ThreadId == threadId && p.UserId == _userInfo.UserId);

        if (participant == null)
            return false;

        await _repos.GetRepository<ChatParticipant>()
            .SoftDeleteAsync(participant);
        return true;
    }
}