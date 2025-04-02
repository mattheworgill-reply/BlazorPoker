using PokerApp.Models.Enums;

namespace PokerApp.Models;

public class Deck
{
    public List<Card> Cards { get; set; }
    public List<Card> RemovedCards { get; set; }

    public Deck()
    {
        Cards = new List<Card>();
        RemovedCards = new List<Card>();
        InitializeDeck();
    }

    public void InitializeDeck()
    {
        Cards.Clear();
        RemovedCards.Clear();

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                Cards.Add(new Card(suit, rank));
            }
        }

        ShuffleCards();
    }

    public void ShuffleDeck()
    {
        foreach (Card card in RemovedCards)
        {
            Cards.Add(card);
        }

        RemovedCards.Clear();
        ShuffleCards();
    }

    public void ShuffleCards()
    {
        for (int i = 0; i < 10; i++)
        {
            Random rand = new Random();
            Cards = Cards.OrderBy(c => rand.Next()).ToList();
        }
    }

    public Card Deal()
    {
        if (Cards.Count == 0)
            throw new InvalidOperationException("No cards left in the deck.");

        Card card = Cards.Last();
        Cards.RemoveAt(Cards.Count - 1);
        RemovedCards.Add(card);

        return card;
    }

    public List<Card> DealHand(int numberOfCards)
    {
        List<Card> hand = new List<Card>();
        for (int i = 0; i < numberOfCards; i++)
        {
            hand.Add(Deal());
        }
        return hand;
    }
}