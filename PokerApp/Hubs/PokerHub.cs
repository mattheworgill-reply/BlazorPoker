using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using PokerApp.Models;
using PokerApp.Models.Enums;
using PokerApp.Services;

namespace PokerApp.Hubs;

public class PokerHub : Hub
{
    private readonly GameTimerService _timerService;
    private readonly ILogger<PokerHub> _logger;
    private static readonly Dictionary<string, string> _userConnections = new();

    public PokerHub(GameTimerService timerService, ILogger<PokerHub> logger)
    {
        _timerService = timerService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections[userId] = Context.ConnectionId;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId) && _userConnections.ContainsKey(userId))
        {
            _userConnections.Remove(userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string? GetConnectionIdByUserId(string userId)
    {
        if (_userConnections.TryGetValue(userId, out var connectionId))
        {
            return connectionId;
        }

        return null;
    }

    public async Task JoinTableGroup(string tableId)
    {
        _logger.LogDebug($"[SignalR] Connection {Context.ConnectionId} joining table group: table_{tableId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"table_{tableId}");
        _logger.LogDebug($"[SignalR] Connection {Context.ConnectionId} successfully joined table group: table_{tableId}");
        int remainingTime = _timerService.GetRemainingTime(tableId);
        bool isPaused = _timerService.IsTimerPaused(tableId);
        await Clients.Caller.SendAsync("TimerUpdate", remainingTime, isPaused);
    }

    public async Task LeaveTableGroup(string tableId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"table_{tableId}");
    }

    public async Task GameUpdate(string tableId, SerializableGameSnapshot serializableSnapshot)
    {
        _logger.LogDebug($"[SignalR] Broadcasting GameUpdate to table_{tableId}. GameState: {serializableSnapshot.GameState}, Players: {serializableSnapshot.Players.Count}");
        await Clients.Group($"table_{tableId}").SendAsync("GameUpdate", serializableSnapshot);
    }

    public async Task PlayerAction(string tableId, string playerName, string playerPosition, PlayerAction action, decimal amount)
    {
        await Clients.Group($"table_{tableId}").SendAsync("PlayerAction", playerName, playerPosition, action.ToString(), amount);
    }

    public async Task SendChatMessage(string tableId, string userName, string message)
    {
        await Clients.Group($"table_{tableId}").SendAsync("ReceiveChatMessage", userName, message);
    }

    public async Task BroadcastGameHistoryEvent(string tableId, GameHistoryEvent gameEvent)
    {
        await Clients.Group($"table_{tableId}").SendAsync("GameHistoryEvent", gameEvent);
    }

    public async Task SendPlayerCards(string connectionId, GameHistoryEvent gameEvent)
    {
        await Clients.Client(connectionId).SendAsync("PlayerCards", gameEvent);
    }
}