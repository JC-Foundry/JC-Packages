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

public class ChatThreadService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<ChatThreadService> _logger;
    private readonly MessagingValidationService _validationService;
    //private readonly MessagingOptions _options;

    public ChatThreadService(IRepositoryManager repos,
        IUserInfo userInfo,
        ILogger<ChatThreadService> logger,
        MessagingValidationService validationService)
    {
        _repos = repos;
        _userInfo = userInfo;
        _logger = logger;
        _validationService = validationService;
        //_options = options.Value;
    }

    #region Queries

    private IQueryable<ChatThread> QueryThreads(bool asNoTracking, DeletedQueryType deletedQueryType)
    {
        var query = _repos.GetRepository<ChatThread>()
            .AsQueryable().FilterDeleted(deletedQueryType);

        if (asNoTracking)
            query = query.AsNoTracking();

        query = query.Include(t => t.Messages)
            .Include(t => t.Participants)
            .Include(t => t.ChatMetadata)
            .Where(t => t.Participants.Any(p => p.UserId == _userInfo.UserId));
        return query.OrderByDescending(t => t.CreatedUtc);
    }


    public async Task<List<ChatModel>> GetUserChats(string dateFormat = "g", bool preferHexCode = true,
        bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        => (await QueryThreads(asNoTracking, deletedQueryType).ToListAsync())
            .Select(t => new ChatModel(t, dateFormat, preferHexCode)).ToList();

    public async Task<IPagination<ChatModel>> GetUserChats(int pageNumber, int pageSize, string dateFormat = "g",
        bool preferHexCode = true,
        bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        => (await GetUserChats(dateFormat, preferHexCode, asNoTracking, deletedQueryType))
            .ToPagedList(pageNumber, pageSize);


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
        return thread == null ? null : new ChatModel(thread, dateFormat, preferHexCode);
    }

    public async Task<ChatModel?> GetChatModelById(string chatThreadId, string dateFormat = "g",
        bool preferHexCode = true, bool asNoTracking = false, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        var thread = await QueryThreads(asNoTracking, deletedQueryType).FirstOrDefaultAsync(t => t.Id == chatThreadId);
        return thread == null ? null : new ChatModel(thread, dateFormat, preferHexCode);
    }

    #endregion
    

    #region Create/Get

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
    

    public async Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)> 
        CreateAndGetNewChat(ChatThreadParams chatThreadParams, params IEnumerable<ChatParticipant> participantsParams)
    {
        var participants = participantsParams.ToList();
        var defaultChat = await _validationService
            .CheckForDefaultChat(participants.Select(p => p.UserId));
        
        return defaultChat
            ? await CreateThread(chatThreadParams, participants, false)
            : await CreateThread(chatThreadParams, participants, true);
    }
    

    private async Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)> 
        CreateThread(ChatThreadParams chatThreadParams, List<ChatParticipant> participants, bool isDefault)
    {
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


    #region Standard Create/Update/Delete/Restore

    public async Task<ChatThreadValidationResponse> TryCreateChat(ChatThread thread, ChatMetadata? metadata = null, 
        params IEnumerable<ChatParticipant> participantParams)
    {
        var participants = participantParams.ToList();
        
        var participantResponse = _validationService.ValidateAndPrepareParticipants(thread.Id, participants);
        participants = participantResponse.ValidatedParticipants;
        
        var threadResponse = await _validationService.ValidateAndPrepareChatThread(thread, participants, 
            participantResponse.ErrorMessage);

        if (!participantResponse.IsValid || !threadResponse.IsValid)
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
            return new ChatThreadValidationResponse();
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to create new chat thread");
            return new ChatThreadValidationResponse("Unable to create new chat thread");
        }
    }


    public async Task<ChatThreadValidationResponse> TryUpdateChatThread(string threadId, ChatThread updatedThread)
    {
        var thread = await _repos.GetRepository<ChatThread>()
            .AsQueryable().FirstOrDefaultAsync(t => t.Id == threadId);
        if (thread == null)
            return new ChatThreadValidationResponse("Chat thread not found");
        
        var response = _validationService.ValidateChatThread(thread, updatedThread);
        if(!response.IsValid) return response;
            
        await _repos.GetRepository<ChatThread>()
            .UpdateAsync(updatedThread);
        return new ChatThreadValidationResponse(updatedThread);
    }

    #endregion
}