using PokerApp.Models.Enums;
namespace PokerApp.Models.Events;

public class PlayerActionEventArgs
{
    public PlayerAction Action { get; set; }
    public decimal? Amount { get; set; }
}