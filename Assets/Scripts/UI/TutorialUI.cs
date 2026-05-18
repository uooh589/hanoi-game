using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class TutorialUI : MonoBehaviour
    {
        private int _step;
        private BattleManager _battle;
        private Text _msgText;
        private GameObject _msgGo;

        private static readonly string[] Messages = {
            "点击柱子顶部取出圆盘",
            "很好！再点另一根柱子放下",
            "完成汉诺塔发动卡牌效果",
            "点击【结束回合】让敌人行动",
        };

        void Start()
        {
            _battle = GameManager.Instance?.GetBattleManager();
            if (_battle != null)
                _battle.OnCardCompleted += OnHanoiDone;

            var font = FontHelper.GetFont(14);

            _msgGo = new GameObject("TutorialMsg", typeof(Text));
            _msgGo.transform.SetParent(transform, false);
            _msgText = _msgGo.GetComponent<Text>();
            _msgText.fontSize = 16;
            _msgText.alignment = TextAnchor.MiddleCenter;
            _msgText.color = new Color(1f, 0.84f, 0f);
            _msgText.font = font;
            _msgText.raycastTarget = false;
            var rt = _msgText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(500, 40);
            rt.anchoredPosition = new Vector2(0, 40);

            _step = 0;
            ShowStep(0);
        }

        void OnDestroy()
        {
            if (_battle != null)
                _battle.OnCardCompleted -= OnHanoiDone;
        }

        void ShowStep(int step)
        {
            if (_msgText != null)
                _msgText.text = Messages[Mathf.Min(step, Messages.Length - 1)];
        }

        void Update()
        {
            if (_battle == null) return;

            switch (_step)
            {
                case 0:
                    if (_battle.stepsConsumedThisTurn >= 1) { _step = 1; ShowStep(1); }
                    break;
                case 1:
                    if (_battle.stepsConsumedThisTurn >= 2) { _step = 2; ShowStep(2); }
                    break;
                case 3:
                    if (!_battle.IsPlayerTurn()) Complete();
                    break;
            }
        }

        void OnHanoiDone(int _)
        {
            if (_step == 2) { _step = 3; ShowStep(3); }
        }

        void Complete()
        {
            PlayerPrefs.SetInt("HanoiTutorialDone", 1);
            PlayerPrefs.Save();
            if (_msgGo) Destroy(_msgGo);
            enabled = false;
        }

        public static bool IsDone => PlayerPrefs.GetInt("HanoiTutorialDone", 0) == 1;
    }
}
