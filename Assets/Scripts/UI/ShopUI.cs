using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class ShopUI : MonoBehaviour
    {
        public Text titleText;
        public Text moraText;
        public Button[] buyButtons;
        public Text[] buyLabels;
        public Button leaveButton;

        private List<CardData> _cards;
        private int[] _prices;

        public void Show()
        {
            gameObject.SetActive(true);
            var gm = GameManager.Instance;
            if (titleText) titleText.text = $"商人 — 当前摩拉: {gm.mora}";
            if (moraText) moraText.text = $"摩拉: {gm.mora}";

            _cards = gm.Deck.GenerateRewardChoices(gm.currentStage, 3);
            _prices = new int[3];
            for (int i = 0; i < 3; i++)
            {
                _prices[i] = 60 + _cards[i].towerLevel * 15 + Random.Range(0, 30);
                if (buyLabels != null && i < buyLabels.Length && buyLabels[i] != null)
                    buyLabels[i].text = $"{_cards[i].towerLevel}层\n{_cards[i].effectDescription}\n{_prices[i]} 摩拉";
                if (buyButtons != null && i < buyButtons.Length && buyButtons[i] != null)
                {
                    int idx = i;
                    buyButtons[i].onClick.RemoveAllListeners();
                    buyButtons[i].onClick.AddListener(() => Buy(idx));
                    buyButtons[i].interactable = gm.mora >= _prices[i];
                }
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); gameObject.SetActive(false); gm.OnShopDone(); });
            }
        }

        void Buy(int idx)
        {
            var gm = GameManager.Instance;
            if (gm.mora < _prices[idx]) return;
            gm.mora -= _prices[idx];
            gm.Deck.AddCard(_cards[idx]);
            gm.SaveManager_();
            SimpleAudio.Instance?.PlayComplete();
            Show(); // refresh
        }
    }
}
