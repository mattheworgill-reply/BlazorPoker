using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PokerApp.Models.Enums;
using PokerApp.Models.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PokerApp.Models;

public class PokerGame //: IPokerGame
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TableId { get; set; } = string.Empty;
    public GameState State { get; set; } = GameState.NOT_STARTED;
    public int CurrentTurnPlayerPosition { get; set; } = 0;
    public int CurrentDealerPosition { get; set; } = 0;
    public int EndPlayerPosition { get; set; } = -1;
    public decimal CurrentBetAmount { get; set; } = 0.0m;
    public decimal CurrentRoundPotAmount { get; set; } = 0.0m;
    public bool BlindOption { get; set; } = true;
    public bool BetsMade { get; set; } = false;

    // JSON serialized fields
    public string? CommunityCardsJson { get; set; }
    public string? DeckJson { get; set; }
    public string? PotJson { get; set; }
    public string? WinnersJson { get; set; }
    public List<string> PlayerIds { get; set; } = [];

    public virtual PokerTable GameTable { get; set; } = null!;

    [NotMapped]
    public List<Card> CommunityCards
    {
        get
        {
            if (string.IsNullOrEmpty(CommunityCardsJson))
                return new List<Card>();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                List<Card> deserializedCards = JsonSerializer.Deserialize<List<Card>>(CommunityCardsJson, options) ?? new List<Card>();
                return deserializedCards;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deserializing community cards: {ex.Message}");
                return new List<Card>();
            }
        }
        set => CommunityCardsJson = JsonSerializer.Serialize(value);
    }

    // For Deck
    [NotMapped]
    public Deck GameDeck
    {
        get
        {
            // Cache the deserialized deck to avoid recreating it repeatedly
            if (_cachedDeck != null)
                return _cachedDeck;

            if (string.IsNullOrEmpty(DeckJson))
            {
                _cachedDeck = new Deck();
                return _cachedDeck;
            }

            try
            {
                _cachedDeck = DeserializeDeck(DeckJson);
                return _cachedDeck;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deserializing deck: {ex.Message}");
                _cachedDeck = new Deck();
                return _cachedDeck;
            }
        }
        set
        {
            _cachedDeck = value;
            DeckJson = SerializeDeck(value);
        }
    }

    // Add a private field to cache the deck instance
    private Deck? _cachedDeck;

    [NotMapped]
    public Pot GamePot
    {
        get
        {
            if (string.IsNullOrEmpty(PotJson))
                return new Pot(new List<int>());

            try
            {
                return DeserializePot(PotJson);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deserializing pot: {ex.Message}");
                return new Pot(new List<int>());
            }
        }
        set => PotJson = SerializePot(value);
    }

    [NotMapped]
    public Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> Winners
    {
        get
        {
            if (string.IsNullOrEmpty(WinnersJson))
                return new Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>>();

            try
            {
                return DeserializeWinners(WinnersJson);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deserializing winners: {ex.Message}");
                return new Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>>();
            }
        }
        set => WinnersJson = SerializeWinners(value);
    }

    private Deck DeserializeDeck(string json)
    {
        try
        {
            var deck = new Deck();
            deck.Cards.Clear();
            deck.RemovedCards.Clear();

            if (string.IsNullOrEmpty(json))
                return deck;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Cards", out var cardsElement) && cardsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var cardElement in cardsElement.EnumerateArray())
                {
                    if (cardElement.TryGetProperty("Suit", out var suitElement) &&
                        cardElement.TryGetProperty("Rank", out var rankElement))
                    {
                        var suit = (Suit)suitElement.GetInt32();
                        var rank = (Rank)rankElement.GetInt32();
                        deck.Cards.Add(new Card(suit, rank));
                    }
                }
            }

            if (root.TryGetProperty("RemovedCards", out var removedElement) && removedElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var cardElement in removedElement.EnumerateArray())
                {
                    if (cardElement.TryGetProperty("Suit", out var suitElement) &&
                        cardElement.TryGetProperty("Rank", out var rankElement))
                    {
                        var suit = (Suit)suitElement.GetInt32();
                        var rank = (Rank)rankElement.GetInt32();
                        deck.RemovedCards.Add(new Card(suit, rank));
                    }
                }
            }

            return deck;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deserializing deck: {ex.Message}");
            return new Deck();
        }
    }

    private string SerializeDeck(Deck deck)
    {
        var cardsArray = deck.Cards.Select(c => new { c.Suit, c.Rank }).ToArray();
        var removedArray = deck.RemovedCards.Select(c => new { c.Suit, c.Rank }).ToArray();

        var deckObject = new
        {
            Cards = cardsArray,
            RemovedCards = removedArray
        };

        return JsonSerializer.Serialize(deckObject);
    }

    public Pot GetPotFromJson()
    {
        var jsonPot = PotJson != null ? JsonSerializer.Deserialize<Pot>(PotJson) : new Pot([]);
        return jsonPot ?? new Pot([]);
    }

    private Pot DeserializePot(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        decimal amount = 0;
        var players = new List<int>();

        if (root.TryGetProperty("Amount", out var amountProp))
            amount = amountProp.GetDecimal();

        if (root.TryGetProperty("Players", out var playersProp) && playersProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in playersProp.EnumerateArray())
            {
                players.Add(item.GetInt32());
            }
        }

        var result = new Pot(players, amount);

        if (root.TryGetProperty("SidePot", out var sidePotProp) &&
            sidePotProp.ValueKind != JsonValueKind.Null)
        {
            result.SidePot = DeserializePot(sidePotProp.GetRawText());
        }

        return result;
    }

    private string SerializePot(Pot pot)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        var jsonObject = new Dictionary<string, object>
        {
            ["Amount"] = pot.Amount,
            ["Players"] = pot.Players
        };

        if (pot.SidePot != null)
        {
            Dictionary<string, object>? jsonSidePot = JsonSerializer.Deserialize<Dictionary<string, object>>(SerializePot(pot.SidePot));
            if (jsonSidePot == null) throw new Exception("Pot serialization broke");

            jsonObject["SidePot"] = jsonSidePot;
        }
        else
        {
            jsonObject["SidePot"] = new Dictionary<string, object>();
        }

        return JsonSerializer.Serialize(jsonObject, options);
    }

    private string SerializeWinners(Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> winners)
    {
        var serializablePotWinners = SerializableGameData.GetSerializablePotWinners(winners);
        return JsonSerializer.Serialize(serializablePotWinners);
    }

    private Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> DeserializeWinners(string json)
    {
        var serPotWinnersList = JsonSerializer.Deserialize<List<SerializablePotWinners>>(json);
        if (serPotWinnersList == null)
            return new Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>>();

        return SerializableGameData.GetDeserializablePotWinners(serPotWinnersList, [.. GameTable.Players]);
    }

    public void InitializeGame()
    {
        State = GameState.PREFLOP;
        CommunityCards = [];
        GameDeck = new Deck();
        GamePot = new Pot([]);
        DeckJson = SerializeDeck(GameDeck);
        PotJson = SerializePot(GamePot);
        CommunityCardsJson = "[]";
        SetPlayers();
    }

    public void SetPlayers()
    {
        List<GamePlayer> tablePlayers = [.. GameTable.Players];
        PlayerIds.Clear();
        foreach (GamePlayer p in tablePlayers)
        {
            PlayerIds.Add(p.Id);
        }
    }

    public void MoveDealerButton()
    {
        var oldDealerPlayer = GetPlayerAtPosition(CurrentDealerPosition);
        if (oldDealerPlayer != null)
        {
            oldDealerPlayer.IsDealer = false;
            oldDealerPlayer.PokerPosition = PokerPosition.Other;
        }

        CurrentDealerPosition = FindNextActivePlayerPosition(CurrentDealerPosition, preHand: true);
    }

    public void SetBlinds()
    {
        var dealerPlayer = GetPlayerAtPosition(CurrentDealerPosition);
        if (dealerPlayer == null) return;

        int smallBlindPos = FindNextActivePlayerPosition(CurrentDealerPosition, preHand: true);
        var smallBlindPlayer = GetPlayerAtPosition(smallBlindPos);
        if (smallBlindPlayer == null) return;

        var bigBlindPlayer = dealerPlayer;

        if (GetActivePlayers(preHand: true).Count > 2)
        {
            int bigBlindPos = FindNextActivePlayerPosition(smallBlindPos, preHand: true);
            bigBlindPlayer = GetPlayerAtPosition(bigBlindPos);
        }

        if (bigBlindPlayer == null) return;

        dealerPlayer.IsDealer = true;
        dealerPlayer.PokerPosition = PokerPosition.Dealer;
        smallBlindPlayer.PokerPosition = PokerPosition.SmallBlind;
        bigBlindPlayer.PokerPosition = PokerPosition.BigBlind;
        EndPlayerPosition = CurrentDealerPosition;
        CurrentTurnPlayerPosition = smallBlindPos;
    }

    public void BetBlinds()
    {
        var smallBlindPlayer = GetPlayerAtPosition(CurrentTurnPlayerPosition);
        if (smallBlindPlayer == null) return;

        PlayerBet(smallBlindPlayer, GameTable.SmallBlind);

        int bigBlindPos = FindNextActivePlayerPosition(CurrentTurnPlayerPosition);
        var bigBlindPlayer = GetPlayerAtPosition(bigBlindPos);
        if (bigBlindPlayer == null) return;

        PlayerBet(bigBlindPlayer, GameTable.BigBlind);
        CurrentTurnPlayerPosition = FindNextActivePlayerPosition(bigBlindPos);
    }

    public List<GamePlayer> GetActivePlayers(bool onlyBettors = false, bool preHand = false)
    {
        var activePlayers = new List<GamePlayer>();

        for (int i = 0; i < PlayerIds.Count; i++)
        {
            GamePlayer? p = GetPlayerAtPosition(i);
            if (p != null && p.IsActive(onlyBettors) && (p.Hand.Count == 2 || preHand))
            {
                activePlayers.Add(p);
            }
        }

        return activePlayers;
    }

    public List<GamePlayer> GetBettingPlayers() => GetActivePlayers(onlyBettors: true);

    public List<GamePlayer> GetPreHandPlayers() => GetActivePlayers(onlyBettors: true, preHand: true);

    public List<GamePlayer> GetActivePlayersForPot(List<int> playerIndices)
    {
        var potPlayers = new List<GamePlayer>();

        foreach (int i in playerIndices)
        {
            if (i >= 0 && i < PlayerIds.Count)
            {
                var p = GetPlayerAtPosition(i);
                if (p != null && p.IsActive())
                {
                    potPlayers.Add(p);
                }
            }
        }
        return potPlayers;
    }

    public void SetNextTurn()
    {
        List<GamePlayer> players = GetActivePlayers();
        if (players.Count(p => !p.HasFolded) <= 1)
        {
            EndRound();
            return;
        }

        int nextTurn = FindNextActivePlayerPosition(CurrentTurnPlayerPosition);

        Boolean bigBlindEnded = EndPlayerPosition == CurrentTurnPlayerPosition
                                && BlindOption
                                && GetPlayerAtPosition(CurrentTurnPlayerPosition)?.PokerPosition == PokerPosition.BigBlind;
        Boolean reachedAggressor = EndPlayerPosition == nextTurn
                                && !(BlindOption && GetPlayerAtPosition(nextTurn)?.PokerPosition == PokerPosition.BigBlind);

        if (bigBlindEnded || reachedAggressor)
        {
            EndRound();
        }
        else
        {
            CurrentTurnPlayerPosition = nextTurn;
        }
    }

    public void EndRound()
    {
        BetsMade = true;
        CurrentTurnPlayerPosition = -1;
        EndPlayerPosition = -1;
        AddCurrentRoundToPot();
        ResetPlayerBets();
    }

    public int FindNextActivePlayerPosition(int currentPosition, bool preHand = false)
    {
        if (PlayerIds.Count == 0)
            return -1;

        if (PlayerIds.Count == 1)
            return 0;

        int nextPosition = PlayerIds.Count == currentPosition + 1 ? 0 : currentPosition + 1;

        if (EndPlayerPosition == nextPosition)
        {
            return nextPosition;
        }

        var nextPlayer = GetPlayerAtPosition(nextPosition);

        int safeguardCount = 0;
        while (safeguardCount < PlayerIds.Count && nextPosition != currentPosition && nextPlayer != null
                && ((!preHand && nextPlayer.Hand.Count < 2) || !nextPlayer.IsActive(onlyBettors: true)))
        {
            nextPosition = PlayerIds.Count == nextPosition + 1 ? 0 : nextPosition + 1;
            nextPlayer = GetPlayerAtPosition(nextPosition);
            safeguardCount++;
        }

        return nextPosition;
    }

    public void PlayerBet(GamePlayer currentPlayer, decimal bet)
    {
        decimal totalBet = currentPlayer.CurrentBet + bet;

        if (totalBet > CurrentBetAmount)
        {
            CurrentBetAmount = totalBet;
            EndPlayerPosition = GetPlayerIndex(currentPlayer);
        }

        CurrentRoundPotAmount += bet;
        currentPlayer.MakeBet(bet);
    }

    private void AddCurrentRoundToPot()
    {
        Pot originalPot = GamePot;
        Pot currentPot = originalPot;
        while (currentPot.SidePot != null)
        {
            currentPot = currentPot.SidePot;
        }

        List<GamePlayer> activePotPlayers = RefreshPotPlayers(currentPot);
        List<GamePlayer> sortedPotPlayers = [.. activePotPlayers.OrderBy(p => p.CurrentBet)];

        if (sortedPotPlayers.Count == 0 || sortedPotPlayers[0].Stack > 0)
        {
            // No all ins - just add current round pot to main pot
            currentPot.Amount += CurrentRoundPotAmount;
            CurrentRoundPotAmount = 0.0m;
            CurrentBetAmount = 0.0m;
        }
        else
        {
            HandleAllIns(currentPot, sortedPotPlayers);
        }

        PotJson = SerializePot(originalPot);
    }

    private List<GamePlayer> RefreshPotPlayers(Pot pot)
    {
        List<GamePlayer> activePotPlayers;
        if (pot.Players.Count == 0)
        {
            activePotPlayers = GetActivePlayers();
        }
        else
        {
            activePotPlayers = GetActivePlayersForPot(pot.Players);
        }

        pot.Players.Clear();
        foreach (var p in activePotPlayers)
        {
            int playerIndex = GetPlayerIndex(p);
            pot.Players.Add(playerIndex);
        }

        return activePotPlayers;
    }

    private void HandleAllIns(Pot currentPot, List<GamePlayer> sortedPotPlayers)
    {
        int idx = 0;
        decimal smallestBet = sortedPotPlayers[idx].CurrentBet;
        decimal betAccountedFor = 0.0m;

        while (idx < sortedPotPlayers.Count && sortedPotPlayers[idx].Stack == 0)
        {
            if (smallestBet == CurrentBetAmount)
            {
                // All in is same as current bet
                currentPot.Amount += CurrentRoundPotAmount;
                CurrentRoundPotAmount = 0.0m;
                CurrentBetAmount = 0.0m;

                // Find first player that isn't all in for creating side pot
                while (idx < sortedPotPlayers.Count && sortedPotPlayers[idx].Stack == 0)
                {
                    idx++;
                }

                if (idx < sortedPotPlayers.Count)
                {
                    CreateSidePot(currentPot, sortedPotPlayers.GetRange(idx, sortedPotPlayers.Count - idx));
                }

                return;
            }

            idx++;

            if (idx < sortedPotPlayers.Count && sortedPotPlayers[idx].Stack == 0)
            {
                // Two or more all ins, need to find where to create side pot
                decimal nextSmallestBet = sortedPotPlayers[idx].CurrentBet;
                if (nextSmallestBet > smallestBet)
                {
                    decimal amountAllCanWin = (smallestBet - betAccountedFor) * currentPot.Players.Count;
                    decimal sidePotAmount = CurrentRoundPotAmount - amountAllCanWin;
                    CurrentRoundPotAmount = sidePotAmount;
                    currentPot.Amount += amountAllCanWin;

                    CreateSidePot(currentPot, sortedPotPlayers.GetRange(idx, sortedPotPlayers.Count - idx));

                    betAccountedFor = smallestBet;
                    smallestBet = nextSmallestBet;
                    currentPot = currentPot.SidePot!;
                }
            }
            else if (idx < sortedPotPlayers.Count)
            {
                // All ins accounted for, add what all can win to pot and create side pot with remainder
                decimal amountAllCanWin = smallestBet * currentPot.Players.Count;
                decimal sidePotAmount = CurrentRoundPotAmount - amountAllCanWin;
                CurrentRoundPotAmount = 0.0m;
                CurrentBetAmount = 0.0m;
                currentPot.Amount += amountAllCanWin;
                CreateSidePot(currentPot, sortedPotPlayers.GetRange(idx, sortedPotPlayers.Count - idx));
                if (currentPot.SidePot != null)
                {
                    currentPot.SidePot.Amount = sidePotAmount;
                }
            }
        }
    }

    private void CreateSidePot(Pot pot, List<GamePlayer> potPlayers)
    {
        Pot nextSidePot = new(new List<int>());

        for (int i = 0; i < potPlayers.Count; i++)
        {
            GamePlayer sideBetPlayer = potPlayers[i];
            int playerIndex = GetPlayerIndex(sideBetPlayer);
            nextSidePot.Players.Add(playerIndex);
        }

        pot.SidePot = nextSidePot;
    }

    public void ResetPlayerBets()
    {
        for (int i = 0; i < PlayerIds.Count; i++)
        {
            GamePlayer? p = GetPlayerAtPosition(i);
            p!.CurrentBet = 0.0m;
        }
    }

    public GamePlayer? GetPlayerAtPosition(int position)
    {
        if (position < 0 || position >= PlayerIds.Count)
            return null;

        List<GamePlayer> players = [.. GameTable.Players];
        return players.FirstOrDefault(p => p.Id == PlayerIds[position]);
    }

    public int GetPlayerIndex(GamePlayer player)
    {
        return PlayerIds.IndexOf(player.Id);
    }

    public List<decimal> GetSidePotAmounts()
    {
        var amounts = new List<decimal>();
        Pot? currentPot = GamePot?.SidePot;

        while (currentPot != null)
        {
            if (currentPot.Amount > 0)
            {
                amounts.Add(currentPot.Amount);
            }
            currentPot = currentPot.SidePot;
        }

        return amounts;
    }

    public void Reset()
    {
        InitializeGame();
        CurrentRoundPotAmount = 0.0m;
        CurrentBetAmount = 0.0m;
        CurrentTurnPlayerPosition = -1;
        EndPlayerPosition = -1;
        BlindOption = true;
        BetsMade = false;
    }

    public void IncrementState()
    {
        switch (State)
        {
            case GameState.NOT_STARTED:
                State = GameState.START;
                break;
            case GameState.START:
                State = GameState.PREFLOP;
                break;
            case GameState.PREFLOP:
                State = GameState.FLOP;
                break;
            case GameState.FLOP:
                State = GameState.TURN;
                break;
            case GameState.TURN:
                State = GameState.RIVER;
                break;
            case GameState.RIVER:
                State = GameState.END;
                break;
        }
    }

    public Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> DetermineWinners()
    {
        Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> winners = new();
        Pot? pot = GamePot;

        while (pot != null && pot.Players.Count > 0 && pot.Amount > 0)
        {
            List<GamePlayer> activePotPlayers = GetActivePlayersForPot(pot.Players);
            Dictionary<GamePlayer, HandEvaluation> potWinners = DeterminePotWinners(activePotPlayers, CommunityCards);
            winners[pot] = potWinners;
            pot = pot.SidePot;
        }

        Winners = winners;
        return winners;
    }

    public Dictionary<GamePlayer, HandEvaluation> DeterminePotWinners(List<GamePlayer> activePlayers, List<Card> communityCards)
    {
        Dictionary<GamePlayer, HandEvaluation> bestHands = new();

        foreach (GamePlayer player in activePlayers)
        {
            var bestHand = EvaluateBestHand(player, communityCards);
            bestHands[player] = bestHand;
        }

        var highestRank = bestHands.Values.Max(h => h.Type);
        var playersWithBestHands = bestHands.Where(kvp => kvp.Value.Type == highestRank).ToList();

        if (playersWithBestHands.Count == 1)
        {
            return new Dictionary<GamePlayer, HandEvaluation> { { playersWithBestHands[0].Key, playersWithBestHands[0].Value } };
        }

        // Compare high card values for tie-breaking
        var winningHands = new Dictionary<GamePlayer, HandEvaluation>();
        var highestHighCards = playersWithBestHands.Select(kvp => kvp.Value.BestCards.Select(c => (int)c.Rank).ToList()).ToList();

        for (int i = 0; i < 5; i++)
        {
            // Get i-th element from each list into a list
            var currentHighCardValues = highestHighCards.Select(hc => hc.ElementAtOrDefault(i)).ToList();
            var maxHighCardValue = currentHighCardValues.Max();

            // Check how many players have the same highest high card
            var playersWithThisHighCard = playersWithBestHands
                .Where(kvp => kvp.Value.BestCards.Select(c => (int)c.Rank).ToList().ElementAtOrDefault(i) == maxHighCardValue)
                .Select(kvp => kvp.Key)
                .ToList();

            if (playersWithThisHighCard.Count == 1)
            {
                GamePlayer winningPlayer = playersWithThisHighCard[0];
                winningHands[winningPlayer] = bestHands[winningPlayer];
                return winningHands;
            }
            else if (playersWithThisHighCard.Count < playersWithBestHands.Count)
            {
                // Some players are eliminated; continue checking
                playersWithBestHands = playersWithBestHands
                    .Where(kvp => playersWithThisHighCard.Contains(kvp.Key))
                    .ToList();
            }
        }

        // If all high cards are the same, it's a tie
        foreach (var winner in playersWithBestHands)
        {
            winningHands[winner.Key] = winner.Value;
        }

        return winningHands;
    }

    public HandEvaluation EvaluateBestHand(GamePlayer player, List<Card> communityCards)
    {
        // Combine player's hand with community cards
        var allCards = new List<Card>(player.Hand);
        allCards.AddRange(communityCards);

        // Generate all combinations of 5 cards from the available cards
        IEnumerable<List<Card>> combinations = GetCombinations(allCards, 5);

        if (!combinations.Any())
            return new HandEvaluation { Type = HandType.HighCard, BestCards = allCards.Take(5).ToList() };

        var bestHand = EvaluateHand(combinations.First());

        foreach (var combo in combinations)
        {
            var handEvaluation = EvaluateHand(combo);
            if (handEvaluation.Type > bestHand.Type)
            {
                bestHand = handEvaluation;
            }
            else if (handEvaluation.Type == bestHand.Type)
            {
                bestHand = EvaluateTieBreaker(handEvaluation, bestHand);
            }
        }

        return bestHand;
    }

    private HandEvaluation EvaluateTieBreaker(HandEvaluation currentHand, HandEvaluation bestHand)
    {
        var currentBestHighCards = bestHand.BestCards.OrderByDescending(c => c.Rank).ToList();
        var newHighCards = currentHand.BestCards.OrderByDescending(c => c.Rank).ToList();

        // Compare the high cards to determine which hand is better
        for (int i = 0; i < Math.Min(currentBestHighCards.Count, newHighCards.Count); i++)
        {
            // If the new hand's high card is greater, replace the best hand
            if (newHighCards[i].Rank > currentBestHighCards[i].Rank)
            {
                return currentHand;
            }
            // If the high cards are equal, continue checking the next card
            else if (newHighCards[i].Rank < currentBestHighCards[i].Rank)
            {
                break;
            }
        }

        return bestHand;
    }

    // Generate combinations of a specified length from a list of cards
    private IEnumerable<List<Card>> GetCombinations(List<Card> cards, int length)
    {
        if (length == 0) yield return new List<Card>();
        if (cards.Count < length) yield break;

        for (int i = 0; i < cards.Count; i++)
        {
            var remainingCards = cards.Skip(i + 1).ToList();
            foreach (var combination in GetCombinations(remainingCards, length - 1))
            {
                yield return new List<Card> { cards[i] }.Concat(combination).ToList();
            }
        }
    }

    public HandEvaluation EvaluateHand(List<Card> hand)
    {
        // Sort the hand by rank
        var sortedHand = hand.OrderByDescending(c => c.Rank).ToList();
        bool isFlush = IsFlush(sortedHand);
        bool isStraight = IsStraight(sortedHand);

        // Check for hand types and return the best evaluation
        if (isFlush && isStraight && sortedHand.First().Rank == Rank.Ace)
        {
            return new HandEvaluation { Type = HandType.RoyalFlush, BestCards = sortedHand };
        }
        if (isFlush && isStraight)
        {
            return new HandEvaluation { Type = HandType.StraightFlush, BestCards = sortedHand };
        }

        var rankGroups = sortedHand.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();

        if (rankGroups[0].Count() == 4)
        {
            return new HandEvaluation { Type = HandType.FourOfAKind, BestCards = rankGroups.SelectMany(g => g).ToList() };
        }
        if (rankGroups[0].Count() == 3 && rankGroups.Count > 1 && rankGroups[1].Count() == 2)
        {
            return new HandEvaluation { Type = HandType.FullHouse, BestCards = rankGroups.SelectMany(g => g).ToList() };
        }
        if (isFlush)
        {
            return new HandEvaluation { Type = HandType.Flush, BestCards = sortedHand };
        }
        if (isStraight)
        {
            if (sortedHand[0].Rank == Rank.Ace && sortedHand[1].Rank == Rank.Five)
            {
                Card ace = sortedHand[0];
                sortedHand = sortedHand.GetRange(1, 4);
                sortedHand.Add(ace);
            }
            return new HandEvaluation { Type = HandType.Straight, BestCards = sortedHand };
        }
        if (rankGroups[0].Count() == 3)
        {
            return new HandEvaluation { Type = HandType.ThreeOfAKind, BestCards = rankGroups.SelectMany(g => g).ToList() };
        }
        if (rankGroups.Count > 1 && rankGroups[0].Count() == 2 && rankGroups[1].Count() == 2)
        {
            return new HandEvaluation { Type = HandType.TwoPair, BestCards = rankGroups.SelectMany(g => g).ToList() };
        }
        if (rankGroups[0].Count() == 2)
        {
            return new HandEvaluation { Type = HandType.OnePair, BestCards = rankGroups.SelectMany(g => g).ToList() };
        }

        return new HandEvaluation { Type = HandType.HighCard, BestCards = sortedHand };
    }

    private bool IsFlush(List<Card> hand)
    {
        return hand.All(c => c.Suit == hand[0].Suit);
    }

    private bool IsStraight(List<Card> sortedHand)
    {
        // Special case for A-5-4-3-2
        if (sortedHand[0].Rank == Rank.Ace && sortedHand[1].Rank == Rank.Five)
        {
            for (int i = 1; i < sortedHand.Count - 1; i++)
            {
                if ((int)sortedHand[i].Rank != (int)sortedHand[i + 1].Rank + 1)
                {
                    return false;
                }
            }
            return true;
        }

        // Normal straight check
        for (int i = 0; i < sortedHand.Count - 1; i++)
        {
            if ((int)sortedHand[i].Rank != (int)sortedHand[i + 1].Rank + 1)
            {
                return false;
            }
        }
        return true;
    }
}