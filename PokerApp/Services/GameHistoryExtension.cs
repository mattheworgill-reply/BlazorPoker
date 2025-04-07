// Services/GameHistoryExtension.cs
using Microsoft.AspNetCore.SignalR;
using PokerApp.Data;
using PokerApp.Hubs;
using PokerApp.Models;
using PokerApp.Models.Enums;

namespace PokerApp.Services;

// This class extends PokerGameService with history functionality
// Use this if you don't want to modify the original service directly
public static class GameHistoryExtension
{
    private static readonly Dictionary<string, List<GameHistoryEvent>> _tableHistories = new();
    private static readonly Dictionary<string, int> _tableGameCounts = new();

    // Call this method at the start of the app to initialize
    public static void InitializeHistoryTracking()
    {
        _tableHistories.Clear();
        _tableGameCounts.Clear();
    }

    // Track game start
    public static async Task TrackGameStart(IHubContext<PokerHub> hubContext, string tableId, PokerTable table)
    {
        if (!_tableGameCounts.ContainsKey(tableId))
        {
            _tableGameCounts[tableId] = 1;
        }
        else
        {
            _tableGameCounts[tableId]++;
        }

        int seatedPlayerCount = table.Players.Count(p => p.IsInGame && p.IsSeated && p.Stack > 0);
        var gameEvent = GameHistoryEvent.GameStart(_tableGameCounts[tableId], seatedPlayerCount);

        await AddGameHistoryEvent(hubContext, tableId, gameEvent);
    }

    // Track player cards (sent only to the specific player)
    public static async Task TrackPlayerCards(IHubContext<PokerHub> hubContext, string tableId,
        string connectionId, string playerName, List<Card> cards)
    {
        var gameEvent = GameHistoryEvent.PlayerCards(playerName, cards);

        if (!_tableHistories.ContainsKey(tableId))
        {
            _tableHistories[tableId] = new List<GameHistoryEvent>();
        }

        _tableHistories[tableId].Add(gameEvent);

        // Send only to the specific client
        await hubContext.Clients.Client(connectionId).SendAsync("PlayerCards", gameEvent);
    }

    // Track player action
    public static async Task TrackPlayerAction(IHubContext<PokerHub> hubContext, string tableId,
        string playerName, PlayerAction action, decimal? amount = null)
    {
        var gameEvent = GameHistoryEvent.PlayerAction(playerName, action, amount);

        await AddGameHistoryEvent(hubContext, tableId, gameEvent);
    }

    // Track community cards
    public static async Task TrackCommunityCards(IHubContext<PokerHub> hubContext, string tableId,
        GameState state, List<Card> communityCards)
    {
        var gameEvent = GameHistoryEvent.CommunityCards(state, communityCards);

        await AddGameHistoryEvent(hubContext, tableId, gameEvent);
    }

    // Track winner
    public static async Task TrackWinner(IHubContext<PokerHub> hubContext, string tableId,
        string playerName, HandType handType, decimal amount, List<Card> cards)
    {
        var gameEvent = GameHistoryEvent.Winner(playerName, handType, amount, cards);

        await AddGameHistoryEvent(hubContext, tableId, gameEvent);
    }

    // Track game end
    public static async Task TrackGameEnd(IHubContext<PokerHub> hubContext, string tableId)
    {
        if (!_tableGameCounts.ContainsKey(tableId))
        {
            _tableGameCounts[tableId] = 1;
        }

        var gameEvent = GameHistoryEvent.GameEnd(_tableGameCounts[tableId]);

        await AddGameHistoryEvent(hubContext, tableId, gameEvent);
    }

    // Generic method to add a game history event
    public static async Task AddGameHistoryEvent(IHubContext<PokerHub> hubContext, string tableId,
        GameHistoryEvent gameEvent)
    {
        if (!_tableHistories.ContainsKey(tableId))
        {
            _tableHistories[tableId] = new List<GameHistoryEvent>();
        }

        _tableHistories[tableId].Add(gameEvent);

        // Broadcast the event to all clients
        await hubContext.Clients.Group($"table_{tableId}").SendAsync("GameHistoryEvent", gameEvent);
    }

    // Get table history
    public static List<GameHistoryEvent> GetTableHistory(string tableId)
    {
        if (!_tableHistories.ContainsKey(tableId))
        {
            return new List<GameHistoryEvent>();
        }

        return _tableHistories[tableId];
    }

    // Get current game number
    public static int GetCurrentGameNumber(string tableId)
    {
        if (!_tableGameCounts.ContainsKey(tableId))
        {
            _tableGameCounts[tableId] = 1;
            return 1;
        }

        return _tableGameCounts[tableId];
    }

    // Clear table history (e.g., when a table is deleted)
    public static void ClearTableHistory(string tableId)
    {
        if (_tableHistories.ContainsKey(tableId))
        {
            _tableHistories.Remove(tableId);
        }

        if (_tableGameCounts.ContainsKey(tableId))
        {
            _tableGameCounts.Remove(tableId);
        }
    }
}