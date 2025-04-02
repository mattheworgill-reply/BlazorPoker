// using PokerApp.Models.Enums;

// namespace PokerApp.Models.Interfaces;

// public interface ITable
// {
//     List<Player> Players { get; }
//     List<int> TablePositions { get; }
//     Deck Deck { get; }
//     List<Card> CommunityCards { get; }
//     decimal BigBlind { get; set; }
//     Pot Pot { get; }
//     decimal CurrentRoundPot { get; set; }
//     decimal CurrentBet { get; set; }
//     int DealerButtonPosition { get; set; }
    
//     IPokerGame GetGame();
//     void SetGame(PokerGame game);
//     GameSnapshot GetGameSnapshot();
//     void CreateTable(string playerNames);
//     void StartGame(bool isFirstGame);
//     void AddPlayer(Player player, int position);
//     List<Player> GetActivePlayers(bool onlyBettors = false, bool preHand = false);
//     List<Player> GetActivePlayersForPot(List<int> playerIndices);
// }