
using PokerApp.Models.Enums;

namespace PokerApp.Models
{
    public class SerializableGameData
    {
        public static SerializableCard GetSerializableCard(Card card)
        {
            return new SerializableCard
            {
                Suit = (int)card.Suit,
                Rank = (int)card.Rank
            };
        }

        public static GamePlayer GetDeserializableGamePlayer(SerializablePlayer serPlayer)
        {
            return new GamePlayer
            {
                Id = serPlayer.Id,
                UserId = serPlayer.UserId,
                Name = serPlayer.Name,
                Position = serPlayer.Position,
                Stack = serPlayer.Stack,
                CurrentBet = serPlayer.CurrentBet,
                HasFolded = serPlayer.HasFolded,
                IsInGame = serPlayer.IsInGame,
                IsSeated = serPlayer.IsSeated,
                PokerPosition = (PokerPosition)serPlayer.PokerPosition,
                IsDealer = serPlayer.IsDealer,
                Hand = [.. serPlayer.Hand.Select(GetDeserializableCard)]
            };
        }

        public static SerializablePlayer GetSerializableGamePlayer(GamePlayer player)
        {
            var serPlayer = new SerializablePlayer
            {
                Id = player.Id,
                UserId = player.UserId,
                Name = player.Name,
                Position = player.Position,
                Stack = player.Stack,
                CurrentBet = player.CurrentBet,
                HasFolded = player.HasFolded,
                IsInGame = player.IsInGame,
                IsSeated = player.IsSeated,
                PokerPosition = (int)player.PokerPosition,
                IsDealer = player.IsDealer
            };

            foreach (var card in player.Hand)
            {
                serPlayer.Hand.Add(GetSerializableCard(card));
            }

            return serPlayer;
        }

        public static Card GetDeserializableCard(SerializableCard serCard)
        {
            return new Card((Suit)serCard.Suit, (Rank)serCard.Rank);
        }

        public static List<SerializablePotWinners> GetSerializablePotWinners(Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> potWinners)
        {
            List<SerializablePotWinners> serializablePotWinners = [];

            foreach (var potAndWinners in potWinners)
            {
                Pot pot = potAndWinners.Key;
                Dictionary<GamePlayer, HandEvaluation> winners = potAndWinners.Value;

                SerializablePotWinners serPotWinners = new()
                {
                    PotAmount = pot.Amount,
                    HandType = winners.Values.First().Type.ToString()
                };

                foreach (var winner in potAndWinners.Value)
                {
                    GamePlayer player = winner.Key;
                    HandEvaluation hand = winner.Value;

                    SerializableWinner serWinner = new()
                    {
                        Cards = [.. hand.BestCards.Select(GetSerializableCard)],
                        Winner = new SerializablePlayer
                        {
                            Id = player.Id,
                            UserId = player.UserId,
                            Name = player.User?.DisplayName ?? "Unknown",
                            Position = player.Position,
                            Stack = player.Stack,
                            CurrentBet = player.CurrentBet,
                            HasFolded = player.HasFolded,
                            IsInGame = player.IsInGame,
                            IsSeated = player.IsSeated,
                            PokerPosition = (int)player.PokerPosition,
                            IsDealer = player.IsDealer
                        }
                    };
                    serPotWinners.Winners.Add(serWinner);
                }

                serializablePotWinners.Add(serPotWinners);
            }

            return serializablePotWinners;
        }

        public static Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> GetDeserializablePotWinners(List<SerializablePotWinners> serPotWinnersList, List<GamePlayer> players)
        {
            Dictionary<Pot, Dictionary<GamePlayer, HandEvaluation>> potWinners = [];

            foreach (var serPotWinners in serPotWinnersList)
            {
                List<int> potPlayers = [];
                Dictionary<GamePlayer, HandEvaluation> winnersForPot = [];

                foreach (var winner in serPotWinners.Winners)
                {
                    SerializablePlayer serPlayer = winner.Winner;
                    GamePlayer player = GetDeserializableGamePlayer(serPlayer);
                    HandEvaluation handEvaluation = new()
                    {
                        Type = Enum.Parse<HandType>(serPotWinners.HandType),
                        BestCards = [.. winner.Cards.Select(GetDeserializableCard)]
                    };
                    winnersForPot[player] = handEvaluation;

                    potPlayers.Add(players.FindIndex((p) => p.Id == player.Id));
                }

                Pot winningPot = new(potPlayers, serPotWinners.PotAmount);
                potWinners[winningPot] = winnersForPot;
            }

            return potWinners;
        }
    }
    public class SerializableGameSnapshot
    {
        public string TableId { get; set; } = string.Empty;
        public List<SerializablePlayer> Players { get; set; } = new();
        public List<int> TablePositions { get; set; } = new();
        public decimal PotAmount { get; set; }
        public List<decimal> SidePotAmounts { get; set; } = new();
        public decimal BigBlind { get; set; }
        public decimal SmallBlind { get; set; }
        public List<SerializableCard> CommunityCards { get; set; } = new();
        public string GameState { get; set; } = "NOT_STARTED";
        public bool BetsMade { get; set; }
        public int CurrentTurnPlayerIndex { get; set; } = -1;
        public decimal CurrentBet { get; set; }
        public List<SerializablePotWinners> Winners { get; set; } = new();
        public int TimerSeconds { get; set; } = 31;
        public int TimeRemaining { get; set; } = 31;
        public bool IsGamePaused { get; set; } = false;


