using JC.Communication.Messaging.Models;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Communication.Messaging.Models.Options;
using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Helpers;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JC.Communication.Messaging.Services;

/// <summary>
/// Provides validation and preparation logic for all messaging entities including threads,
/// participants, messages, and metadata. Methods prefixed with "ValidateAndPrepare" mutate
/// the input entities to set computed properties before validation.
/// </summary>
public class MessagingValidationService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly MessagingOptions _options;

    /// <summary>
    /// Creates a new instance of the messaging validation service.
    /// </summary>
    /// <param name="repos">The repository manager for database queries during validation.</param>
    /// <param name="options">The messaging configuration options.</param>
    /// <param name="userInfo">The current user's identity.</param>
    public MessagingValidationService(IRepositoryManager repos,
        IOptions<MessagingOptions> options,
        IUserInfo userInfo)
    {
        _repos = repos;
        _userInfo = userInfo;
        _options = options.Value;
    }

    /// <summary>
    /// Appends an error message to an existing error string, separated by a newline.
    /// </summary>
    /// <param name="errors">The existing error string, or <c>null</c> if no errors yet.</param>
    /// <param name="err">The new error message to append.</param>
    /// <returns>The combined error string.</returns>
    private string AppendError(string? errors, string err)
    {
        if (!string.IsNullOrWhiteSpace(errors))
            errors += Environment.NewLine;

        errors += err;
        return errors;
    }


    /// <summary>
    /// Checks whether an active default chat thread exists for the given participant set.
    /// The current user is automatically included if not already present.
    /// </summary>
    /// <param name="participantUserIds">The user IDs of the expected participants.</param>
    /// <returns><c>true</c> if a default thread exists for this exact participant set; otherwise <c>false</c>.</returns>
    internal async Task<bool> CheckForDefaultChat(IEnumerable<string> participantUserIds)
    {
        var participantIdList = participantUserIds.ToList();
        if(!participantIdList.Contains(_userInfo.UserId))
            participantIdList.Add(_userInfo.UserId);

        return await _repos.GetRepository<ChatThread>()
            .AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive)
            .AnyAsync(t => t.IsDefaultThread
                                         && t.Participants.Count == participantIdList.Count
                                         && t.Participants.All(p =>
                                             participantIdList.Contains(p.UserId)));
    }

    /// <summary>
    /// Determines whether a participant list represents a direct message (two participants including the
    /// current user, or a single participant).
    /// </summary>
    /// <param name="participants">The participant list to evaluate.</param>
    /// <returns><c>true</c> if the thread is a direct message; <c>false</c> if it is a group chat.</returns>
    internal bool IsThreadDirectMessage(List<ChatParticipant> participants)
        => (participants.Count == 2 && participants.Select(p => p.UserId).Contains(_userInfo.UserId)) 
           || participants.Count == 1;
    

    /// <summary>
    /// Validates and prepares a new chat thread for creation. Sets <see cref="ChatThread.IsGroupThread"/>
    /// and <see cref="ChatThread.IsDefaultThread"/> based on participants and existing defaults.
    /// Enforces <see cref="MessagingOptions.PreventDuplicateChatThreads"/> when a default already exists.
    /// </summary>
    /// <param name="thread">The thread entity to validate and mutate.</param>
    /// <param name="participants">The validated participant list for this thread.</param>
    /// <param name="errors">Optional pre-existing errors from earlier validation stages.</param>
    /// <returns>A <see cref="ChatThreadValidationResponse"/> indicating success (with the prepared thread) or containing validation errors.</returns>
    internal async Task<ChatThreadValidationResponse> ValidateAndPrepareChatThread(ChatThread thread, List<ChatParticipant> participants,
        string? errors = null)
    {
        if (string.IsNullOrWhiteSpace(thread.Name))
            errors = AppendError(errors, "Chat name is required.");
        
        thread.IsGroupThread = !IsThreadDirectMessage(participants);
        thread.IsDefaultThread = !await CheckForDefaultChat(participants.Select(p => p.UserId));
        
        if(!thread.IsDefaultThread && _options.PreventDuplicateChatThreads)
            errors = AppendError(errors, "A default chat already exists for these participants.");
        
        return string.IsNullOrEmpty(errors)
            ? new ChatThreadValidationResponse(thread)
            : new ChatThreadValidationResponse(errors);
    }

    /// <summary>
    /// Validates an update to an existing chat thread. Enforces that <see cref="ChatThread.IsDefaultThread"/>
    /// and <see cref="ChatThread.IsGroupThread"/> are immutable — any attempt to change them produces an error.
    /// </summary>
    /// <param name="thread">The original thread entity from the database.</param>
    /// <param name="updatedThread">The thread entity containing the proposed changes.</param>
    /// <param name="errors">Optional pre-existing errors from earlier validation stages.</param>
    /// <returns>A <see cref="ChatThreadValidationResponse"/> indicating success (with the updated thread) or containing validation errors.</returns>
    internal ChatThreadValidationResponse ValidateChatThread(ChatThread thread, ChatThread updatedThread, string? errors = null)
    {
        if(thread.IsDefaultThread != updatedThread.IsDefaultThread)
            errors = AppendError(errors, "You cannot change the default chat state.");
        
        if(thread.IsGroupThread != updatedThread.IsGroupThread)
            errors = AppendError(errors, "You cannot change whether the chat is a group chat.");
        
        return string.IsNullOrEmpty(errors)
            ? new ChatThreadValidationResponse(updatedThread)
            : new ChatThreadValidationResponse(errors);
    }
    

    /// <summary>
    /// Validates and prepares a participant list for a thread. Adds the current user if not present (when
    /// <paramref name="addCurrentUser"/> is <c>true</c>), checks for duplicates, sets <see cref="ChatParticipant.CanSeeHistory"/>
    /// from options, and enforces the <see cref="MessagingOptions.DisableGroups"/> setting.
    /// </summary>
    /// <param name="threadId">The thread ID to assign to each participant.</param>
    /// <param name="participants">The participant list to validate and mutate.</param>
    /// <param name="errors">Optional pre-existing errors from earlier validation stages.</param>
    /// <param name="addCurrentUser">If <c>true</c>, the current user is added when not already in the list.</param>
    /// <returns>A <see cref="ParticipantValidationResponse"/> indicating success (with the prepared participants) or containing validation errors.</returns>
    internal ParticipantValidationResponse ValidateAndPrepareParticipants(string threadId, List<ChatParticipant> participants,
        string? errors = null, bool addCurrentUser = true)
    {
        var containsUser = participants.Select(pt => pt.UserId).Contains(_userInfo.UserId);
        switch (containsUser)
        {
            case false when addCurrentUser:
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

    /// <summary>
    /// Validates a chat message against the configured <see cref="MessagingOptions.MaxMessageLength"/>.
    /// </summary>
    /// <param name="message">The message entity to validate.</param>
    /// <param name="errors">Optional pre-existing errors from earlier validation stages.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success (with the validated message) or containing validation errors.</returns>
    internal ChatMessageValidationResponse ValidateChatMessage(ChatMessage message, string? errors = null)
    {
        if(message.Message.Length > _options.MaxMessageLength)
            errors = AppendError(errors, $"Message length must be less than {_options.MaxMessageLength} characters.");
        
        return string.IsNullOrEmpty(errors)
            ? new ChatMessageValidationResponse(message)
            : new ChatMessageValidationResponse(errors);
    }

    /// <summary>
    /// Validates and prepares chat metadata for a thread. Requires at least one field (icon, image path,
    /// colour hex, or colour RGB) to be populated. Normalises colour values via <see cref="ColourHelper"/>
    /// and assigns the <paramref name="threadId"/> to the metadata entity.
    /// </summary>
    /// <param name="threadId">The thread ID to assign to the metadata.</param>
    /// <param name="metadata">The metadata entity to validate and mutate, or <c>null</c> to skip validation.</param>
    /// <param name="errors">Optional pre-existing errors from earlier validation stages.</param>
    /// <returns>A <see cref="ChatMetadataValidationResponse"/> indicating success (with the prepared metadata) or containing validation errors.</returns>
    internal ChatMetadataValidationResponse ValidateAndPrepareChatMetadata(string threadId, ChatMetadata? metadata, string? errors = null)
    {
        if (metadata == null) return new ChatMetadataValidationResponse();
        
        if(string.IsNullOrEmpty(metadata.Icon) 
           && string.IsNullOrEmpty(metadata.ImgPath) 
           && string.IsNullOrEmpty(metadata.ColourHex) 
           && string.IsNullOrEmpty(metadata.ColourRgb))
            errors = AppendError(errors, "Chat metadata must contain at least one valid icon, image path, colour hex, or colour RGB value.");

        if (!string.IsNullOrEmpty(metadata.ColourHex))
        {
            var (isValid, hex) = ColourHelper.ValidateAndNormaliseHexColour(metadata.ColourHex);
            if(isValid)
                metadata.ColourHex = $"#{hex}";
            else
                errors = AppendError(errors, "Invalid colour hex format.");
        }

        if (!string.IsNullOrEmpty(metadata.ColourRgb))
        {
            var (isValid, rgb) = ColourHelper.ValidateAndNormaliseRgbColour(metadata.ColourRgb);
            if(isValid)
                metadata.ColourRgb = rgb;
            else
                errors = AppendError(errors, "Invalid colour RGB format.");
        }
        
        metadata.ThreadId = threadId;
        return string.IsNullOrEmpty(errors)
            ? new ChatMetadataValidationResponse(metadata)
            : new ChatMetadataValidationResponse(errors);
    }
}