using JC.Communication.Messaging.Models;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;

namespace JC.Communication.Messaging.Services;

/// <summary>
/// Manages chat metadata operations including creating, updating, deleting, and restoring thread metadata.
/// All operations verify thread existence before proceeding.
/// </summary>
public class ChatMetadataService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ChatThreadService _threadService;
    private readonly MessagingValidationService _validationService;

    /// <summary>
    /// Creates a new instance of the chat metadata service.
    /// </summary>
    /// <param name="repos">The repository manager for database operations.</param>
    /// <param name="userInfo">The current user's identity.</param>
    /// <param name="threadService">The thread service for thread verification.</param>
    /// <param name="validationService">The validation service for metadata validation and preparation.</param>
    public ChatMetadataService(IRepositoryManager repos,
        IUserInfo userInfo,
        ChatThreadService threadService,
        MessagingValidationService validationService)
    {
        _repos = repos;
        _userInfo = userInfo;
        _threadService = threadService;
        _validationService = validationService;
    }

    /// <summary>
    /// Retrieves metadata for a thread, optionally including soft-deleted records.
    /// </summary>
    /// <param name="threadId">The ID of the thread to retrieve metadata for.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all metadata records are searched.</param>
    /// <returns>The matching metadata, or <c>null</c> if not found.</returns>
    private async Task<ChatMetadata?> GetChatMetadata(string threadId, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        => await _repos.GetRepository<ChatMetadata>().AsQueryable()
            .FilterDeleted(deletedQueryType)
            .FirstOrDefaultAsync(m => m.ThreadId == threadId);
    
    /// <summary>
    /// Checks whether active metadata exists for the specified thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread to check.</param>
    /// <returns><c>true</c> if active metadata exists; otherwise <c>false</c>.</returns>
    private async Task<bool> MetadataExists(string threadId)
        => await _repos.GetRepository<ChatMetadata>().AsQueryable()
            .FilterDeleted(DeletedQueryType.OnlyActive)
            .AnyAsync(m => m.ThreadId == threadId);
    

    /// <summary>
    /// Validates and creates new metadata for the specified thread. If soft-deleted metadata already exists,
    /// it is hard-deleted before creating the new record. Returns an error if active metadata already exists.
    /// </summary>
    /// <param name="threadId">The ID of the thread to create metadata for.</param>
    /// <param name="metadata">The metadata entity to validate and persist.</param>
    /// <returns>A <see cref="ChatMetadataValidationResponse"/> indicating success (with the created metadata) or containing validation errors.</returns>
    public async Task<ChatMetadataValidationResponse> TryCreateChatMetadata(string threadId, ChatMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var threadExists = await _threadService.VerifyChatExists(threadId);
        if(!threadExists) return new ChatMetadataValidationResponse("Chat thread does not exist");

        var existingMetadata = await GetChatMetadata(threadId, DeletedQueryType.All);
        if (existingMetadata != null)
        {
            if(!existingMetadata.IsDeleted)
                return new ChatMetadataValidationResponse("Chat metadata already exists");
            
            await _repos.GetRepository<ChatMetadata>()
                .DeleteAsync(existingMetadata);
        }
        
        var response = _validationService.ValidateAndPrepareChatMetadata(threadId, metadata);
        if (!response.IsValid) return response;
        
        //Null suppressor '!' used - should never be null since passed metadata is not null.
        metadata = response.ValidatedChatMetadata!;
        await _repos.GetRepository<ChatMetadata>()
            .AddAsync(metadata);
        return response;
    }
    
    /// <summary>
    /// Validates and updates existing metadata for the specified thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread whose metadata is being updated.</param>
    /// <param name="metadata">The metadata entity containing the updated values.</param>
    /// <returns>A <see cref="ChatMetadataValidationResponse"/> indicating success (with the updated metadata) or containing validation errors.</returns>
    public async Task<ChatMetadataValidationResponse> TryUpdateChatMetadata(string threadId, ChatMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if (!threadExists) return new ChatMetadataValidationResponse("Chat thread does not exist");

        var metadataExists = await MetadataExists(threadId);
        if (!metadataExists) return new ChatMetadataValidationResponse("Chat metadata does not exist");

        var response = _validationService.ValidateAndPrepareChatMetadata(threadId, metadata);
        if (!response.IsValid) return response;
        
        //Null suppressor '!' used - should never be null since passed metadata is not null.
        metadata = response.ValidatedChatMetadata!;
        await _repos.GetRepository<ChatMetadata>()
            .UpdateAsync(metadata);
        return response;
    }

    /// <summary>
    /// Soft-deletes the metadata for the specified thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread whose metadata is being deleted.</param>
    /// <returns>A <see cref="ChatMetadataValidationResponse"/> indicating success (with the deleted metadata) or containing validation errors.</returns>
    public async Task<ChatMetadataValidationResponse> TryDeleteChatMetadata(string threadId)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if (!threadExists) return new ChatMetadataValidationResponse("Chat thread does not exist");
        
        var metadata = await GetChatMetadata(threadId);
        if (metadata == null) return new ChatMetadataValidationResponse("Chat metadata does not exist");
        
        await _repos.GetRepository<ChatMetadata>()
            .SoftDeleteAsync(metadata);
        return new ChatMetadataValidationResponse(metadata);
    }

    /// <summary>
    /// Restores soft-deleted metadata for the specified thread. Returns success if the metadata is already active.
    /// </summary>
    /// <param name="threadId">The ID of the thread whose metadata is being restored.</param>
    /// <returns>A <see cref="ChatMetadataValidationResponse"/> indicating success (with the restored metadata) or containing validation errors.</returns>
    public async Task<ChatMetadataValidationResponse> TryRestoreChatMetadata(string threadId)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if (!threadExists) return new ChatMetadataValidationResponse("Chat thread does not exist");
        
        var metadata = await GetChatMetadata(threadId, DeletedQueryType.All);
        if (metadata == null) return new ChatMetadataValidationResponse("Chat metadata does not exist");

        if (!metadata.IsDeleted) return new ChatMetadataValidationResponse(metadata);
        
        await _repos.GetRepository<ChatMetadata>()
            .RestoreAsync(metadata);
        return new ChatMetadataValidationResponse(metadata);
    } 
}