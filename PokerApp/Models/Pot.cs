using System.Text.Json.Serialization;

namespace PokerApp.Models;

public class Pot
{
    public decimal Amount { get; set; }
    public List<int> Players { get; set; }
    public Pot? SidePot { get; set; }
    
    [JsonConstructor]
    public Pot(List<int> players, decimal amount = 0.0m)
    {
        Amount = amount;
        Players = players;
    }

    public void StartSidePot(List<int> sidePlayers, decimal amount = 0.0m)
    {
        SidePot = new Pot(sidePlayers, amount);
    }

    public void Reset()
    {
        Players.Clear();
        Amount = 0.0m;
        SidePot = null;
    }
}