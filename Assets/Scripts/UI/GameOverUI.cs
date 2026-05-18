using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    /// <summary>
    /// Game over screen — shows defeat and option to return to menu.
    /// Attach to GameOverPanel.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        public Text resultText;
        public Text statsText;
        public Button returnButton;

        private void OnEnable()
        {
            if (resultText != null)
                resultText.text = "败北";

            if (statsText != null)
                statsText.text = $"到达第 {GameManager.Instance.currentStage} 关\n步数倍率: ×{GameManager.Instance.stepMultiplier:F2}\n卡组数量: {GameManager.Instance.Deck.cards.Count}";

            if (returnButton != null)
            {
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    GameManager.Instance.ReturnToMenu();
                });
            }
        }
    }
}
