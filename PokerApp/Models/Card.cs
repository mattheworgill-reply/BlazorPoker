using System.Text.Json.Serialization;
using PokerApp.Models.Enums;

namespace PokerApp.Models;

public class Card
{
    public Suit Suit { get; set; }
    public Rank Rank { get; set; }

    [JsonConstructor]
    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public override string ToString() => $"{Rank} of {Suit}";

    public string GetSuitLowercase() => $"{Suit}".ToLower();
    
    public string GetRankString() => $"{Rank}";
    
    // New methods for display
    public string GetCssClass() => $"card {GetSuitLowercase()}";
    
    public string GetDisplaySymbol()
    {
        return Suit switch
        {
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            Suit.Spades => "♠",
            _ => "?"
        };
    }
    
    public string GetColor() => Suit == Suit.Hearts || Suit == Suit.Diamonds ? "red" : "black";
}