        // Convert from GameSnapshot to SerializableGameSnapshot
        public static SerializableGameSnapshot FromGameSnapshot(GameSnapshot snapshot)
        {
            var serSnapshot = new SerializableGameSnapshot
            {
                TableId = snapshot.TableId,
                TablePositions = snapshot.TablePositions,
                PotAmount = snapshot.Pot?.Amount ?? 0,
                SidePotAmounts = snapshot.SidePotAmounts,
                BigBlind = snapshot.BigBlind,
                SmallBlind = snapshot.BigBlind / 2,
                GameState = snapshot.GameState.ToString(),
                BetsMade = snapshot.BetsMade,
                CurrentBet = snapshot.CurrentBet,
                TimerSeconds = snapshot.TimerSeconds,
                TimeRemaining = snapshot.TimeRemaining,
                IsGamePaused = snapshot.IsGamePaused
            };

            foreach (var card in snapshot.CommunityCards)
            {
                serSnapshot.CommunityCards.Add(SerializableGameData.GetSerializableCard(card));
            }

            Console.WriteLine("GameSnasphot to SerSnapshot");
            foreach (var player in snapshot.Players)
            {
                serSnapshot.Players.Add(SerializableGameData.GetSerializableGamePlayer(player));
                Console.WriteLine($"p: {player.Name} s: {SerializableGameData.GetSerializableGamePlayer(player).Name}");
            }

            // Set current turn player
            if (snapshot.TurnPlayer != null)
            {
                serSnapshot.CurrentTurnPlayerIndex = snapshot.Players.IndexOf(snapshot.TurnPlayer);
            }

            // Side pot amounts
            serSnapshot.SidePotAmounts = snapshot.SidePotAmounts;

            if (snapshot.AllWinners != null && snapshot.AllWinners.Count > 0)
            {
                serSnapshot.Winners = SerializableGameData.GetSerializablePotWinners(snapshot.AllWinners);
            }

            return serSnapshot;
        }

        // Helper method to convert back to a game state update
        public GameStateUpdate ToGameStateUpdate()
        {
            return new GameStateUpdate
            {
                TableId = TableId,
                GameState = Enum.Parse<GameState>(GameState),
                TablePositions = TablePositions,  // Add this line to include table positions
                CommunityCards = CommunityCards.Select(c => SerializableGameData.GetDeserializableCard(c)).ToList(),
                CurrentTurnPlayerIndex = CurrentTurnPlayerIndex,
                CurrentBet = CurrentBet,
                BetsMade = BetsMade,
                PotAmount = PotAmount,
                SidePotAmounts = SidePotAmounts,
                Players = Players.Select(p => SerializableGameData.GetDeserializableGamePlayer(p)).ToList(),
            };
        }
    }

    public class SerializableCard
    {
        public int Suit { get; set; }
        public int Rank { get; set; }
    }

    public class SerializablePlayer
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Position { get; set; }
        public decimal Stack { get; set; }
        public decimal CurrentBet { get; set; }
        public bool HasFolded { get; set; }
        public bool IsInGame { get; set; }
        public bool IsSeated { get; set; }
        public int PokerPosition { get; set; }
        public bool IsDealer { get; set; }
        public List<SerializableCard> Hand { get; set; } = [];
    }

    public class SerializablePotWinners
    {
        public List<SerializableWinner> Winners { get; set; } = [];
        public decimal PotAmount { get; set; }
        public string HandType { get; set; } = string.Empty;
    }

    public class SerializableWinner
    {
        public SerializablePlayer Winner { get; set; } = new();
        public List<SerializableCard> Cards { get; set; } = [];
    }

    // Class to handle game state updates in the client
    public class GameStateUpdate
    {
        public string TableId { get; set; } = string.Empty;
        public GameState GameState { get; set; }
        public List<int> TablePositions { get; set; } = new();
        public List<Card> CommunityCards { get; set; } = new();
        public int CurrentTurnPlayerIndex { get; set; }
        public decimal CurrentBet { get; set; }
        public bool BetsMade { get; set; }
        public decimal PotAmount { get; set; }
        public List<decimal> SidePotAmounts { get; set; } = new();
        public List<GamePlayer> Players { get; set; } = new();
    }

    public class PlayerUpdate
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Position { get; set; }
        public decimal Stack { get; set; }
        public decimal CurrentBet { get; set; }
        public bool HasFolded { get; set; }
        public bool IsInGame { get; set; }
        public bool IsSeated { get; set; }
        public PokerPosition PokerPosition { get; set; }
        public bool IsDealer { get; set; }
        public List<Card> Hand { get; set; } = new();
    }
}