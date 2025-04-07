// Models/GameHistoryEvent.cs
using System;
using PokerApp.Models.Enums;

namespace PokerApp.Models;

public enum GameEventType
{
    GameStart,
    PlayerAction,
    CardDealt,
    CommunityCards,
    PlayerCards,
    GameEnd,
    Winner,
    Error
}

public class GameHistoryEvent
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public GameEventType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int GameNumber { get; set; } = 1;

    // Factory methods for creating different types of events
    public static GameHistoryEvent GameStart(int gameNumber, int playerCount)
    {
        return new GameHistoryEvent
        {
            Type = GameEventType.GameStart,
            Message = $"<strong>Game {gameNumber}</strong> started with {playerCount} players",
            GameNumber = gameNumber
        };
    }

    public static GameHistoryEvent PlayerCards(string playerName, List<Card> cards)
    {
        string cardHtml = string.Join(" ", cards.Select(FormatCard));

        return new GameHistoryEvent
        {
            Type = GameEventType.PlayerCards,
            Message = $"You received {cardHtml}"
        };
    }

    public static GameHistoryEvent PlayerAction(string playerName, PlayerAction action, decimal? amount = null)
    {
        string actionText = action switch
        {
            PokerApp.Models.Enums.PlayerAction.CHECK => "checks",
            PokerApp.Models.Enums.PlayerAction.CALL => "calls",
            PokerApp.Models.Enums.PlayerAction.FOLD => "folds",
            PokerApp.Models.Enums.PlayerAction.BET => amount > 0 ? $"bets <span class=\"bet-amount\">${amount}</span>" : "bets",
            _ => action.ToString().ToLower()
        };

        return new GameHistoryEvent
        {
            Type = GameEventType.PlayerAction,
            Message = $"<span class=\"player-name\">{playerName}</span> {actionText}"
        };
    }

    public static GameHistoryEvent CommunityCards(GameState state, List<Card> communityCards)
    {
        string stateText = state switch
        {
            GameState.FLOP => "Flop",
            GameState.TURN => "Turn",
            GameState.RIVER => "River",
            _ => state.ToString()
        };

        string cardHtml = string.Join(" ", communityCards.Select(FormatCard));

        return new GameHistoryEvent
        {
            Type = GameEventType.CommunityCards,
            Message = $"<strong>{stateText}:</strong> {cardHtml}"
        };
    }

    public static GameHistoryEvent Winner(string playerName, HandType handType, decimal amount, List<Card> cards)
    {
        string cardHtml = string.Join(" ", cards.Select(FormatCard));

        return new GameHistoryEvent
        {
            Type = GameEventType.Winner,
            Message = $"<span class=\"player-name\">{playerName}</span> wins <span class=\"bet-amount\">${amount}</span> with <span class=\"hand-type\">{handType}</span> {cardHtml}"
        };
    }

    public static GameHistoryEvent GameEnd(int gameNumber)
    {
        return new GameHistoryEvent
        {
            Type = GameEventType.GameEnd,
            Message = $"<strong>Game {gameNumber}</strong> ended",
            GameNumber = gameNumber
        };
    }

    public static GameHistoryEvent Error(string message)
    {
        return new GameHistoryEvent
        {
            Type = GameEventType.Error,
            Message = $"Error: {message}"
        };
    }

    // Helper method to format a card as HTML
    private static string FormatCard(Card card)
    {
        string suitSymbol = card.Suit switch
        {
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            Suit.Spades => "♠",
            _ => "?"
        };

        string rankSymbol = card.Rank switch
        {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            Rank.Ten => "10",
            Rank.Nine => "9",
            Rank.Eight => "8",
            Rank.Seven => "7",
            Rank.Six => "6",
            Rank.Five => "5",
            Rank.Four => "4",
            Rank.Three => "3",
            Rank.Two => "2",
            _ => "?"
        };

        string colorClass = (card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds) ? "red" : "black";

        return $"<span class=\"card-icon {colorClass}\">{rankSymbol}{suitSymbol}</span>";
    }
}