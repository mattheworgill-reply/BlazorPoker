using PokerApp.Models.Enums;

namespace PokerApp.Models;

public class HandEvaluation
{
    public HandType Type { get; set; }
    public List<Card> BestCards { get; set; } = new();
}