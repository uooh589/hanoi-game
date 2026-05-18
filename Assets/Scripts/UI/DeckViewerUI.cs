using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class DeckViewerUI : MonoBehaviour
    {
        public Text cardListText;
        public Button closeButton;
        public ScrollRect scrollRect;

        void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    gameObject.SetActive(false);
                });
            }
        }

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (cardListText == null) return;
            var deck = GameManager.Instance?.Deck;
            if (deck == null || deck.cards.Count == 0)
            {
                cardListText.text = "卡组为空";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"卡组 ({deck.cards.Count}张)");
            sb.AppendLine("─────────────────");
            foreach (var c in deck.cards)
            {
                string tag = c.isTaskCard ? "★" : "";
                sb.AppendLine($"{tag}{c.towerLevel}层: {c.effectDescription}");
            }
            cardListText.text = sb.ToString();
        }
    }
}
