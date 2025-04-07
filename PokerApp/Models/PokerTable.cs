// Models/PokerTable.cs

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using PokerApp.Models.Enums;

namespace PokerApp.Models;

public class PokerTable
{
    // Database fields
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public decimal BigBlind { get; set; } = 2.0m;
    public decimal SmallBlind { get; set; } = 1.0m;
    public int TimerSeconds { get; set; } = 30;
    public int TimeRemaining { get; set; } = 30;
    public bool IsGamePaused { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string TableCode { get; set; } = string.Empty;

    // Game State fields
    public List<string> playerLeaves { get; set; } = [];
    public string? TablePositionsJson { get; set; }

    // Navigation properties
    public virtual ApplicationUser Owner { get; set; } = null!;
    public virtual PokerGame Game { get; set; } = null!;
    public virtual ICollection<GamePlayer> Players { get; set; } = new List<GamePlayer>();
    public virtual ICollection<GameHistory> GameHistories { get; set; } = new List<GameHistory>();

    [NotMapped]
    public List<int> TablePositions
    {
        get
        {
            if (!string.IsNullOrEmpty(TablePositionsJson))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<int>>(TablePositionsJson) ?? CalculateTablePositions();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error deserializing table positions: {ex.Message}");
                    return CalculateTablePositions();
                }
            }
            return CalculateTablePositions();
        }
        set => TablePositionsJson = JsonSerializer.Serialize(value);
    }

    public void InitializeTable()
    {
        TablePositions = Enumerable.Repeat(-1, 10).ToList();
        BigBlind = 2.0m;
        SmallBlind = 1.0m;
        playerLeaves = [];
    }

    public void MoveDealerButton()
    {
        Game.MoveDealerButton();
    }

    public void UpdateTablePositions()
    {
        var positions = Enumerable.Repeat(-1, 10).ToList();
        var playersList = Players.ToList();

        for (int i = 0; i < playersList.Count; i++)
        {
            var p = playersList[i];
            if (p.Position >= 1 && p.Position <= 10)
            {
                positions[p.Position - 1] = i;
            }
        }

        TablePositions = positions;
    }

    private List<int> CalculateTablePositions()
    {
        var positions = Enumerable.Repeat(-1, 10).ToList();
        var playersList = Players.ToList();

        for (int i = 0; i < playersList.Count; i++)
        {
            var p = playersList[i];
            if (p.Position >= 1 && p.Position <= 10)
            {
                positions[p.Position - 1] = i;
            }
        }

        return positions;
    }

    public GameSnapshot GetGameSnapshot()
    {
        var snapshot = new GameSnapshot(Game)
        {
            TableId = Id,
            Players = [.. Players],
            TablePositions = TablePositions,
            BigBlind = BigBlind,
            TimerSeconds = TimerSeconds,
            TimeRemaining = TimeRemaining,
            IsGamePaused = IsGamePaused
        };

        return snapshot;
    }
}