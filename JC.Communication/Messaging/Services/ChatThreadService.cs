using JC.Communication.Logging.Models.Messaging;
using JC.Communication.Logging.Services;
using JC.Communication.Messaging.Models;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Communication.Messaging.Models.Options;
using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Models.Pagination;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Messaging.Services;

/// <summary>
/// Central service for chat thread operations including querying, creation, promotion,
/// activity tracking, and standard CRUD. All queries are scoped to the current user's participation.
/// </summary>
public class ChatThreadService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<ChatThreadService> _logger;
    private readonly MessagingValidationService _validationService;
    private readonly MessagingLogService _logService;
    private readonly MessagingOptions _options;

    /// <summary>
    /// Creates a new instance of the chat thread service.
    /// </summary>
    /// <param name="repos">The repository manager for database operations.</param>
    /// <param name="userInfo">The current user's identity.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="validationService">The validation service for thread and participant validation.</param>
    /// <param name="options">The messaging configuration options.</param>
    /// <param name="logService">The logging service for thread activity and message read events.</param>
    public ChatThreadService(IRepositoryManager repos,
        IUserInfo userInfo,
        ILogger<ChatThreadService> logger,
        MessagingValidationService validationService,
        IOptions<MessagingOptions> options,
        MessagingLogService logService)
    {
        _repos = repos;
        _userInfo = userInfo;
        _logger = logger;
        _validationService = validationService;
        _logService = logService;
        _options = options.Value;
    }

    #region Queries

    /// <summary>
    /// Builds a base query for chat threads the current user participates in,
    /// including messages, participants, and metadata, ordered by creation date descending.
    /// </summary>
    /// <param name="asNoTracking">If <c>true</c>, the query uses no-tracking for read-only access.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all threads are included.</param>
    /// <returns>An ordered queryable of <see cref="ChatThread"/> scoped to the current user.</returns>
    private IQueryable<ChatThread> QueryThreads(bool asNoTracking, DeletedQueryType deletedQueryType)
    {
        var query = _repos.GetRepository<ChatThread>()
            .AsQueryable().FilterDeleted(deletedQueryType);

        if (asNoTracking)
            query = query.AsNoTracking();

        query = query.Include(t => t.Messages)
            .Include(t => t.Participants)
            .Include(t => t.ChatMetadata)
            .Where(t => t.Participants.Any(p => !p.IsDeleted && p.UserId == _userInfo.UserId) 
                        && !t.UserThreadDeletions.Any(d => !d.IsDeleted && d.UserId == _userInfo.UserId));
        return query.OrderByDescending(t => t.CreatedUtc);
    }

    /// <summary>
    /// Filters messages in the given <see cref="ChatModel"/> based on the current user's history visibility.
    /// If the user cannot see history, only messages sent on or after their <see cref="ParticipantModel.JoinedAtUtc"/> are retained.
    /// </summary>
    /// <param name="model">The chat model whose messages will be filtered.</param>
    /// <returns>The same <see cref="ChatModel"/> instance with messages filtered in place.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the current user is not a participant in the chat.</exception>
    private ChatModel FilterMessageHistory(ChatModel model)
    {
        var userParticipant = model.Participants
            .FirstOrDefault(p => p.UserId == _userInfo.UserId);
        if(userParticipant == null)
            throw new InvalidOperationException("User participant not found in chat model.");

        if (userParticipant.CanSeeHistory)
            return model;
        
        model.Messages = model.Messages
            .Where(m => m.SentAtUtc >= userParticipant.JoinedAtUtc)
            .ToList();
        return model;
    }


    /// <summary>
    /// Retrieves all chat threads the current user participates in, projected as <see cref="ChatModel"/>s.
    /// </summary>
    /// <param name="dateFormat">The format string used to display dates. Defaults to general short format.</param>
    /// <param name="preferHexCode">If <c>true</c>, colour values prefer hex over RGB in the returned model.</param>
    /// <param name="asNoTracking">If <c>true</c>, entities are queried without change tracking.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all threads are returned.</param>
    /// <returns>A list of <see cref="ChatModel"/> representing the user's chat threads.</returns>
    public async Task<List<ChatModel>> GetUserChats(string dateFormat = "g", bool preferHexCode = true,
        bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        => (await QueryThreads(asNoTracking, deletedQueryType).ToListAsync())
            .Select(t => new ChatModel(t, dateFormat, preferHexCode))
            .Select(FilterMessageHistory)
            .ToList();

    /// <summary>
    /// Retrieves a paginated subset of chat threads the current user participates in, projected as <see cref="ChatModel"/>s.
    /// Pagination is applied at the database level for efficiency.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="dateFormat">The format string used to display dates. Defaults to general short format.</param>
    /// <param name="preferHexCode">If <c>true</c>, colour values prefer hex over RGB in the returned model.</param>
    /// <param name="asNoTracking">If <c>true</c>, entities are queried without change tracking.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all threads are returned.</param>
    /// <returns>A paginated collection of <see cref="ChatModel"/>.</returns>
    public async Task<IPagination<ChatModel>> GetUserChats(int pageNumber, int pageSize, string dateFormat = "g",
        bool preferHexCode = true,
        bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        var pagedThreads = await QueryThreads(asNoTracking, deletedQueryType)
            .ToPagedListAsync(pageNumber, pageSize);

        var models = pagedThreads
            .Select(t => new ChatModel(t, dateFormat, preferHexCode))
            .Select(FilterMessageHistory)
            .ToList();
        return new PagedList<ChatModel>(models, pageNumber, pageSize, pagedThreads.TotalCount);
    }


    /// <summary>
    /// Finds the default chat thread between the current user and the specified participants.
    /// The current user is automatically included if not already present in the participant list.
    /// Logs a read event for the most recent message in the thread.
    /// </summary>
    /// <param name="dateFormat">The format string used to display dates.</param>
    /// <param name="preferHexCode">If <c>true</c>, colour values prefer hex over RGB in the returned model.</param>
    /// <param name="asNoTracking">If <c>true</c>, entities are queried without change tracking.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all threads are searched.</param>
    /// <param name="participantUserIds">The user IDs of the expected participants.</param>
    /// <returns>The matching <see cref="ChatModel"/>, or <c>null</c> if no default thread exists for these participants.</returns>
    public async Task<ChatModel?> GetDefaultUserChat(string dateFormat = "g", bool preferHexCode = true, bool asNoTracking = false,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive, params IEnumerable<string> participantUserIds)
    {
        var participantIdList = participantUserIds.ToList();
        if(!participantIdList.Contains(_userInfo.UserId))
            participantIdList.Add(_userInfo.UserId);

        var query = QueryThreads(asNoTracking, deletedQueryType);
        var thread = await query.FirstOrDefaultAsync(t => t.IsDefaultThread
                                                          && t.Participants.Count == participantIdList.Count
                                                          && t.Participants.All(p =>
                                                              participantIdList.Contains(p.UserId)));
        if (thread == null) return null;

        var messages = thread.Messages;
        await _logService.LogMessageReadAsync(messages.MaxBy(m => m.CreatedUtc));
        
        var model =  new ChatModel(thread, dateFormat, preferHexCode);
        return FilterMessageHistory(model);
    }

    /// <summary>
    /// Retrieves a single chat thread by its ID, provided the current user is a participant.
    /// Logs a read event for the most recent message in the thread.
    /// </summary>
    /// <param name="chatThreadId">The unique identifier of the chat thread.</param>
    /// <param name="dateFormat">The format string used to display dates.</param>
    /// <param name="preferHexCode">If <c>true</c>, colour values prefer hex over RGB in the returned model.</param>
    /// <param name="asNoTracking">If <c>true</c>, the entity is queried without change tracking.</param>
    /// <param name="deletedQueryType">Controls whether active, deleted, or all threads are searched.</param>
    /// <returns>The matching <see cref="ChatModel"/>, or <c>null</c> if not found or the user is not a participant.</returns>
    public async Task<ChatModel?> GetChatModelById(string chatThreadId, string dateFormat = "g",
        bool preferHexCode = true, bool asNoTracking = false, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        var thread = await QueryThreads(asNoTracking, deletedQueryType).FirstOrDefaultAsync(t => t.Id == chatThreadId);
        if (thread == null) return null;
        
        var messages = thread.Messages;
        await _logService.LogMessageReadAsync(messages.MaxBy(m => m.CreatedUtc));
        
        var model = new ChatModel(thread, dateFormat, preferHexCode);
        return FilterMessageHistory(model);
    }
    
    /// <summary>
    /// Verifies that an active chat thread exists and the current user is a participant.
    /// </summary>
    /// <param name="threadId">The ID of the thread to verify.</param>
    /// <returns><c>true</c> if the thread exists and the current user participates; otherwise <c>false</c>.</returns>
    public async Task<bool> VerifyChatExists(string threadId)
        => await _repos.GetRepository<ChatThread>().AsQueryable()
            .FilterDeleted(DeletedQueryType.OnlyActive)
            .AnyAsync(t => t.Id == threadId 
                           && t.Participants.Any(p => !p.IsDeleted && p.UserId == _userInfo.UserId));
    
    
    /// <summary>
    /// Finds an existing default chat thread for the given participant set, excluding the specified thread ID.
    /// Used internally to detect default-thread conflicts during promotion and restore operations.
    /// </summary>
    /// <param name="threadId">The thread ID to exclude from the search (typically the thread being promoted or restored).</param>
    /// <param name="participantIdList">The user IDs of the expected participants.</param>
    /// <returns>The existing default <see cref="ChatThread"/>, or <c>null</c> if none exists.</returns>
    private async Task<ChatThread?> GetDefaultChat(string threadId, List<string> participantIdList)
        => await _repos.GetRepository<ChatThread>()
            .AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(t => t.IsDefaultThread
                                      && t.Id != threadId
                                      && t.Participants.Count == participantIdList.Count
                                      && t.Participants.All(p =>
                                          participantIdList.Contains(p.UserId)));

    #endregion
    

    #region Create/Get

    /// <summary>
    /// Returns the existing default chat thread for the given participants, or creates a new default thread if none exists.
    /// </summary>
    /// <param name="chatThreadParams">Parameters controlling thread creation and query behaviour.</param>
    /// <param name="participantsParams">The participants to include in the thread.</param>
    /// <returns>A tuple containing the <see cref="ChatModel"/> (or <c>null</c> on failure) and the participant validation result.</returns>
    public async Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)>
        GetOrCreateDefaultChat(ChatThreadParams chatThreadParams, params IEnumerable<ChatParticipant> participantsParams)
    {
        var participants = participantsParams.ToList();
        var chat = await GetDefaultUserChat(chatThreadParams.DateFormat, chatThreadParams.PreferHexCode, chatThreadParams.AsNoTracking, chatThreadParams.DeletedQueryType,
            participants.Select(p => p.UserId));
        
        return chat == null 
            ? await CreateThread(chatThreadParams, participants, true) 
            : (chat, new ParticipantValidationResponse());
    }

    /// <summary>
    /// Returns the chat thread matching <paramref name="threadId"/> if it exists, or creates a new thread if not found.
    /// </summary>
    /// <param name="threadId">The ID of the thread to look up.</param>
    /// <param name="chatThreadParams">Parameters controlling thread creation and query behaviour.</param>
    /// <param name="participantsParams">The participants to include if a new thread is created.</param>
    /// <returns>A tuple containing the <see cref="ChatModel"/> (or <c>null</c> on failure) and the participant validation result.</returns>
    public async Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)>
        GetOrCreateChat(string threadId, ChatThreadParams chatThreadParams, params IEnumerable<ChatParticipant> participantsParams)
    {
        var participants = participantsParams.ToList();
        var chat = await GetChatModelById(threadId, chatThreadParams.DateFormat, chatThreadParams.PreferHexCode, 
            chatThreadParams.AsNoTracking, chatThreadParams.DeletedQueryType);
        
        return chat == null 
            ? await CreateAndGetNewChat(chatThreadParams, participants) 
            : (chat, new ParticipantValidationResponse());
    }
    

    /// <summary>
    /// Creates a new chat thread and returns it as a <see cref="ChatModel"/>. Automatically determines whether
    /// the thread should be marked as default based on whether a default already exists for the given participants.
    /// </summary>
    /// <param name="chatThreadParams">Parameters controlling thread creation.</param>
    /// <param name="participantsParams">The participants to include in the new thread.</param>
    /// <returns>A tuple containing the <see cref="ChatModel"/> (or <c>null</c> on failure) and the participant validation result.</returns>
    public async Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)>
        CreateAndGetNewChat(ChatThreadParams chatThreadParams, params IEnumerable<ChatParticipant> participantsParams)
    {
        var participants = participantsParams.ToList();
        var defaultAlreadyExists = await _validationService
            .CheckForDefaultChat(participants.Select(p => p.UserId));

        return defaultAlreadyExists
            ? await CreateThread(chatThreadParams, participants, false)
            : await CreateThread(chatThreadParams, participants, true);
    }
    

    /// <summary>
    /// Creates a new <see cref="ChatThread"/> with the given participants within a transaction.
    /// Validates participants, assigns a default name if not provided, and respects the
    /// <see cref="MessagingOptions.PreventDuplicateChatThreads"/> setting.
    /// </summary>
    /// <param name="chatThreadParams">Parameters controlling thread name, description, and query options.</param>
    /// <param name="participants">The validated list of participants to add to the thread.</param>
    /// <param name="isDefault">Whether the new thread should be marked as the default for its participant set.</param>
    /// <returns>A tuple containing the <see cref="ChatModel"/> (or <c>null</c> on failure) and the participant validation result.</returns>
    private async Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)>
        CreateThread(ChatThreadParams chatThreadParams, List<ChatParticipant> participants, bool isDefault)
    {
        if(!isDefault && _options.PreventDuplicateChatThreads)
            return (null, new ParticipantValidationResponse("A default chat already exists for these participants."));
        
        var isDm = _validationService.IsThreadDirectMessage(participants);

        if (string.IsNullOrWhiteSpace(chatThreadParams.Name)) chatThreadParams.Name = null;
        var thread = new ChatThread
        {
            Name = chatThreadParams.Name ?? (isDm
                ? ChatThread.DirectMessageName
                : ChatThread.GroupChatName),
            Description = chatThreadParams.Description,
            IsDefaultThread = isDefault,
            IsGroupThread = !isDm
        };
        
        var response = _validationService.ValidateAndPrepareParticipants(thread.Id, participants);
        if (!response.IsValid)
        {
            _logger.LogDebug("Unable to create default chat thread for user IDs: {Participants}, chat name: {Name}",
                string.Join("; ", response.ValidatedParticipants), thread.Name);
            return (null, response);
        }

        participants = response.ValidatedParticipants;
        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<ChatThread>()
                .AddAsync(thread, saveNow: false);

            await _repos.GetRepository<ChatParticipant>()
                .AddRangeAsync(participants, saveNow: false);

            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();

            thread.Participants = participants;
            return (new ChatModel(thread), response);
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to create new default chat thread for user IDs: {Participants}, chat name: {Name}",
                string.Join("; ", response.ValidatedParticipants), thread.Name);
            return (null, response);
        }
    }

    #endregion


    #region Operations

    /// <summary>
    /// Promotes a non-default chat thread to default status. The current user must be a participant.
    /// If the thread is already the default, returns <c>true</c> immediately.
    /// When another default thread exists for the same participants, the <paramref name="demoteExisting"/>
    /// parameter controls whether the existing default is demoted or the operation is blocked.
    /// </summary>
    /// <param name="threadId">The ID of the thread to promote.</param>
    /// <param name="demoteExisting">
    /// If <c>true</c>, the existing default thread is demoted to allow promotion.
    /// If <c>false</c>, the operation returns <c>false</c> when another default already exists.
    /// </param>
    /// <returns><c>true</c> if the thread is now the default (or already was); <c>false</c> if not found, the user is not a participant, or promotion was blocked.</returns>
    public async Task<bool> PromoteChatToDefault(string threadId, bool demoteExisting = false)
    {
        var thread = await _repos.GetRepository<ChatThread>()
            .AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive)
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == threadId 
                                      && t.Participants.Any(p => !p.IsDeleted && p.UserId == _userInfo.UserId));
        
        if (thread == null)
            return false;
        
        if (thread.IsDefaultThread)
            return true;

        var existingModified = false;
        var existingDefault = await GetDefaultChat(threadId, thread.Participants.Select(p => p.UserId).ToList());
        if (existingDefault != null)
        {
            if (!demoteExisting)
                return false;
            
            existingDefault.IsDefaultThread = false;
            existingModified = true;
        }

        thread.IsDefaultThread = true;
        
        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<ChatThread>()
                .UpdateAsync(thread, saveNow: false);
            
            if (existingModified && existingDefault != null)
                await _repos.GetRepository<ChatThread>()
                    .UpdateAsync(existingDefault, saveNow: false);
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to promote chat to default");
            return false;
        }
    }

    /// <summary>
    /// Updates the thread's last activity timestamp and logs the activity via the messaging log service.
    /// Does not call save — the caller is responsible for persisting changes.
    /// </summary>
    /// <param name="threadId">The ID of the thread to update.</param>
    /// <param name="activityType">The type of activity that occurred.</param>
    /// <param name="activityDetails">Optional details describing the activity (e.g. participant user IDs).</param>
    internal async Task UpdateLastActivity(string threadId, ThreadActivityType activityType, string? activityDetails = null)
    {
        var thread = await _repos.GetRepository<ChatThread>()
            .AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(t => t.Id == threadId 
                        && t.Participants.Any(p => !p.IsDeleted && p.UserId == _userInfo.UserId));
        if(thread == null) return;
        
        thread.LastActivityUtc = DateTime.UtcNow;
        await _repos.GetRepository<ChatThread>()
            .UpdateAsync(thread, saveNow: false);
        
        await _logService.LogThreadActivityAsync(threadId, activityType, activityDetails);
    }

    #endregion
    

    #region Standard Create/Update/Delete/Restore

    /// <summary>
    /// Validates and persists a new chat thread with its participants and optional metadata within a transaction.
    /// Both participant and thread validation are performed; all accumulated errors are returned in the response.
    /// </summary>
    /// <param name="thread">The chat thread entity to create.</param>
    /// <param name="metadata">Optional metadata (icon, colour, image) to associate with the thread.</param>
    /// <param name="participantParams">The participants to add to the thread.</param>
    /// <returns>A <see cref="ChatThreadValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<ChatThreadValidationResponse> TryCreateChat(ChatThread thread, ChatMetadata? metadata = null,
        params IEnumerable<ChatParticipant> participantParams)
    {
        var participants = participantParams.ToList();
        
        var participantResponse = _validationService.ValidateAndPrepareParticipants(thread.Id, participants);
        participants = participantResponse.ValidatedParticipants;
        
        var metadataResponse = _validationService.ValidateAndPrepareChatMetadata(thread.Id, metadata, 
            participantResponse.ErrorMessage);
        metadata = metadataResponse.ValidatedChatMetadata;
        
        var threadResponse = await _validationService.ValidateAndPrepareChatThread(thread, participants, 
            metadataResponse.ErrorMessage);

        if (!participantResponse.IsValid || !metadataResponse.IsValid || !threadResponse.IsValid)
            return threadResponse; //Last response has all errors in its message

        thread = threadResponse.ValidatedChatThread 
                 ?? throw new InvalidOperationException("Unexpected error occured during thread validation. Thread is null");
        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<ChatThread>()
                .AddAsync(thread, saveNow: false);
            
            await _repos.GetRepository<ChatParticipant>()
                .AddRangeAsync(participants, saveNow: false);
            
            if(metadata != null)
                await _repos.GetRepository<ChatMetadata>()
                    .AddAsync(metadata, saveNow: false);
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return threadResponse;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to create new chat thread");
            return new ChatThreadValidationResponse("Unable to create new chat thread");
        }
    }


    /// <summary>
    /// Validates and applies updates to an existing chat thread. The current user must be a participant.
    /// Immutable properties (default status, group status) are enforced by validation.
    /// </summary>
    /// <param name="threadId">The ID of the thread to update.</param>
    /// <param name="updatedThread">The thread entity containing the updated values.</param>
    /// <returns>A <see cref="ChatThreadValidationResponse"/> indicating success (with the validated thread) or containing validation errors.</returns>
    public async Task<ChatThreadValidationResponse> TryUpdateChatThread(string threadId, ChatThread updatedThread)
    {
        var thread = await _repos.GetRepository<ChatThread>()
            .AsQueryable().FirstOrDefaultAsync(t => t.Id == threadId 
                                                    && t.Participants.Any(p => !p.IsDeleted && p.UserId == _userInfo.UserId));
        if (thread == null)
            return new ChatThreadValidationResponse("Chat thread not found");
        
        var response = _validationService.ValidateChatThread(thread, updatedThread);
        if(!response.IsValid) return response;
            
        await _repos.GetRepository<ChatThread>()
            .UpdateAsync(response.ValidatedChatThread 
                         ?? throw new InvalidOperationException("Unexpected error occured during thread validation. Thread is null"));
        return response;
    }


    /// <summary>
    /// Soft-deletes an active chat thread. The current user must be a participant.
    /// </summary>
    /// <param name="threadId">The ID of the thread to delete.</param>
    /// <returns><c>true</c> if the thread was found and soft-deleted; <c>false</c> if not found or the user is not a participant.</returns>
    public async Task<bool> TryDeleteChatThreadForAll(string threadId)
    {
        var thread = await _repos.GetRepository<ChatThread>()
            .AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive)
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == threadId
                                      && t.Participants.Any(p => !p.IsDeleted && p.UserId == _userInfo.UserId));

        if (thread == null)
            return false;

        var userDeletions = await GetThreadDeletions(threadId, DeletedQueryType.OnlyActive);
        var threadDeletions = thread.Participants
            .Where(p => !userDeletions.Select(d => d.UserId).Contains(p.UserId))
            .Select(p => new ThreadDeleted
            {
                ThreadId = threadId,
                UserId = p.UserId
            })
            .ToList();

        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<ChatThread>()
                .SoftDeleteAsync(thread, saveNow: false);

            await _repos.GetRepository<ThreadDeleted>()
                .AddAsync(threadDeletions, saveNow: false);

            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to delete thread for all users");
            return false;
        }
    }

    /// <summary>
    /// Restores a soft-deleted chat thread. If the thread is already active, returns <c>true</c> immediately.
    /// When restoring a default thread and another default already exists for the same participants,
    /// the <paramref name="mode"/> determines how the conflict is resolved.
    /// </summary>
    /// <param name="threadId">The ID of the thread to restore.</param>
    /// <param name="mode">
    /// The strategy for resolving default-thread conflicts:
    /// <see cref="DefaultThreadRestoreMode.Block"/> prevents the restore,
    /// <see cref="DefaultThreadRestoreMode.DemoteExisting"/> removes default status from the existing thread,
    /// <see cref="DefaultThreadRestoreMode.DemoteRestored"/> removes default status from the restored thread.
    /// </param>
    /// <returns><c>true</c> if the thread was restored or already active; <c>false</c> if not found, the user is not a participant, or the restore was blocked.</returns>
    public async Task<bool> TryRestoreChatThreadForAll(string threadId, DefaultThreadRestoreMode mode = DefaultThreadRestoreMode.DemoteExisting)
    {
        var thread = await _repos.GetRepository<ChatThread>()
            .AsQueryable().FilterDeleted(DeletedQueryType.All)
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == threadId 
                                      && t.Participants.Any(p => !p.IsDeleted && p.UserId == _userInfo.UserId));
        
        if (thread == null)
            return false;

        if (!thread.IsDeleted)
            return true;
        
        var participantIdList = thread.Participants.Select(p => p.UserId).ToList();
        var existingDefault = await GetDefaultChat(threadId, participantIdList);

        var existingModified = false;
        if (existingDefault != null && thread.IsDefaultThread)
        {
            switch (mode)
            {
                case DefaultThreadRestoreMode.Block:
                    return false;
                case DefaultThreadRestoreMode.DemoteExisting:
                    existingDefault.IsDefaultThread = false;
                    existingModified = true;
                    break;
                case DefaultThreadRestoreMode.DemoteRestored:
                    thread.IsDefaultThread = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        var participantUserIds = thread.Participants.Select(p => p.UserId).ToList();
        var userDeletions = (await GetThreadDeletions(threadId, DeletedQueryType.OnlyActive))
            .Where(d => participantIdList.Contains(d.UserId))
            .ToList();
        
        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<ChatThread>()
                .RestoreAsync(thread, saveNow: false);
            
            if(existingModified && existingDefault != null)
                await _repos.GetRepository<ChatThread>()
                    .UpdateAsync(existingDefault, saveNow: false);

            if (userDeletions.Count > 0)
                await _repos.GetRepository<ThreadDeleted>()
                    .SoftDeleteRangeAsync(userDeletions, saveNow: false);
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to restore chat thread");
            return false;
        }
    }

    #endregion



    #region Per User Delete/Restore

    private async Task<List<ThreadDeleted>> GetThreadDeletions(string threadId, DeletedQueryType deletedQueryType)
        => await _repos.GetRepository<ThreadDeleted>().AsQueryable().FilterDeleted(deletedQueryType)
            .Where(d => d.ThreadId == threadId)
            .ToListAsync();

    private async Task<ThreadDeleted?> GetUserThreadDelete(string threadId, string userId,
        DeletedQueryType deletedQueryType)
        => await _repos.GetRepository<ThreadDeleted>().AsQueryable().FilterDeleted(deletedQueryType)
            .FirstOrDefaultAsync(d => d.ThreadId == threadId && d.UserId == userId);
    
    public async Task<bool> TryDeleteThreadForUser(string threadId)
    {
        var threadExists = await VerifyChatExists(threadId);
        if (!threadExists) return false;

        var threadDelete = await GetUserThreadDelete(threadId, _userInfo.UserId, DeletedQueryType.OnlyActive);
        if (threadDelete != null) return true;
        
        threadDelete = new ThreadDeleted
        {
            ThreadId = threadId,
            UserId = _userInfo.UserId
        };

        await _repos.GetRepository<ThreadDeleted>()
            .AddAsync(threadDelete);
        return true;
    }

    public async Task<bool> TryRestoreThreadForUser(string threadId)
    {
        var threadExists = await VerifyChatExists(threadId);
        if (!threadExists) return false;

        var threadDelete = await GetUserThreadDelete(threadId, _userInfo.UserId, DeletedQueryType.All);
        if (threadDelete == null || threadDelete.IsDeleted) return true;

        await _repos.GetRepository<ThreadDeleted>()
            .SoftDeleteAsync(threadDelete);
        return true;
    }

    #endregion
}