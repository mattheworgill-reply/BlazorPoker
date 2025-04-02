using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using PokerApp.Models.Enums;

namespace PokerApp.Models;

public class GamePlayer
{
    // Existing database properties
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PokerTableId { get; set; } = string.Empty;
    public int Position { get; set; }
    public decimal Stack { get; set; } = 100.0m;
    // public bool IsActive { get; set; } = true;
    public bool IsSeated { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    // New game state properties
    public decimal CurrentBet { get; set; } = 0.0m;
    public bool HasFolded { get; set; } = false;
    public bool IsInGame { get; set; } = true;
    public PokerPosition PokerPosition { get; set; } = PokerPosition.Other;
    public bool IsDealer { get; set; } = false;
    public string? HandCardsJson { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual PokerTable PokerTable { get; set; } = null!;
    
    // Game properties (not mapped to database)
    [NotMapped]
    public List<Card> Hand
    {
        get => DeserializeJson<List<Card>>(HandCardsJson) ?? new List<Card>();
        set => HandCardsJson = SerializeJson(value);
    }
    
    // Player methods from Player.cs
    public void ReceiveCard(Card card)
    {
        var currentHand = Hand;
        currentHand.Add(card);
        Hand = currentHand.OrderByDescending(c => c.Rank).ToList();
    }
    
    public void ClearHand()
    {
        Hand = new List<Card>();
    }
    
    public void MakeBet(decimal bet)
    {
        CurrentBet += bet;
        Stack -= bet;
    }
    
    public bool IsActive(bool onlyBettors = false)
    {
        return IsInGame && !HasFolded && !(onlyBettors && Stack <= 0.0m);
    }
    
    public override string ToString()
    {
        return $"{User?.DisplayName ?? "Unknown"} (Position {Position}, Stack ${Stack})";
    }
    
    // Helper methods for serialization
    private T? DeserializeJson<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return default;
            
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    
    private string SerializeJson<T>(T obj)
    {
        return JsonSerializer.Serialize(obj);
    }
}