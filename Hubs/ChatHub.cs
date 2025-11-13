using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HaluluAPI.Services;

namespace HaluluAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ITokenService tokenService, ILogger<ChatHub> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(Context.User!);
        if (profileId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{profileId.Value}");
            _logger.LogInformation($"User {profileId.Value} connected to chat hub");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(Context.User!);
        if (profileId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{profileId.Value}");
            _logger.LogInformation($"User {profileId.Value} disconnected from chat hub");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRequestChat(string requestId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"request_{requestId}");
    }

    public async Task LeaveRequestChat(string requestId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request_{requestId}");
    }
}