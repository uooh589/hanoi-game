using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class CardRewardUI : MonoBehaviour
    {
        public Text titleText;
        public Text descText;
        public Button confirmButton;
        public Button nextButton;
        public RectTransform hanoiPreviewArea; // where Hanoi tower renders

        private List<CardData> _choices;
        private int _currentIdx;
        private HanoiUI _previewHanoi;

        public void ShowRewards(int stageLevel, int count = 3)
        {
            gameObject.SetActive(true);
            _choices = GameManager.Instance.Deck.GenerateRewardChoices(stageLevel, count);
            _currentIdx = 0;

            if (titleText) titleText.text = $"战斗胜利！选择一张卡牌（第{stageLevel}关）";

            ShowCard(_currentIdx);

            if (confirmButton)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayComplete();
                    if (_previewHanoi) Destroy(_previewHanoi.gameObject);
                    gameObject.SetActive(false);
                    GameManager.Instance.OnCardSelected(_choices[_currentIdx]);
                });
            }

            if (nextButton)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    _currentIdx = (_currentIdx + 1) % _choices.Count;
                    ShowCard(_currentIdx);
                });
            }
        }

        void ShowCard(int idx)
        {
            if (idx < 0 || idx >= _choices.Count) return;
            var card = _choices[idx];

            if (descText)
                descText.text = $"[{idx + 1}/{_choices.Count}]  {card.towerLevel}层汉诺塔\n{card.effectDescription}\n最优步数: {card.OptimalSteps}";

            // Rebuild Hanoi preview
            if (_previewHanoi) Destroy(_previewHanoi.gameObject);

            if (hanoiPreviewArea != null)
            {
                var go = new GameObject("PreviewHanoi", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HanoiUI));
                go.transform.SetParent(hanoiPreviewArea, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(340, 240);
                rt.anchoredPosition = Vector2.zero;
                go.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.14f, 0.9f);

                var hui = go.GetComponent<HanoiUI>();
                hui.handIndex = -1; // preview mode
                hui.uiFont = FontHelper.GetFont(12);
                var puzzle = new HanoiPuzzle(card.towerLevel);
                hui.Initialize(puzzle, card, -1, null);
                _previewHanoi = hui;
            }
        }
    }
}
