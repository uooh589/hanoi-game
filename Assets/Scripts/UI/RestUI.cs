using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class RestUI : MonoBehaviour
    {
        public Text infoText;
        public Text effectText;
        public Button continueButton;

        public void Show(string title, string effect)
        {
            gameObject.SetActive(true);
            if (infoText) infoText.text = title;
            if (effectText) effectText.text = effect;
            if (continueButton)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    gameObject.SetActive(false);
                    GameManager.Instance.OnRestComplete();
                });
            }
        }
    }
}
