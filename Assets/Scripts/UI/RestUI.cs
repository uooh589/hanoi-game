using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class RestUI : MonoBehaviour
    {
        public Text infoText;
        public Text effectText;
        public Button continueButton;

        public void Show(int healAmount, float stepMult, int atkBonus)
        {
            gameObject.SetActive(true);
            if (infoText) infoText.text = "七天神像的祝福";
            if (effectText) effectText.text = $"生命上限永久 +{healAmount}\n当前元素共鸣 ×{stepMult:F2}\n攻击力加成 +{atkBonus}";
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
