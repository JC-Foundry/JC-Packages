using JC.Communication.Messaging.Models;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Models.Pagination;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JC.Communication.Messaging.Services;

public class ChatThreadService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<ChatThreadService> _logger;
    private readonly MessagingValidationService _validationService;

    public ChatThreadService(IRepositoryManager repos,
        IUserInfo userInfo,
        ILogger<ChatThreadService> logger,
        MessagingValidationService validationService)
    {
        _repos = repos;
        _userInfo = userInfo;
        _logger = logger;
        _validationService = validationService;
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

    public async Task<ChatModel?> GetOrCreateDefaultChat(string? name = null, string? description = null, 
        string dateFormat = "g", bool preferHexCode = true, bool asNoTracking = false, 
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive, params IEnumerable<ChatParticipant> participantsParams)
    {
        var participants = participantsParams.ToList();
        var chat = await GetDefaultUserChat(dateFormat, preferHexCode, asNoTracking, deletedQueryType,
            participants.Select(p => p.UserId));
        if (chat != null) return chat;
        
        var isDm = (participants.Count == 2 && participants.Select(p => p.UserId).Contains(_userInfo.UserId)) 
                      || participants.Count == 1;
        var thread = new ChatThread
        {
            Name = name ?? (isDm
                ? ChatThread.DirectMessageName
                : ChatThread.GroupChatName),
            Description = description,
            IsDefaultThread = true,
            IsGroupThread = !isDm
        };

        var response = _validationService.ValidateAndPrepareParticipants(thread.Id, participants);
        if (!response.IsValid)
        {
            _logger.LogDebug("Unable to create default chat thread for user IDs: {Participants}, chat name: {Name}",
                string.Join("; ", response.ValidatedParticipants), thread.Name);
            return null;
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
            return new ChatModel(thread);
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to create new default chat thread for user IDs: {Participants}, chat name: {Name}",
                string.Join("; ", response.ValidatedParticipants), thread.Name);
            return null;
        }
    }

    #endregion
}