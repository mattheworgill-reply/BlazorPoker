using PokerApp.Models.Enums;
using PokerApp.Models.Interfaces;

namespace PokerApp.Models;

public class GameSnapshot
{
    public string TableId { get; set; } = string.Empty;
    public List<GamePlayer> Players { get; set; } = new();
    public List<int> TablePositions { get; set; } = new();
    public decimal BigBlind { get; set; }
    public GameState GameState { get; set; } = GameState.NOT_STARTED;
    public int TimerSeconds { get; set; }
    public int TimeRemaining { get; set; }
    public bool IsGamePaused { get; set; }
    public Pot Pot { get; set; } = new(new List<int>());
    public List<decimal> SidePotAmounts { get; set; } = new();
    public List<Card> CommunityCards { get; set; } = new();
    public bool BetsMade { get; set; }
    public GamePlayer? TurnPlayer { get; set; }
    public decimal CurrentBet { get; set; }
    public Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> AllWinners { get; set; } = new();

    public GameSnapshot(PokerGame? game)
    {
        if (game != null)
        {
            GameState = game.State;
            CommunityCards = game.CommunityCards;
            Pot = game.GamePot;
            SidePotAmounts = game.GetSidePotAmounts();
            CurrentBet = game.CurrentBetAmount;
            BetsMade = game.BetsMade;
            TurnPlayer = game.GetPlayerAtPosition(game.CurrentTurnPlayerPosition);
            AllWinners = game.Winners;
        }
    }

    public void SetSidePots()
    {
        Pot? currentPot = Pot;
        while (currentPot?.SidePot != null)
        {
            if (currentPot.SidePot.Amount > 0)
            {
                SidePotAmounts.Add(currentPot.SidePot.Amount);
            }

            currentPot = currentPot.SidePot;
        }
    }
}