// using PokerApp.Models.Enums;
// using PokerApp.Models.Interfaces;

// namespace PokerApp.Models;

// public class Table : ITable
// {
//     public PokerGame PokerGame;
//     public List<Player> Players { get; private set; } = new();
//     public List<int> TablePositions { get; private set; } = new();
//     public Deck Deck { get; set; }
//     public List<Card> CommunityCards { get; set; } = new();
//     public decimal BigBlind { get; set; } = 2.0m;
//     public Pot Pot { get; set; }
//     public decimal CurrentRoundPot { get; set; } = 0.0m;
//     public decimal CurrentBet { get; set; } = 0.0m;
//     public int DealerButtonPosition { get; set; } = 0;
//     public int EndPlayer { get; set; } = 0;

//     public Table()
//     {
//         PokerGame = new PokerGame(DealerButtonPosition);
//         TablePositions = Enumerable.Repeat(-1, 10).ToList();
//         Deck = new Deck();
//         Pot = new Pot(new List<int>());
//     }

//     public IPokerGame GetGame()
//     {
//         return PokerGame;
//     }

//     public void SetGame(PokerGame game)
//     {
//         PokerGame = game;
//     }
    
//     public GameSnapshot GetGameSnapshot()
//     {
//         return new GameSnapshot(this);
//     }

//     public void InitializeTable()
//     {
//         DealerButtonPosition = 0;
//         PokerGame = new PokerGame(DealerButtonPosition);
//         Players.Clear();
//         TablePositions = Enumerable.Repeat(-1, 10).ToList();
//         Deck = new Deck();
//         CommunityCards.Clear();
//         BigBlind = 2.0m;
//         Pot = new Pot(new List<int>());
//         CurrentRoundPot = 0.0m;
//         CurrentBet = 0.0m;
//         EndPlayer = 0;
//     }

//     public void CreateTable(string playerNames)
//     {
//         if (Deck.Cards.Count > 0)
//         {
//             InitializeTable();
//         }

//         var separatedNames = playerNames.Split(',');
//         int index = 1;
        
//         foreach (var name in separatedNames)
//         {
//             var player = new Player(name.Trim());
//             AddPlayer(player, index++);
//         }

//         DealerButtonPosition = 0;
//         Deck.InitializeDeck();
//     }

//     public void StartGame(bool isFirstGame)
//     {
//         if (isFirstGame)
//         {
//             PokerGame.State = GameState.START;
//             SetBlinds();
//         }
//         else
//         {
//             Reset();
//             MoveDealerButton();
//         }
//         Console.WriteLine("start game, setting turn");
//         PokerGame.Turn = FindNextActivePlayerIndex(DealerButtonPosition);
//         Console.WriteLine("turn: " + PokerGame.Turn);
//         foreach (Player p in Players)
//         {
//             Console.WriteLine("in start game.. " + p.Name);
//         }
//     }

//     public void MoveDealerButton()
//     {
//         Players[DealerButtonPosition].IsDealer = false;
//         Players[DealerButtonPosition].PokerPosition = PokerPosition.Other;
//         DealerButtonPosition = FindNextActivePlayerIndex(DealerButtonPosition, preHand: true);
//         SetBlinds();
//     }

//     public void AddPlayer(Player player, int position)
//     {
//         player.TablePosition = position;
//         Players.Add(player);
//         Players = Players.OrderBy(p => p.TablePosition).ToList();
//         UpdateTablePositions();
//         player.PokerPosition = PokerPosition.Other;
//         Console.WriteLine("Player added");
//         foreach (Player p in Players)
//         {
//             Console.WriteLine(p.Name);
//         }
//         for (int i = 0; i < TablePositions.Count; i++)
//         {
//             int idx = TablePositions[i];
//             if (idx >= 0)
//             {
//                 Console.WriteLine("at i: " + i + " idx points to: " + Players[idx].Name);
//             }
//         }
//     }

