// Models/UserStats.cs
namespace PokerApp.Models;

public class UserStats
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    // public int HandsPlayed { get; set; }
    public int GamesWon { get; set; }
    public int GamesPlayed { get; set; }
    public decimal BiggestPot { get; set; }
    public decimal TotalWinnings { get; set; }
    public decimal TotalLosses { get; set; }
    public string BestHand { get; set; } = string.Empty;
    public DateTime LastPlayed { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual ApplicationUser User { get; set; } = null!;
}