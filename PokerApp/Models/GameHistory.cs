// Models/GameHistory.cs
namespace PokerApp.Models;

public class GameHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PokerTableId { get; set; } = string.Empty;
    public string? WinnerId { get; set; }
    public decimal PotAmount { get; set; }
    public string HandType { get; set; } = string.Empty;
    public string GameDetails { get; set; } = string.Empty; // JSON of game details
    public DateTime EndedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual PokerTable PokerTable { get; set; } = null!;
    public virtual ApplicationUser? Winner { get; set; }
    public virtual ICollection<GameHistoryPlayer> Players { get; set; } = new List<GameHistoryPlayer>();
}

// Additional class to track players in each game history
public class GameHistoryPlayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GameHistoryId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Position { get; set; }
    public decimal StartingStack { get; set; }
    public decimal EndingStack { get; set; }
    public string Cards { get; set; } = string.Empty; // JSON representation of cards
    public bool WonHand { get; set; }
    
    // Navigation properties
    public virtual GameHistory GameHistory { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}