//     private void UpdateTablePositions()
//     {
//         TablePositions = Enumerable.Repeat(-1, 10).ToList();
//         for (int i = 0; i < Players.Count; i++)
//         {
//             Player p = Players[i];
//             if (p.TablePosition >= 1 && p.TablePosition <= 10)
//             {
//                 TablePositions[p.TablePosition - 1] = i;
//             }
//         }
//     }

//     public void SetBlinds()
//     {
//         Player dealer = Players[DealerButtonPosition];
//         int smallBlindIdx = FindNextActivePlayerIndex(DealerButtonPosition, preHand: true);
//         Player smallBlind = Players[smallBlindIdx];
//         Player bigBlind = dealer;

//         if (Players.Count > 2)
//         {
//             int bigBlindIdx = FindNextActivePlayerIndex(smallBlindIdx, preHand: true);
//             bigBlind = Players[bigBlindIdx];
//         }
        
//         dealer.IsDealer = true;
//         dealer.PokerPosition = PokerPosition.Other;
//         smallBlind.PokerPosition = PokerPosition.SmallBlind;
//         bigBlind.PokerPosition = PokerPosition.BigBlind;
//         EndPlayer = Players.IndexOf(smallBlind);
//     }

//     public void BetBlinds()
//     {
//         Player smallBlind = Players[PokerGame.Turn];
//         PlayerBet(smallBlind, BigBlind / 2);
//         int bigBlindIdx = FindNextActivePlayerIndex(PokerGame.Turn);
//         Player bigBlind = Players[bigBlindIdx];
//         PlayerBet(bigBlind, BigBlind);
//         PokerGame.Turn = FindNextActivePlayerIndex(bigBlindIdx);
//     }

//     public List<Player> GetActivePlayers(bool onlyBettors = false, bool preHand = false)
//     {
//         List<Player> activePlayers = new();
//         foreach (Player p in Players)
//         {
//             if (p.IsActive(onlyBettors) && (p.Hand.Count == 2 || preHand))
//             {
//                 activePlayers.Add(p);
//             }
//         }
//         return activePlayers;
//     }

//     public List<Player> GetBettingPlayers()
//     {
//         return GetActivePlayers(onlyBettors: true);
//     }

//     public List<Player> GetPreHandPlayers()
//     {
//         return GetActivePlayers(onlyBettors: true, preHand: true);
//     }

//     public List<Player> GetActivePlayersForPot(List<int> playerIndices)
//     {
//         List<Player> potPlayers = new();
//         foreach (int i in playerIndices)
//         {
//             if (i >= 0 && i < Players.Count)
//             {
//                 Player p = Players[i];
//                 if (p.IsActive())
//                 {
//                     potPlayers.Add(p);
//                 }
//             }
//         }
//         return potPlayers;
//     }

//     public void SetNextTurn()
//     {
//         if (Players.Count(p => !p.HasFolded) <= 1)
//         {
//             EndRound();
//             return;
//         }
        
//         int nextTurn = FindNextActivePlayerIndex(PokerGame.Turn);

//         if (EndPlayer == nextTurn && !(PokerGame.BlindOption && Players[nextTurn].PokerPosition == PokerPosition.BigBlind))
//         {
//             EndRound();
//         }
//         else
//         {
//             PokerGame.Turn = nextTurn;
//         }
//     }

//     public void EndRound()
//     {
//         PokerGame.BetsMade = true;
//         PokerGame.Turn = FindNextActivePlayerIndex(DealerButtonPosition);
//         EndPlayer = PokerGame.Turn;
//         AddCurrentRoundToPot();
//         ResetPlayerBets();
//     }

//     public int FindNextActivePlayerIndex(int currentIndex, bool preHand = false)
//     {
//         // Safety check - if no players or only one player
//         if (Players.Count == 0)
//         {
//             return 0;
//         }
        
//         if (Players.Count == 1)
//         {
//             return 0;
//         }
        
