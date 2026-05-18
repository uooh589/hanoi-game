using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class EventUI : MonoBehaviour
    {
        public Text eventText;
        public Button[] choiceButtons;
        public Text[] choiceLabels;

        private System.Action[] _callbacks;

        /// <summary>Show an event with up to 3 choices.</summary>
        public void Show(string description, (string label, System.Action action)[] choices)
        {
            gameObject.SetActive(true);
            if (eventText) eventText.text = description;

            _callbacks = new System.Action[choices.Length];
            for (int i = 0; i < 3; i++)
            {
                if (i < choices.Length && choiceButtons != null && i < choiceButtons.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    if (choiceLabels != null && i < choiceLabels.Length)
                        choiceLabels[i].text = choices[i].label;
                    int idx = i;
                    _callbacks[i] = choices[i].action;
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnPick(idx));
                }
                else if (choiceButtons != null && i < choiceButtons.Length)
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        void OnPick(int idx)
        {
            SimpleAudio.Instance?.PlayClick();
            gameObject.SetActive(false);
            _callbacks?[idx]?.Invoke();
        }
    }
}
