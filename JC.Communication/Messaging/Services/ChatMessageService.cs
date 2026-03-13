using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Logging;

namespace JC.Communication.Messaging.Services;

public class ChatMessageService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<ChatMessageService> _logger;
    private readonly ChatThreadService _threadService;

    public ChatMessageService(IRepositoryManager repos,
        IUserInfo userInfo,
        ILogger<ChatMessageService> logger,
        ChatThreadService threadService)
    {
        _repos = repos;
        _userInfo = userInfo;
        _logger = logger;
        _threadService = threadService;
    }

    public async Task<bool> TrySendMessage(string threadId, string message)
    {
        var threadExists = await _threadService.VerifyChatExists(threadId);
        if(!threadExists)
    }
}