//         int nextIndex = Players.Count == currentIndex + 1 ? 0 : currentIndex + 1;
        
//         // Ensure nextIndex is valid
//         if (nextIndex < 0 || nextIndex >= Players.Count)
//         {
//             nextIndex = 0;
//         }
        
//         Player nextPlayer = Players[nextIndex];
        
//         if (EndPlayer == nextIndex) {
//             return nextIndex;
//         }

//         // Avoid infinite loop
//         int safeguardCount = 0;
//         while (nextIndex != currentIndex && 
//             ((!preHand && nextPlayer.Hand.Count < 2) || !nextPlayer.IsActive(onlyBettors: true)) && 
//             safeguardCount < Players.Count)
//         {
//             nextIndex = Players.Count == nextIndex + 1 ? 0 : nextIndex + 1;
            
//             // Ensure nextIndex is valid
//             if (nextIndex < 0 || nextIndex >= Players.Count)
//             {
//                 nextIndex = 0;
//             }
            
//             nextPlayer = Players[nextIndex];
//             safeguardCount++;
//         }

//         return nextIndex;
//     }

//     public void DealHands()
//     {
//         for (int i = 0; i < 2; i++)
//         {
//             foreach (var player in GetPreHandPlayers())
//             {
//                 player.ReceiveCard(Deck.Deal());
//             }
//         }
//     }

//     public void DealFlop()
//     {
//         // Burn a card
//         Deck.Deal();
        
//         // Deal the flop - 3 cards
//         CommunityCards.Add(Deck.Deal());
//         CommunityCards.Add(Deck.Deal());
//         CommunityCards.Add(Deck.Deal());
//     }

//     public void DealTurn()
//     {
//         // Burn a card
//         Deck.Deal();
        
//         // Deal the turn - 1 card
//         CommunityCards.Add(Deck.Deal());
//     }

//     public void DealRiver()
//     {
//         // Burn a card
//         Deck.Deal();
        
//         // Deal the river - 1 card
//         CommunityCards.Add(Deck.Deal());
//     }

//     public void PlayerBet(Player currentPlayer, decimal bet)
//     {
//         decimal totalBet = currentPlayer.CurrentBet + bet;

//         if (totalBet > CurrentBet)
//         {
//             CurrentBet = totalBet;
//             EndPlayer = Players.IndexOf(currentPlayer);
//         }
        
//         CurrentRoundPot += bet;
//         currentPlayer.MakeBet(bet);
//     }

//     private void AddCurrentRoundToPot()
//     {
//         Pot currentPot = Pot;
//         while (currentPot.SidePot != null)
//         {
//             currentPot = currentPot.SidePot;
//         }

//         // Refresh pot players (remove folded players)
//         List<Player> activePotPlayers = RefreshPotPlayers(currentPot);

//         List<Player> sortedPotPlayers = activePotPlayers.OrderBy(p => p.CurrentBet).ToList();

//         if (sortedPotPlayers.Count == 0 || sortedPotPlayers[0].Stack > 0)
//         {
//             // No all ins - just add current round pot to main pot
//             currentPot.Amount += CurrentRoundPot;
//             CurrentRoundPot = 0.0m;
//             CurrentBet = 0.0m;
//         }
//         else
//         {
//             // Handle all in situations and side pots
//             HandleAllIns(currentPot, sortedPotPlayers);
//         }
//     }

//     private List<Player> RefreshPotPlayers(Pot pot)
//     {
//         List<Player> activePotPlayers;
//         if (pot.Players.Count == 0)
//         {
//             activePotPlayers = GetActivePlayers();
//         }
//         else
//         {
//             activePotPlayers = GetActivePlayersForPot(pot.Players);
//         }

//         pot.Players.Clear();
//         foreach (Player p in activePotPlayers)
//         {
//             int playerIndex = Players.IndexOf(p);
//             pot.Players.Add(playerIndex);
//         }

//         return activePotPlayers;
//     }

