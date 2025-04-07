// Services/PokerGameService.cs
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PokerApp.Data;
using PokerApp.Models;
using PokerApp.Models.Enums;
using Microsoft.AspNetCore.SignalR;
using PokerApp.Hubs;
using Microsoft.IdentityModel.Tokens;

namespace PokerApp.Services;

public class PokerGameService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<PokerGameService> _logger;
    private readonly IHubContext<PokerHub> _hubContext;
    private readonly GameTimerService _timerService;
    private readonly Dictionary<string, List<GameHistoryEvent>> _tableHistories = new();
    private readonly Dictionary<string, int> _tableGameCounts = new();

    public PokerGameService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<PokerGameService> logger,
        IHubContext<PokerHub> hubContext, GameTimerService timerService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _hubContext = hubContext;
        _timerService = timerService;
    }

    public async Task<PokerTable> CreateTableAsync(string ownerId, string tableName, decimal bigBlind, int timerSeconds)
    {
        _logger.LogDebug("[CreateTableAsync] tableName: " + tableName);
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var owner = await context.Users.FindAsync(ownerId)
            ?? throw new InvalidOperationException("User not found");

            // Generate a unique 6-digit table code
            var random = new Random();
            string tableCode;
            do
            {
                tableCode = random.Next(100000, 999999).ToString();
            } while (await context.PokerTables.AnyAsync(t => t.TableCode == tableCode));

            var table = new PokerTable
            {
                Name = tableName,
                OwnerId = ownerId,
                Owner = owner,
                TimerSeconds = timerSeconds,
                TimeRemaining = timerSeconds,
                BigBlind = bigBlind,
                SmallBlind = bigBlind / 2,
                TableCode = tableCode,
                TablePositions = [.. Enumerable.Repeat(-1, 10)],
                playerLeaves = []
            };

            context.PokerTables.Add(table);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Created Table: " + tableName);

            return table;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating poker table: {TableName}", tableName);
            throw;
        }
        
    }

    public async Task<GameSnapshot> GetGameStateAsync(string tableId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var table = await GetCompleteTable(context, tableId);

        if (table == null)
            throw new InvalidOperationException("Table not found or inactive");

        return table.GetGameSnapshot();
    }

    public async Task<PokerTable> JoinTableAsync(string userId, string tableCode, int position, decimal stack)
    {
        _logger.LogDebug($"[JoinTableAsync] Player {userId} joining table {tableCode} at position {position}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            var table = await context.PokerTables
                .Where(t => t.TableCode == tableCode && t.IsActive)
                .Include(t => t.Game)
                .FirstOrDefaultAsync();

            if (table == null || string.IsNullOrEmpty(table.Id))
                throw new InvalidOperationException("Table not found or inactive");

            if (position < 1 || position > 10)
                throw new ArgumentException("Invalid position. Must be between 1 and 10.");

            bool isOccupied = await context.GamePlayers
                .AnyAsync(p => p.PokerTableId == table.Id && p.Position == position && p.IsSeated);

            if (isOccupied)
                throw new InvalidOperationException("This position is already taken.");

            var existingPlayer = await context.GamePlayers
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PokerTableId == table.Id);

            bool preGame = table.Game == null ? true : false;

            if (existingPlayer != null)
            {
                existingPlayer.Position = position;
                existingPlayer.IsSeated = true;
                existingPlayer.Stack = stack;
                existingPlayer.IsInGame = true;
                existingPlayer.HasFolded = !preGame;
            }
            else
            {
                var newPlayer = new GamePlayer
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    PokerTableId = table.Id,
                    Position = position,
                    Stack = stack,
                    IsSeated = true,
                    IsInGame = true,
                    HasFolded = !preGame,
                    Hand = new List<Card>()
                };

                context.GamePlayers.Add(newPlayer);
            }

            await context.SaveChangesAsync();
            table = await GetCompleteTable(context, table.Id, true);
            if (table == null) throw new InvalidOperationException("No table");

            await UpdateTablePositionsDirectly(context, table.Id, [.. table.Players]);
            await context.SaveChangesAsync();
            if (existingPlayer != null && table.playerLeaves.Contains(existingPlayer.UserId))
            {
                table.playerLeaves.Remove(existingPlayer.UserId);
                _logger.LogDebug($"[JoinTableAsync] Player {existingPlayer.Name} rejoined before removal, removing from playerLeaves list");
            }

            foreach (var player in table.Players)
            {
                if (string.IsNullOrEmpty(player.Name) && player.User != null)
                {
                    player.Name = string.IsNullOrEmpty(player.User.DisplayName) ? "Player " + player.Position : player.User.DisplayName;
                    _logger.LogDebug($"[JoinTableAsync] Set player name to: {player.Name}");
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            var snapshot = await GetTableSnapshot(context, table.Id);
            await BroadcastGameUpdateNoLoad(table.Id, snapshot);

            _logger.LogInformation($"User {userId} joined table {table.Name}");

            return await GetCompleteTable(context, table.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[DB] Error joining table: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task UpdateTablePositionsDirectly(ApplicationDbContext context, string tableId, List<GamePlayer> players)
    {
        try
        {
            var tablePositions = Enumerable.Repeat(-1, 10).ToList();
            _logger.LogDebug("[UpdateTablePositionsDirectly] playerCount: " + players.Count);

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.Position >= 1 && player.Position <= 10)
                {
                    tablePositions[player.Position - 1] = i;
                }
            }

            string positionsJson = JsonSerializer.Serialize(tablePositions);

            // Use raw SQL to avoid EF tracking issues
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE PokerTables SET TablePositionsJson = {0} WHERE Id = {1}",
                positionsJson, tableId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating table positions: {ex.Message}");
        }
    }

    private async Task<GameSnapshot> GetTableSnapshot(ApplicationDbContext context, string tableId)
    {
        var table = await GetCompleteTable(context, tableId);

        if (table == null)
            return new GameSnapshot(null) { TableId = tableId };

        var snapshot = table.GetGameSnapshot();

        if (snapshot.Pot == null)
        {
            snapshot.Pot = table.Game.GetPotFromJson();
            _logger.LogDebug($"[GetTableSnapshot] Created pot from JSON with amount: ${snapshot.Pot?.Amount ?? 0}");
        }
        else
        {
            _logger.LogDebug($"[GetTableSnapshot] Snapshot has pot with amount: ${snapshot.Pot.Amount}");
        }

        return snapshot;
    }

    public async Task<PokerTable> GetCompleteTable(ApplicationDbContext context, string tableId, bool tracked = false)
    {
        context.ChangeTracker.Clear();
        PokerTable? table;

        if (tracked)
        {
            table = await context.PokerTables
                .Include(t => t.Game)
                .Include(t => t.Owner)
                .Include(t => t.Players)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(t => t.Id == tableId);
        }
        else
        {
            table = await context.PokerTables
            .Include(t => t.Game)
            .Include(t => t.Owner)
            .Include(t => t.Players)
            .ThenInclude(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tableId);
        }


        if (table != null)
        {
            table.Players = [.. table.Players.OrderBy(p => p.Position)];
            return table;
        }

        return new PokerTable();
    }

    public async Task<bool> ToggleTableActive(string tableId)
    {
        _logger.LogDebug($"[ToggleTableActive] Toggling table {tableId}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            var table = await context.PokerTables.FindAsync(tableId);

            if (table != null)
            {
                table.IsActive = !table.IsActive;
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error toggling table: {ex}");
        }

        return false;
    }

    public async Task<bool> DeleteTableAsync(string tableId)
    {
        _logger.LogDebug($"[DeleteTableAsync] Deleting table {tableId}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            var table = await context.PokerTables.FindAsync(tableId);

            if (table != null)
            {
                table.IsActive = false;
                context.PokerTables.Remove(table);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting table: {ex}");
        }

        return false;
    }

    public async Task<GameSnapshot> StartGameAsync(string tableId, bool isNewGame = false)
    {
        _logger.LogInformation($"Starting game for table {tableId}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            PokerTable table = await context.PokerTables
                .Include(t => t.Game)
                .FirstOrDefaultAsync(t => t.Id == tableId && t.IsActive)
                ?? throw new InvalidOperationException("Table not found or inactive");

            List<GamePlayer> players = await context.GamePlayers
                .Where(p => p.PokerTableId == tableId)
                .Include(p => p.User)
                .ToListAsync();

            table.Players = [.. players.OrderBy(p => p.Position)];

            int seatedPlayerCount = table.Players.Count(p => p.IsInGame && p.IsSeated && p.Stack > 0);
            _logger.LogDebug($"Found {seatedPlayerCount} seated players");

            if (seatedPlayerCount < 2)
                throw new InvalidOperationException("Need at least 2 players to start a game");

            if (isNewGame || table.Game == null)
            {
                var pokerGame = new PokerGame
                {
                    TableId = tableId,
                    GameTable = table,
                };
                pokerGame.InitializeGame();
                table.Game = pokerGame;
            }
            else
            {
                if (table.playerLeaves.Count > 0)
                {
                    foreach (string id in table.playerLeaves)
                    {
                        GamePlayer? player = players.FirstOrDefault(p => p.UserId == id);
                        if (player == null) continue;
                        _logger.LogDebug($"[StartGameAsync] Player {player.Name} is being removed");
                        bool removeResult = players.Remove(player);
                        context.GamePlayers.Remove(player);
                    }

                    table.playerLeaves = [];
                    table.Players = [.. players.OrderBy(p => p.Position)];
                    await UpdateTablePositionsDirectly(context, tableId, table.Players.ToList());
                    await context.SaveChangesAsync();
                }

                foreach (var player in table.Players)
                {
                    player.HandCardsJson = "[]";
                    player.CurrentBet = 0;
                    player.HasFolded = false;
                    player.IsDealer = false;
                    player.PokerPosition = PokerPosition.Other;
                }

                table.Game.Reset();
                table.Game.MoveDealerButton();
            }

            //if (isNewGame || !_tableGameCounts.ContainsKey(tableId))
            //{
            //    _tableGameCounts[tableId] = 1;
            //}
            //else
            //{
            //    _tableGameCounts[tableId]++;
            //}

            //int gameNumber = _tableGameCounts[tableId];

            //await AddGameHistoryEvent(tableId, GameHistoryEvent.GameStart(gameNumber, seatedPlayerCount));
            await GameHistoryExtension.TrackGameStart(_hubContext, tableId, table);

            var activePlayers = table.Players.Where(p => p.IsSeated && p.IsInGame && p.Stack > 0).ToList();
            DealCardsToPlayers(table.Game.GameDeck, activePlayers, table.Id);

            table.Game.SetBlinds();
            table.Game.BetBlinds();
            // GameHistoryEvent for blinds??
            table.Game.GameDeck = table.Game.GameDeck;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            await StartTimer(table.Id, table.TimerSeconds);

            _logger.LogInformation($"[StartGameAsync] Game has started for {table.Name} with {table.Players.Count} players");

            return await GetAndBroadcastFreshSnapshot(context, tableId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error starting game in service: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<GameSnapshot> GetAndBroadcastFreshSnapshot(ApplicationDbContext context, string tableId)
    {
        return await GetAndBroadcastFreshSnapshotWithWinners(context, tableId, null);
    }

    public async Task<GameSnapshot> GetAndBroadcastFreshSnapshotWithWinners(ApplicationDbContext context, string tableId, Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>>? winners)
    {
        var freshTable = await GetCompleteTable(context, tableId);

        var snapshot = freshTable.GetGameSnapshot();
        if (winners != null)
        {
            snapshot.AllWinners = winners;
        }

        await BroadcastGameUpdateNoLoad(tableId, snapshot);

        return snapshot;
    }

    private void DealCardsToPlayers(Deck deck, List<GamePlayer> players, string tableId)
    {
        _logger.LogDebug("[Deck] dealing cards to players " + deck.Cards.Count);

        // Deal first card to each player
        foreach (var player in players)
        {
            var card = DealCardFromDeck(deck);
            var hand = new List<Card> { card };
            player.HandCardsJson = JsonSerializer.Serialize(hand);
        }

        // Deal second card to each player
        foreach (var player in players)
        {
            var card = DealCardFromDeck(deck);
            var hand = JsonSerializer.Deserialize<List<Card>>(player.HandCardsJson ?? "[]") ?? new List<Card>();
            if (hand.IsNullOrEmpty()) throw new InvalidOperationException("hand should have first card when dealing second card");
            hand.Add(card);
            player.HandCardsJson = JsonSerializer.Serialize(hand);
            var connectionId = PokerHub.GetConnectionIdByUserId(player.UserId);
            if (connectionId != null)
            {
                var playerName = player.User?.DisplayName ?? "Player " + player.Position;
                _ = GameHistoryExtension.TrackPlayerCards(_hubContext, tableId, connectionId, playerName, player.Hand);
            }
        }
    }

    private Card DealCardFromDeck(Deck deck)
    {
        if (deck.Cards.Count == 0)
            throw new InvalidOperationException("No cards left in deck");

        var card = deck.Cards.Last();
        deck.Cards.RemoveAt(deck.Cards.Count - 1);
        deck.RemovedCards.Add(card);
        return card;
    }

    private int FindNextActivePosition(List<GamePlayer> players, int currentPosition)
    {
        if (players.Count <= 1)
            return 0;

        int nextPos = (currentPosition + 1) % players.Count;
        _logger.LogDebug($"Find Next Active Position, nextPos = {nextPos} player: {players[nextPos].User.DisplayName} : {players[nextPos].IsInGame}");

        int counter = 0;
        while ((!players[nextPos].IsInGame) && counter < players.Count)
        {
            nextPos = (nextPos + 1) % players.Count;
            _logger.LogDebug($"Looping, nextPos = {nextPos} player: {players[nextPos].User.DisplayName} : {players[nextPos].IsInGame}");
            counter++;
        }

        return nextPos;
    }

    private string SerializeDeck(Deck deck)
    {
        var options = new JsonSerializerOptions { WriteIndented = false };

        var cardsArray = deck.Cards.Select(c => new { c.Suit, c.Rank }).ToArray();
        var removedArray = deck.RemovedCards.Select(c => new { c.Suit, c.Rank }).ToArray();

        var jsonObject = new
        {
            Cards = cardsArray,
            RemovedCards = removedArray
        };

        return JsonSerializer.Serialize(jsonObject, options);
    }

    private async Task BroadcastGameUpdateNoLoad(string tableId, GameSnapshot snapshot)
    {
        try
        {
            var serializableSnapshot = SerializableGameSnapshot.FromGameSnapshot(snapshot);
            _logger.LogDebug($"[SignalR] Broadcasting game update for table {tableId}, GameState: {serializableSnapshot.GameState}");
            await _hubContext.Clients.Group($"table_{tableId}").SendAsync("GameUpdate", serializableSnapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting game update: {ex.Message}");
        }
    }

    public async Task<GameSnapshot> HandleAction(string tableId, string userId, PlayerAction action, decimal bet = 0.0m)
    {
        _logger.LogDebug($"[HandleAction] Handling player action: {action} from {userId} on table {tableId}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            var table = await GetCompleteTable(context, tableId, true);
            var currentPlayer = table.Players.FirstOrDefault(p => p.UserId == userId);
            if (currentPlayer == null)
                throw new InvalidOperationException("Player not found at this table");

            int playerIndex = table.Game.GetPlayerIndex(currentPlayer);

            if (table.Game.CurrentTurnPlayerPosition != playerIndex)
            {
                _logger.LogError($"CurrentTurnPosition: {table.Game.CurrentTurnPlayerPosition} playerIndex: {playerIndex}");
                throw new InvalidOperationException("It's not your turn");
            }

            switch (action)
            {
                case PlayerAction.BET:
                    // No more blind option as soon as there's any raise
                    table.Game.BlindOption = false;
                    HandleBet(table, currentPlayer, bet);
                    _logger.LogDebug($"[HandleAction] After bet: Current pot amount is ${table.Game.GamePot.Amount}, round pot: ${table.Game.CurrentRoundPotAmount}");
                    break;
                case PlayerAction.CALL:
                    if (table.Game.BlindOption && currentPlayer.PokerPosition == PokerPosition.BigBlind)
                    {
                        _logger.LogDebug("[HandleAction] Big Blind Checks");
                        table.Game.BlindOption = false;
                        table.Game.EndRound();
                    }
                    else
                    {
                        _logger.LogDebug("[HandleAction] Call but not changing blind option");
                        bet = table.Game.CurrentBetAmount - currentPlayer.CurrentBet;
                        decimal callingBet = Math.Min(bet, currentPlayer.Stack);
                        HandleBet(table, currentPlayer, callingBet);
                    }
                    break;
                case PlayerAction.CHECK:
                    table.Game.SetNextTurn();
                    break;
                case PlayerAction.FOLD:
                    currentPlayer.HasFolded = true;
                    table.Game.SetNextTurn();
                    break;
            }

            //await AddGameHistoryEvent(tableId, GameHistoryEvent.PlayerAction(playerName, action, action == PlayerAction.BET ? bet : null));
            await GameHistoryExtension.TrackPlayerAction(_hubContext, tableId, currentPlayer.Name, action, bet);
            table.Game.PotJson = JsonSerializer.Serialize(table.Game.GamePot);
            _logger.LogDebug($"[HandleAction] Before save, pot amount: ${table.Game.GamePot.Amount}");

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogDebug($"After save for action: {table.Players.ToList()[0].Name} : {table.Players.ToList()[0].User.DisplayName}");

            var snapshot = await GetAndBroadcastFreshSnapshot(context, tableId);
            await BroadcastPlayerAction(tableId, currentPlayer.User?.DisplayName ?? "Unknown",
                currentPlayer.Position.ToString(), action, bet);

            _logger.LogInformation($"[HandleAction] {action} has been completed for {currentPlayer.User?.DisplayName ?? currentPlayer.UserId}");

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[DB] Error handling action: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private void HandleBet(PokerTable table, GamePlayer currentPlayer, decimal bet)
    {
        if (bet < 0 || bet > currentPlayer.Stack)
            throw new InvalidOperationException("Invalid bet amount");

        _logger.LogDebug($"[HandleBet] Player {currentPlayer.User?.DisplayName ?? "Unknown"} betting ${bet}, round pot before is ${table.Game.CurrentRoundPotAmount}");
        table.Game.PlayerBet(currentPlayer, bet);
        table.Game.SetNextTurn();
    }

    private void EnsurePotConsistency(PokerTable table)
    {
        // Calculate what the pot should be based on player bets
        decimal totalBets = table.Players.Sum(p => p.CurrentBet);

        _logger.LogDebug($"[EnsurePotConsistency] Ensuring pot consistency. Current round pot: ${table.Game.CurrentRoundPotAmount}, Player bets: ${totalBets}");

        // If there's a discrepancy, log it
        if (table.Game.CurrentRoundPotAmount != totalBets)
        {
            _logger.LogDebug($"[EnsurePotConsistency] WARNING: Pot discrepancy detected! Fixing: was ${table.Game.CurrentRoundPotAmount}, should be ${totalBets}");
            table.Game.CurrentRoundPotAmount = totalBets;
        }
    }

    public async Task<GameSnapshot> PlayNextStep(string tableId)
    {
        _logger.LogDebug($"[PlayNextStep] Playing next step for table {tableId}");
        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            var table = await GetCompleteTable(context, tableId, true);

            if (!table.Game.BetsMade)
                return table.GetGameSnapshot();

            Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> winners = new();

            // Create a deck reference to ensure it's properly initialized
            var deck = table.Game.GameDeck;

            _logger.LogInformation($"[PlayNextStep] Playing next step for table {tableId} from state {table.Game.State}");

            switch (table.Game.State)
            {
                case GameState.PREFLOP:
                    var burnCard = DealCardFromDeck(deck);
                    _logger.LogDebug($"[PlayNextStep] Burned card: {burnCard}");

                    var flopCards = new List<Card>();
                    for (int i = 0; i < 3; i++)
                    {
                        var card = DealCardFromDeck(deck);
                        flopCards.Add(card);
                        _logger.LogDebug($"[PlayNextStep] Dealt flop card: {card}");
                    }

                    var communityCards = table.Game.CommunityCards.ToList();
                    communityCards.AddRange(flopCards);

                    table.Game.CommunityCards = communityCards;
                    table.Game.State = GameState.FLOP;
                    table.Game.CurrentTurnPlayerPosition = table.Game.FindNextActivePlayerPosition(table.Game.CurrentDealerPosition);
                    table.Game.EndPlayerPosition = table.Game.CurrentTurnPlayerPosition;

                    //await AddGameHistoryEvent(tableId, GameHistoryEvent.CommunityCards(table.Game.State, communityCards));
                    await GameHistoryExtension.TrackCommunityCards(_hubContext, tableId, table.Game.State, communityCards);
                    _logger.LogDebug($"[PlayNextStep] Turn player position now: {table.Game.CurrentTurnPlayerPosition}, End player position now: {table.Game.EndPlayerPosition}");
                    break;

                case GameState.FLOP:
                    burnCard = DealCardFromDeck(deck);
                    _logger.LogDebug($"[PlayNextStep] Burned card: {burnCard}");

                    var turnCard = DealCardFromDeck(deck);
                    _logger.LogDebug($"[PlayNextStep] Dealt turn card: {turnCard}");

                    communityCards = table.Game.CommunityCards.ToList();
                    communityCards.Add(turnCard);

                    table.Game.CommunityCards = communityCards;
                    table.Game.State = GameState.TURN;
                    table.Game.CurrentTurnPlayerPosition = table.Game.FindNextActivePlayerPosition(table.Game.CurrentDealerPosition);
                    table.Game.EndPlayerPosition = table.Game.CurrentTurnPlayerPosition;

                    //await AddGameHistoryEvent(tableId, GameHistoryEvent.CommunityCards(table.Game.State, communityCards));
                    await GameHistoryExtension.TrackCommunityCards(_hubContext, tableId, table.Game.State, communityCards);
                    _logger.LogDebug($"[PlayNextStep] Turn player position now: {table.Game.CurrentTurnPlayerPosition}, End player position now: {table.Game.EndPlayerPosition}");
                    break;

                case GameState.TURN:
                    burnCard = DealCardFromDeck(deck);
                    _logger.LogDebug($"[PlayNextStep] Burned card: {burnCard}");

                    var riverCard = DealCardFromDeck(deck);
                    _logger.LogDebug($"[PlayNextStep] Dealt river card: {riverCard}");

                    communityCards = table.Game.CommunityCards.ToList();
                    communityCards.Add(riverCard);

                    table.Game.CommunityCards = communityCards;
                    table.Game.State = GameState.RIVER;
                    table.Game.CurrentTurnPlayerPosition = table.Game.FindNextActivePlayerPosition(table.Game.CurrentDealerPosition);
                    table.Game.EndPlayerPosition = table.Game.CurrentTurnPlayerPosition;

                    //await AddGameHistoryEvent(tableId, GameHistoryEvent.CommunityCards(table.Game.State, communityCards));
                    await GameHistoryExtension.TrackCommunityCards(_hubContext, tableId, table.Game.State, communityCards);
                    _logger.LogDebug($"[PlayNextStep] Turn player position now: {table.Game.CurrentTurnPlayerPosition}, End player position now: {table.Game.EndPlayerPosition}");
                    break;

                case GameState.RIVER:
                    _logger.LogInformation("[PlayNextStep] End of game reached, determining winners...");
                    winners = GetWinners(table);

                    foreach (var potAndWinners in winners)
                    {
                        foreach (var winner in potAndWinners.Value)
                        {
                            var playerName = winner.Key.User?.DisplayName ?? "Player " + winner.Key.Position;
                            //await AddGameHistoryEvent(tableId, GameHistoryEvent.Winner(
                            //    playerName,
                            //    winner.Value.Type,
                            //    potAndWinners.Key.Amount / potAndWinners.Value.Count,
                            //    winner.Value.BestCards
                            //));
                            var amountWon = potAndWinners.Key.Amount / potAndWinners.Value.Count;
                            await GameHistoryExtension.TrackWinner(_hubContext, tableId, playerName, winner.Value.Type, amountWon, winner.Value.BestCards);
                        }
                    }

                    //await AddGameHistoryEvent(tableId, GameHistoryEvent.GameEnd(_tableGameCounts[tableId]));
                    await GameHistoryExtension.TrackGameEnd(_hubContext, tableId);
                    PayOutWinners(table, winners);
                    await RecordGameHistoryAsync(tableId, table, winners);
                    table.Game.State = GameState.END;
                    break;
            }

            if (table.Game.GetBettingPlayers().Count > 1 && table.Game.State != GameState.END)
            {
                table.Game.BetsMade = false;
            }

            table.Game.DeckJson = SerializeDeck(deck);
            table.Game.CommunityCardsJson = JsonSerializer.Serialize(table.Game.CommunityCards);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogDebug($"After save: {table.Players.ToList()[0].Name} : {table.Players.ToList()[0].User.DisplayName}");

            return await GetAndBroadcastFreshSnapshotWithWinners(context, tableId, winners);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[PlayNextStep] Error playing next step: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> GetWinners(PokerTable table)
    {
        Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> winners = [];

        EnsurePotConsistency(table);

        // Make sure the pot includes all round bets
        if (table.Game.CurrentRoundPotAmount > 0)
        {
            table.Game.GamePot.Amount += table.Game.CurrentRoundPotAmount;
            table.Game.CurrentRoundPotAmount = 0;
            _logger.LogDebug($"[GetWinners] Added round bets to pot. Total pot: ${table.Game.GamePot.Amount}");

            // Update the pot JSON explicitly
            table.Game.PotJson = JsonSerializer.Serialize(table.Game.GamePot);
        }

        // Determine winners - log more debugging info
        _logger.LogDebug($"[GetWinners] Evaluating hands for {table.Players.Count(p => !p.HasFolded)} active players with {table.Game.CommunityCards.Count} community cards");
        winners = table.Game.DetermineWinners();

        if (winners.Count == 0 || winners.Values.Count == 0)
        {
            _logger.LogDebug("[GetWinners] WARNING: No winners determined! Assigning pot to remaining players");

            // Fallback: if no winners, distribute pot to any remaining players
            var activePlayers = table.Players.Where(p => p.IsSeated && p.IsInGame && !p.HasFolded).ToList();
            if (activePlayers.Count > 0)
            {
                // Create a simple winner entry for pot distribution
                var fallbackWinners = new Dictionary<GamePlayer, HandEvaluation>();
                foreach (var player in activePlayers)
                {
                    fallbackWinners[player] = new HandEvaluation
                    {
                        Type = HandType.HighCard,
                        BestCards = player.Hand.Take(5).ToList()
                    };
                }

                winners[table.Game.GamePot] = fallbackWinners;
                _logger.LogDebug($"[GetWinners] Assigned pot to {fallbackWinners.Count} active players as fallback");
            }
        }
        // Log winners for debugging
        foreach (var potAndWinners in winners)
        {
            _logger.LogDebug($"[GetWinners] Pot: ${potAndWinners.Key.Amount}");
            foreach (var winner in potAndWinners.Value)
            {
                _logger.LogDebug($"[GetWinners] Winner: {winner.Key.User?.DisplayName ?? "Unknown"} with {winner.Value.Type}");
            }
        }

        return winners;
    }

    public async Task AddChipsAsync(string tableId, string userId, decimal amount)
    {
        _logger.LogDebug($"[AddChipsAsync] Adding {amount} chips for player {userId} at table {tableId}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");

            var player = await context.GamePlayers
                .FirstOrDefaultAsync(p => p.PokerTableId == tableId && p.UserId == userId);

            if (player == null)
                throw new InvalidOperationException("Player not found at this table");

            player.Stack += amount;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            await GetAndBroadcastFreshSnapshot(context, tableId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AddChipsAsync] Error adding chips: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task BroadcastPlayerAction(string tableId, string playerName, string position, PlayerAction action, decimal amount)
    {
        try
        {
            await _hubContext.Clients.Group($"table_{tableId}")
                .SendAsync("PlayerAction", playerName, position, action.ToString(), amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting player action");
        }
    }

    public async Task StandUpAsync(string tableId, string userId)
    {
        _logger.LogDebug($"[StandUpAsync] Player {userId} standing up from table {tableId}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            var player = await context.GamePlayers
                .FirstOrDefaultAsync(p => p.PokerTableId == tableId && p.UserId == userId);

            if (player == null)
                throw new InvalidOperationException("Player not found at this table");

            // Update the player's status
            player.IsSeated = false;
            player.IsInGame = false;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            await GetAndBroadcastFreshSnapshot(context, tableId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[StandUpAsync] Error standing up: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task SitDownAsync(string tableId, string userId)
    {
        _logger.LogDebug($"[SitDownAsync] Player {userId} sitting down at table {tableId}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            var player = await context.GamePlayers
                .FirstOrDefaultAsync(p => p.PokerTableId == tableId && p.UserId == userId);

            if (player == null)
                throw new InvalidOperationException("Player not found at this table");

            // Update the player's status
            player.IsSeated = true;
            player.IsInGame = true;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            await GetAndBroadcastFreshSnapshot(context, tableId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[SitDownAsync] Error sitting down: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task LeaveTableAsync(string tableId, string userId)
    {
        _logger.LogDebug($"[LeaveTableAsync] Player {userId} leaving table {tableId}");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            PokerTable table = await GetCompleteTable(context, tableId, true);
            var player = table.Players.FirstOrDefault(p => p.UserId == userId) ?? throw new InvalidOperationException("Player not found");

            if (table.Game != null && table.Game.State != GameState.NOT_STARTED && table.Game.State != GameState.END)
            {
                // Game is going on so be careful with players leaving...
                _logger.LogDebug("[LeaveTableAsync] adding player to playerLeaves");
                table.playerLeaves.Add(userId);
            }
            else
            {
                table.Players.Remove(player);
                _logger.LogDebug("[LeaveTableAsync] Player removed");
            } 

            await context.SaveChangesAsync();

            if (table != null)
            {
                table.Players = [.. table.Players.OrderBy(p => p.Position)];
                // Use SQL to update positions instead of EF navigation properties
                await UpdateTablePositionsDirectly(context, tableId, [.. table.Players]);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            await GetAndBroadcastFreshSnapshot(context, tableId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[LeaveTableAsync] Error standing up: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private void PayOutWinners(PokerTable table, Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> allWinners)
    {
        _logger.LogDebug($"[PayOutWinners] Paying out winners for {allWinners.Count} pots");

        foreach (var potAndWinnersPairing in allWinners)
        {
            Pot pot = potAndWinnersPairing.Key;
            Dictionary<GamePlayer, HandEvaluation> potWinners = potAndWinnersPairing.Value;

            if (potWinners.Count == 0)
            {
                _logger.LogDebug($"[PayOutWinners] WARNING: No winners for pot of ${pot.Amount}");
                continue;
            }

            decimal amountPerWinner = pot.Amount / potWinners.Count;
            _logger.LogDebug($"[PayOutWinners] Splitting pot ${pot.Amount} between {potWinners.Count} winners, ${amountPerWinner} each");

            foreach (var winner in potWinners)
            {
                GamePlayer winningPlayer = winner.Key;
                winningPlayer.Stack += amountPerWinner;
                _logger.LogDebug($"[PayOutWinners] Paid ${amountPerWinner} to {winningPlayer.User?.DisplayName ?? "Unknown"}, new stack: ${winningPlayer.Stack}");
            }
        }
    }

    private async Task RecordGameHistoryAsync(string tableId, PokerTable table, Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> winners)
    {
        // Extract winners and their hands for recording
        var gameDetails = new List<object>();
        string? winnerId = null;
        string? handType = null;

        foreach (var potAndWinners in winners)
        {
            foreach (var winner in potAndWinners.Value)
            {
                // Find user by display name
                var winnerPlayer = table.Players.FirstOrDefault(p => p.User.DisplayName == winner.Key.Name);

                if (winnerPlayer != null)
                {
                    gameDetails.Add(new
                    {
                        WinnerName = winner.Key.Name,
                        PotAmount = potAndWinners.Key.Amount,
                        HandType = winner.Value.Type.ToString(),
                        Cards = winner.Value.BestCards.Select(c => c.ToString()).ToList()
                    });

                    // Set the first winner as the main winner for the game history
                    if (winnerId == null)
                    {
                        winnerId = winnerPlayer.UserId;
                        handType = winner.Value.Type.ToString();
                    }

                    // Update player stats
                    await UpdatePlayerStatsAsync(tableId, winnerPlayer.UserId);
                }
            }
        }

        var gameHistory = new GameHistory
        {
            PokerTableId = tableId,
            PotAmount = table.Game.GamePot.Amount,
            WinnerId = winnerId,
            HandType = handType ?? "Unknown",
            GameDetails = JsonSerializer.Serialize(gameDetails),
            EndedAt = DateTime.UtcNow
        };

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            context.GameHistories.Add(gameHistory);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[RecordGameHistoryAsync] Error recording game history: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task UpdatePlayerStatsAsync(string tableId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var player = await context.GamePlayers
            .Include(p => p.User)
            .ThenInclude(u => u.Stats)
            .FirstOrDefaultAsync(p => p.PokerTableId == tableId && p.UserId == userId);

            if (player != null && player.User.Stats != null)
            {
                player.User.Stats.GamesPlayed++;
                player.User.Stats.GamesWon++;
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[UpdatePlayerStatsAsync] Error recording player stats: {ex.Message}");
            await transaction.RollbackAsync();
            throw;
        }
        
    }

    public async Task StartTimer(string tableId, int durationSeconds)
    {
        await _timerService.StartTimer(tableId, durationSeconds);
    }

    public async Task PauseTimer(string tableId)
    {
        await _timerService.PauseTimer(tableId);
    }

    public async Task ResumeTimer(string tableId)
    {
        await _timerService.ResumeTimer(tableId);
    }

    public async Task ResetTimer(string tableId, int durationSeconds)
    {
        await _timerService.ResetTimer(tableId, durationSeconds);
    }

    public async Task RefreshTimer(string tableId, int durationSeconds)
    {
        int timeLeft = _timerService.GetRemainingTime(tableId);
        if (timeLeft >= 0)
        {
            await _timerService.ResetTimer(tableId, durationSeconds);
        }
    }

    public void Log(string logType, string message)
    {
        switch (logType)
        {
            case "Info":
                _logger.LogInformation(message);
                break;
            case "Error":
                _logger.LogError(message);
                break;
            case "Debug":
                _logger.LogDebug(message);
                break;
        }
    }

    public List<GameHistoryEvent> GetTableHistory(string tableId)
    {
        return GameHistoryExtension.GetTableHistory(tableId);
    }

    //public async Task AddGameHistoryEvent(string tableId, GameHistoryEvent gameEvent)
    //{
    //    if (!_tableHistories.ContainsKey(tableId))
    //    {
    //        _tableHistories[tableId] = new List<GameHistoryEvent>();
    //    }

    //    _tableHistories[tableId].Add(gameEvent);

    //    await _hubContext.Clients.Group($"table_{tableId}").SendAsync("GameHistoryEvent", gameEvent);
    //}

    //public List<GameHistoryEvent> GetTableHistory(string tableId)
    //{
    //    if (!_tableHistories.ContainsKey(tableId))
    //    {
    //        return new List<GameHistoryEvent>();
    //    }

    //    return _tableHistories[tableId];
    //}

    //public async Task SendPlayerCards(string tableId, string connectionId, string playerName, List<Card> cards)
    //{
    //    var gameEvent = GameHistoryEvent.PlayerCards(playerName, cards);

    //    if (!_tableHistories.ContainsKey(tableId))
    //    {
    //        _tableHistories[tableId] = new List<GameHistoryEvent>();
    //    }

    //    _tableHistories[tableId].Add(gameEvent);

    //    await _hubContext.Clients.Client(connectionId).SendAsync("PlayerCards", gameEvent);
    //}
}