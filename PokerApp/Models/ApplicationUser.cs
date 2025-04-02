// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace PokerApp.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 1000.0m;
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<PokerTable> OwnedTables { get; set; } = new List<PokerTable>();
    public virtual ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();
    public virtual UserStats Stats { get; set; } = null!;
}