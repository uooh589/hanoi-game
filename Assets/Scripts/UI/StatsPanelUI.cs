using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HanoiGame
{
    public class StatsPanelUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Text statsText;
        public Button closeButton;
        public GameObject tooltipPanel;
        public Text tooltipText;

        static readonly (string key, string desc)[] Tooltips = {
            ("攻击力", "基础攻击力 + 永久加成\n影响所有伤害卡牌的效果"),
            ("步数倍率", "每回合获得的总步数 = 卡牌最优步数 × 倍率\n完成任务牌可提升"),
            ("护盾", "吸收伤害，优先于生命值扣除"),
            ("连携", "完成卡牌后可触发连携\n下一张卡牌伤害按倍率提升"),
            ("元素反应", "连续完成两张不同元素卡牌\n触发对应元素反应效果"),
            ("中毒", "每回合扣除固定伤害\n持续若干回合"),
            ("弱化", "伤害倍率降低\n持续若干回合"),
            ("眩晕", "跳过敌人的行动回合"),
            ("灼烧/毒", "敌人类似中毒效果\n每回合扣除固定伤害"),
            ("冻结", "跳过敌人1回合行动"),
            ("草核", "下回合爆炸造成范围伤害"),
        };

        void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        void Refresh()
        {
            var gm = GameManager.Instance;
            var battle = gm?.GetBattleManager();
            if (battle == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("══ 旅行者属性 ══");
            sb.AppendLine($"攻击力: {battle.baseATK + gm.permanentATKBonus}  (基础{battle.baseATK} + 永久{gm.permanentATKBonus})");
            sb.AppendLine($"当前生命: {battle.playerHP}/{battle.playerMaxHP + gm.maxHPBonus}");
            sb.AppendLine($"护盾: {battle.playerShield}");
            sb.AppendLine($"步数倍率: ×{gm.stepMultiplier:F2}");
            sb.AppendLine($"剩余步数: {battle.stepsRemaining}");
            sb.AppendLine($"刷新次数: {battle.refreshCharges}/{battle.maxRefreshCharges}");
            sb.AppendLine($"任务步数: {gm.taskSteps}");
            sb.AppendLine($"摩拉: {gm.mora}");

            sb.AppendLine("");
            sb.AppendLine("══ 状态效果 ══");
            if (battle.playerPoisonTurns > 0) sb.AppendLine($"中毒: {battle.playerPoisonDamage}×{battle.playerPoisonTurns}回");
            if (battle.playerWeakenTurns > 0) sb.AppendLine($"弱化: -{(int)(battle.playerWeakenPercent*100)}% ({battle.playerWeakenTurns}回)");
            if (battle.comboCharges > 0) sb.AppendLine($"连携: ×{battle.comboMultiplier:F1} ({battle.comboCharges}回)");
            if (battle.pendingReflectTurns > 0) sb.AppendLine($"反射: {(int)(battle.pendingReflectPercent*100)}%×{battle.pendingReflectTurns}回");
            if (battle.blockedPegTurns > 0) sb.AppendLine($"柱子封锁: {battle.blockedPegTurns}回");

            sb.AppendLine("");
            sb.AppendLine("══ 卡组 ({0}) ══".Replace("{0}", (gm.Deck?.cards.Count ?? 0).ToString()));
            if (gm.Deck != null)
                foreach (var c in gm.Deck.cards)
                    sb.AppendLine($"{(c.isTaskCard?"★":"")}{c.towerLevel}层 {c.element}: {c.effectDescription}");

            if (statsText != null) statsText.text = sb.ToString();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            // Show tooltip for the hovered stat line
            if (tooltipPanel == null || tooltipText == null) return;
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }
    }
}