//    private void HandleAllIns(Pot currentPot, List<Player> sortedPotPlayers)
//     {
//         int idx = 0;
//         decimal smallestBet = sortedPotPlayers[idx].CurrentBet;
//         decimal betAccountedFor = 0.0m;

//         while (idx < sortedPotPlayers.Count && sortedPotPlayers[idx].Stack == 0)
//         {
//             if (smallestBet == CurrentBet)
//             {
//                 // All in is same as current bet
//                 currentPot.Amount += CurrentRoundPot;
//                 CurrentRoundPot = 0.0m;
//                 CurrentBet = 0.0m;

//                 // Find first player that isn't all in for creating side pot
//                 while (idx < sortedPotPlayers.Count && sortedPotPlayers[idx].Stack == 0)
//                 {
//                     idx++;
//                 }

//                 if (idx < sortedPotPlayers.Count)
//                 {
//                     CreateSidePot(currentPot, sortedPotPlayers.GetRange(idx, sortedPotPlayers.Count - idx));
//                 }
//                 return;
//             }
            
//             idx++;

//             if (idx < sortedPotPlayers.Count && sortedPotPlayers[idx].Stack == 0)
//             {
//                 // Two or more all ins, need to find where to create side pot
//                 decimal nextSmallestBet = sortedPotPlayers[idx].CurrentBet;
//                 if (nextSmallestBet > smallestBet)
//                 {
//                     decimal amountAllCanWin = (smallestBet - betAccountedFor) * currentPot.Players.Count;
//                     decimal sidePotAmount = CurrentRoundPot - amountAllCanWin;
//                     CurrentRoundPot = sidePotAmount;
//                     currentPot.Amount += amountAllCanWin;

//                     CreateSidePot(currentPot, sortedPotPlayers.GetRange(idx, sortedPotPlayers.Count - idx));

//                     betAccountedFor = smallestBet;
//                     smallestBet = nextSmallestBet;
//                     currentPot = currentPot.SidePot!;
//                 }
//             }
//             else if (idx < sortedPotPlayers.Count)
//             {
//                 // All ins accounted for, add what all can win to pot and create side pot with remainder
//                 decimal amountAllCanWin = smallestBet * currentPot.Players.Count;
//                 decimal sidePotAmount = CurrentRoundPot - amountAllCanWin;
//                 CurrentRoundPot = 0.0m;
//                 CurrentBet = 0.0m;
//                 currentPot.Amount += amountAllCanWin;
//                 CreateSidePot(currentPot, sortedPotPlayers.GetRange(idx, sortedPotPlayers.Count - idx));
//                 if (currentPot.SidePot != null)
//                 {
//                     currentPot.SidePot.Amount = sidePotAmount;
//                 }
//             }
//         }
//     }

//     private void CreateSidePot(Pot pot, List<Player> potPlayers)
//     {
//         Pot nextSidePot = new(new List<int>());

//         for (int i = 0; i < potPlayers.Count; i++)
//         {
//             Player sideBetPlayer = potPlayers[i];
//             int playerIndex = Players.IndexOf(sideBetPlayer);
//             nextSidePot.Players.Add(playerIndex);
//         }

//         pot.SidePot = nextSidePot;
//     }

//     public void ResetPlayerBets()
//     {
//         foreach (Player p in Players)
//         {
//             p.CurrentBet = 0.0m;
//         }
//     }

//     public void Reset()
//     {
//         PokerGame.Reset();
//         Deck.ShuffleDeck();
//         CommunityCards.Clear();
//         Pot.Reset();
//         CurrentRoundPot = 0.0m;
//         CurrentBet = 0.0m;
//         ResetPlayers();
//     }

//     public void ResetPlayers()
//     {
//         foreach (Player p in Players)
//         {
//             p.ClearHand();
//             p.HasFolded = false;
//             if (p.Stack <= 0)
//             {
//                 p.IsInGame = false;
//             }
//         }
//     }
// }