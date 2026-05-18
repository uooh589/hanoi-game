using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class ChestUI : MonoBehaviour
    {
        public Text infoText;   // title
        public Text cardText;   // card details
        public Button okButton;

        public void Show(CardData card, int gold)
        {
            gameObject.SetActive(true);
            if (infoText) infoText.text = "发现宝箱！";
            if (cardText) cardText.text = $"获得卡牌: {card.towerLevel}层汉诺塔\n\n{card.effectDescription}\n\n摩拉 +{gold}";
            if (okButton)
            {
                okButton.onClick.RemoveAllListeners();
                okButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    gameObject.SetActive(false);
                    GameManager.Instance.OnChestDone();
                });
            }
        }
    }
}
