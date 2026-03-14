using JC.Communication.Logging.Models.Messaging;
using JC.Communication.Messaging.Models;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JC.Communication.Messaging.Services;

/// <summary>
/// Manages chat message operations including sending, editing, deleting, restoring, and bulk operations.
/// All operations verify thread existence and user participation before proceeding.
/// </summary>
public class ChatMessageService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<ChatMessageService> _logger;
    private readonly ChatThreadService _threadService;
    private readonly MessagingValidationService _validationService;

    /// <summary>
    /// Creates a new instance of the chat message service.
    /// </summary>
    /// <param name="repos">The repository manager for database operations.</param>
    /// <param name="userInfo">The current user's identity.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="threadService">The thread service for thread verification and activity updates.</param>
    /// <param name="validationService">The validation service for message validation.</param>
    public ChatMessageService(IRepositoryManager repos,
        IUserInfo userInfo,
        ILogger<ChatMessageService> logger,
        ChatThreadService threadService,
        MessagingValidationService validationService)
    {
        _repos = repos;
        _userInfo = userInfo;
        _logger = logger;
        _threadService = threadService;
        _validationService = validationService;
    }

    /// <summary>
    /// Retrieves a single message by ID, scoped to the current user as the sender.
    /// </summary>
    /// <param name="threadId">The ID of the thread the message belongs to.</param>
    /// <param name="messageId">The ID of the message to retrieve.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all messages are searched.</param>
    /// <returns>The matching message, or <c>null</c> if not found or not owned by the current user.</returns>
    private async Task<ChatMessage?> GetMessage(string threadId, string messageId,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        => await _repos.GetRepository<ChatMessage>().AsQueryable()
            .FilterDeleted(deletedQueryType)
            .FirstOrDefaultAsync(m => m.Id == messageId
                                      && m.ThreadId == threadId
                                      && m.CreatedById == _userInfo.UserId);

    /// <summary>
    /// Retrieves messages in a thread, optionally filtered to a specific user.
    /// When <paramref name="userId"/> is <c>null</c>, returns all messages in the thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread to retrieve messages from.</param>
    /// <param name="userId">Optional user ID to filter messages by sender. <c>null</c> returns all messages.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all messages are returned.</param>
    /// <returns>A list of matching messages.</returns>
    private async Task<List<ChatMessage>> GetMessages(string threadId, string? userId = null,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        => await _repos.GetRepository<ChatMessage>().AsQueryable()
            .FilterDeleted(deletedQueryType)
            .Where(m => m.ThreadId == threadId
                        && (string.IsNullOrEmpty(userId) || m.CreatedById == userId))
            .ToListAsync();


    /// <summary>
    /// Validates and sends a new message in the specified thread within a transaction.
    /// Updates the thread's last activity timestamp and logs the activity.
    /// </summary>
    /// <param name="threadId">The ID of the thread to send the message in.</param>
    /// <param name="message">The message content to send.</param>
    /// <param name="replyToId">The ID of the message to reply to.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success (with the created message) or containing validation errors.</returns>
    public async Task<ChatMessageValidationResponse> TrySendMessage(string threadId, string message, string? replyToId)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if(!threadExists) return new ChatMessageValidationResponse("Chat thread does not exist");

        var msg = new ChatMessage(threadId, message, replyToId);
        var response = _validationService.ValidateChatMessage(msg);
        if(!response.IsValid) return response;
        
        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<ChatMessage>()
                .AddAsync(response.ValidatedChatMessage!, saveNow: false);
            
            //Update last activity:
            await _threadService.UpdateLastActivity(threadId, ThreadActivityType.Message,
                ActivityDetailsHelper.GetActivityDetails(ThreadActivityType.Message, [_userInfo.UserId]));
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return response;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to send message");
            return new ChatMessageValidationResponse("Failed to send message");
        }
    }

    /// <summary>
    /// Validates and updates the content of an existing message owned by the current user.
    /// </summary>
    /// <param name="threadId">The ID of the thread the message belongs to.</param>
    /// <param name="messageId">The ID of the message to edit.</param>
    /// <param name="newMessage">The new message content.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success (with the updated message) or containing validation errors.</returns>
    public async Task<ChatMessageValidationResponse> TryEditMessage(string threadId, string messageId, string newMessage)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if(!threadExists) return new ChatMessageValidationResponse("Chat thread does not exist");

        var message = await GetMessage(threadId, messageId);
        if(message == null) return new ChatMessageValidationResponse("Message not found");
        
        message.Message = newMessage;
        var response = _validationService.ValidateChatMessage(message);
        if (!response.IsValid) return response;
        
        await _repos.GetRepository<ChatMessage>()
            .UpdateAsync(message);
        return response;
    }

    /// <summary>
    /// Soft-deletes a message owned by the current user.
    /// </summary>
    /// <param name="threadId">The ID of the thread the message belongs to.</param>
    /// <param name="messageId">The ID of the message to delete.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success (with the deleted message) or containing validation errors.</returns>
    public async Task<ChatMessageValidationResponse> TryDeleteMessage(string threadId, string messageId)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if(!threadExists) return new ChatMessageValidationResponse("Chat thread does not exist");

        var message = await GetMessage(threadId, messageId);
        if(message == null) return new ChatMessageValidationResponse("Message not found");
        
        await _repos.GetRepository<ChatMessage>()
            .SoftDeleteAsync(message);
        return new ChatMessageValidationResponse(message);
    }

    /// <summary>
    /// Restores a soft-deleted message owned by the current user. Returns success if the message is already active.
    /// </summary>
    /// <param name="threadId">The ID of the thread the message belongs to.</param>
    /// <param name="messageId">The ID of the message to restore.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success (with the restored message) or containing validation errors.</returns>
    public async Task<ChatMessageValidationResponse> TryRestoreMessage(string threadId, string messageId)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if(!threadExists) return new ChatMessageValidationResponse("Chat thread does not exist");

        var message = await GetMessage(threadId, messageId, DeletedQueryType.All);
        if(message == null) return new ChatMessageValidationResponse("Message not found");

        //Always return success if message is already restored:
        if (!message.IsDeleted) 
            return new ChatMessageValidationResponse(message);
        
        await _repos.GetRepository<ChatMessage>()
            .RestoreAsync(message);
        return new ChatMessageValidationResponse(message);
    }

    /// <summary>
    /// Soft-deletes all active messages sent by the current user in the specified thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread to delete messages from.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ChatMessageValidationResponse> TryDeleteAllMyMessages(string threadId)
        => await DeleteBulk(threadId, _userInfo.UserId);

    /// <summary>
    /// Restores all soft-deleted messages sent by the current user in the specified thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread to restore messages in.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ChatMessageValidationResponse> TryRestoreAllMyDeletedMessages(string threadId)
        => await RestoreBulk(threadId, _userInfo.UserId);


    #region UNSAFE Delete/Restore Methods

    /// <summary>
    /// Soft-deletes all active messages in the specified thread regardless of sender.
    /// <b>Unsafe:</b> affects messages from all participants.
    /// </summary>
    /// <param name="threadId">The ID of the thread to delete all messages from.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ChatMessageValidationResponse> TryDeleteAllMessages(string threadId)
        => await DeleteBulk(threadId, null);

    /// <summary>
    /// Restores all soft-deleted messages in the specified thread regardless of sender.
    /// <b>Unsafe:</b> affects messages from all participants.
    /// </summary>
    /// <param name="threadId">The ID of the thread to restore all messages in.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ChatMessageValidationResponse> TryRestoreAllDeletedMessages(string threadId)
        => await RestoreBulk(threadId, null);

    #endregion


    #region Private Methods

    /// <summary>
    /// Soft-deletes messages in the specified thread, optionally filtered to a specific user.
    /// </summary>
    /// <param name="threadId">The ID of the thread to delete messages from.</param>
    /// <param name="userId">The user ID to scope deletion to, or <c>null</c> to delete all messages.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success or containing validation errors.</returns>
    private async Task<ChatMessageValidationResponse> DeleteBulk(string threadId, string? userId)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if (!threadExists) return new ChatMessageValidationResponse("Chat thread does not exist");

        var messages = await GetMessages(threadId, userId);
        if (messages.Count == 0) return new ChatMessageValidationResponse("No messages found");
        
        await _repos.GetRepository<ChatMessage>()
            .SoftDeleteRangeAsync(messages);
        return new ChatMessageValidationResponse();
    }

    /// <summary>
    /// Restores soft-deleted messages in the specified thread, optionally filtered to a specific user.
    /// </summary>
    /// <param name="threadId">The ID of the thread to restore messages in.</param>
    /// <param name="userId">The user ID to scope restoration to, or <c>null</c> to restore all messages.</param>
    /// <returns>A <see cref="ChatMessageValidationResponse"/> indicating success or containing validation errors.</returns>
    private async Task<ChatMessageValidationResponse> RestoreBulk(string threadId, string? userId)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if (!threadExists) return new ChatMessageValidationResponse("Chat thread does not exist");
        
        var messages = await GetMessages(threadId, userId, DeletedQueryType.All);
        if (messages.Count == 0) return new ChatMessageValidationResponse("No messages found");
        
        var restoreMessages = messages.Where(m => m.IsDeleted).ToList();
        if (restoreMessages.Count == 0) return new ChatMessageValidationResponse();
        
        await _repos.GetRepository<ChatMessage>()
            .RestoreRangeAsync(restoreMessages);
        return new ChatMessageValidationResponse();
    }

    #endregion
}