using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    /// <summary>
    /// Manages the player's card collection: shuffle, draw, add, remove.
    /// </summary>
    [System.Serializable]
    public class DeckManager
    {
        public List<CardData> cards = new List<CardData>();

        /// <summary>
        /// Initialize deck with starting cards. Call once per new game.
        /// </summary>
        public void InitStartingDeck()
        {
            cards = EffectPool.CreateInitialDeck();
        }

        /// <summary>
        /// Add a card to the collection.
        /// </summary>
        public void AddCard(CardData card)
        {
            cards.Add(card);
        }

        /// <summary>
        /// Draw N random cards from the deck. If the 8-layer task card is drawn,
        /// preserve its persistent puzzle state. Returns the drawn cards.
        /// Hand size limited to 3 — excess draws are skipped.
        /// </summary>
        public List<CardData> DrawHand(int count, CardData currentTaskCard)
        {
            var hand = new List<CardData>();
            var available = new List<CardData>(cards);
            // Shuffle
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = available[i];
                available[i] = available[j];
                available[j] = tmp;
            }

            foreach (var card in available)
            {
                if (hand.Count >= count) break;
                if (card.isTaskCard) continue; // task card is handled separately
                hand.Add(card.Clone());
            }
            return hand;
        }

        /// <summary>
        /// Generate 3 random cards (levels 3-7) for post-battle reward selection.
        /// </summary>
        public List<CardData> GenerateRewardChoices(int stageLevel, int count = 3)
        {
            var choices = new List<CardData>();
            var possibleLevels = new List<int> { 3, 4, 5, 6, 7 };

            for (int i = 0; i < count; i++)
            {
                // Boss rewards (count=4): levels 5-7 only
                int level = count >= 4
                    ? possibleLevels[Random.Range(2, possibleLevels.Count)] // levels 5,6,7
                    : possibleLevels[Random.Range(0, Mathf.Min(possibleLevels.Count, 2 + stageLevel / 2))];
                if (stageLevel >= 3 && Random.value < 0.3f) level = Mathf.Min(7, level + 1);
                if (stageLevel >= 5 && Random.value < 0.2f) level = Mathf.Min(7, level + 2);
                var card = EffectPool.GenerateRandomCard(level);
                choices.Add(card);
            }
            return choices;
        }

        /// <summary>
        /// Count of non-task cards in deck.
        /// </summary>
        public int CombatCardCount
        {
            get
            {
                int count = 0;
                foreach (var c in cards) if (!c.isTaskCard) count++;
                return count;
            }
        }
    }
}
