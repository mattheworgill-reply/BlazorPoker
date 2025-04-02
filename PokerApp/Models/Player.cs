// using System.Text.Json.Serialization;
// using PokerApp.Models.Enums;

// namespace PokerApp.Models;

// public class Player
// {
//     public string Id { get; set; } = string.Empty;
//     public string Name { get; set; }
//     public List<Card> Hand { get; set; } = new();
//     public PokerPosition PokerPosition { get; set; } = PokerPosition.Other;
//     public bool IsDealer { get; set; }
//     public int TablePosition { get; set; }
//     public decimal Stack { get; set; }
//     public decimal CurrentBet { get; set; } = 0.0m;
//     public bool IsInGame { get; set; } = true;
//     public bool HasFolded { get; set; } = false;

//     [JsonConstructor]
//     public Player(string name, decimal stack = 100.0m)
//     {
//         Name = name;
//         Stack = stack;
//     }

//     public Player()
//     {
//         Name = string.Empty;
//         Stack = 100.0m;
//     }

//     public void ReceiveCard(Card card)
//     {
//         Hand.Add(card);
//         Hand = Hand.OrderByDescending(c => c.Rank).ToList();
//     }

//     public void ClearHand()
//     {
//         Hand.Clear();
//     }

//     public void MakeBet(decimal bet)
//     {
//         CurrentBet += bet;
//         Stack -= bet;
//     }

//     public bool IsActive(bool onlyBettors = false)
//     {
//         return IsInGame && !HasFolded && !(onlyBettors && Stack <= 0.0m);
//     }
// }