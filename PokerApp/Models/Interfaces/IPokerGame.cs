using PokerApp.Models.Enums;

namespace PokerApp.Models.Interfaces;

public interface IPokerGame
{
    GameState State { get; set; }
    int CurrentTurnPlayerPosition { get; set; }
    bool BlindOption { get; set; }
    bool BetsMade { get; set; }

    void Reset();
    void IncrementState();
    Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> DetermineWinners(PokerTable table);
}