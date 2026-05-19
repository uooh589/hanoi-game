using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class LibraryUI : MonoBehaviour
    {
        public Text contentText;
        public Button closeBtn, cardsTab, eventsTab, effectsTab, monstersTab;
        public InputField searchInput;
        public ToggleGroup elementGroup, levelGroup;
        public Toggle[] elementToggles, levelToggles;

        private string _tab = "cards";
        private string _filterElem = "";
        private int _filterLevel;
        private string _search = "";

        void Start()
        {
            closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
            cardsTab.onClick.AddListener(() => { _tab = "cards"; Refresh(); });
            eventsTab.onClick.AddListener(() => { _tab = "events"; Refresh(); });
            effectsTab.onClick.AddListener(() => { _tab = "effects"; Refresh(); });
            monstersTab.onClick.AddListener(() => { _tab = "monsters"; Refresh(); });
            searchInput?.onValueChanged.AddListener(v => { _search = v.ToLower(); Refresh(); });

            // Element filter toggles
            if (elementToggles != null)
                for (int i = 0; i < elementToggles.Length; i++) { int idx = i; elementToggles[i].onValueChanged.AddListener(v => { if (v) _filterElem = idx switch { 0=>"",1=>"Pyro",2=>"Cryo",3=>"Hydro",4=>"Electro",5=>"Anemo",6=>"Geo",7=>"Dendro",8=>"Omni" }; Refresh(); }); }
            if (levelToggles != null)
                for (int i = 0; i < levelToggles.Length; i++) { int idx = i; levelToggles[i].onValueChanged.AddListener(v => { if (v) _filterLevel = idx == 0 ? 0 : idx + 2; Refresh(); }); }

            Refresh();
        }

        void OnEnable() { Refresh(); }

        void Refresh()
        {
            switch (_tab)
            {
                case "cards": ShowCards(); break;
                case "events": ShowEvents(); break;
                case "effects": ShowEffects(); break;
                case "monsters": ShowMonsters(); break;
            }
        }

        void ShowCards()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"══ 卡牌图鉴 ({EffectPool.Pool.Sum(kv=>kv.Value.Count)}张) ══");
            foreach (var kv in EffectPool.Pool.OrderBy(k => k.Key))
            {
                foreach (var def in kv.Value)
                {
                    string elem = def.element.ToString();
                    if (!string.IsNullOrEmpty(_filterElem) && elem != _filterElem) continue;
                    if (_filterLevel > 0 && kv.Key != _filterLevel) continue;
                    if (!string.IsNullOrEmpty(_search) && !def.descTemplate.ToLower().Contains(_search)) continue;
                    sb.AppendLine($"[{kv.Key}层|{elem}] {def.descTemplate}");
                }
            }
            contentText.text = sb.ToString();
        }

        void ShowEvents()
        {
            var sb = new StringBuilder();
            sb.AppendLine("══ 事件图鉴 ══");
            sb.AppendLine("共120+随机事件，按原神地区分布：");
            sb.AppendLine("蒙德19 | 璃月17 | 稻妻14 | 须弥13 | 枫丹8 | 跨区域10 | 通用39");
            sb.AppendLine("");
            sb.AppendLine("效果类型：攻+1~3 | 倍率+0.01~0.06 | 恢复10~40%HP | 卡牌1~3张 | 摩拉±20~100");
            sb.AppendLine("");
            sb.AppendLine("风险事件：扣血换攻、倍率换卡、摩拉换卡等");
            contentText.text = sb.ToString();
        }

        void ShowEffects()
        {
            var sb = new StringBuilder();
            sb.AppendLine("══ 效果图鉴 ══");
            foreach (EffectType et in System.Enum.GetValues(typeof(EffectType)))
            {
                string desc = et switch
                {
                    EffectType.PureDamage => "造成固定伤害 (+攻击力加成)",
                    EffectType.DamageDraw => "造成伤害并抽取卡牌",
                    EffectType.PureShield => "获得护盾吸收伤害",
                    EffectType.ShieldReflect => "护盾+反弹部分伤害",
                    EffectType.PureHeal => "恢复生命值",
                    EffectType.HealCleanse => "治疗并清除负面状态",
                    EffectType.StepRecovery => "恢复可用步数",
                    EffectType.ComboChain => "伤害+下张牌伤害提升(连携)",
                    EffectType.MidDamage => "中等伤害",
                    EffectType.MidDamageDraw => "中等伤害+抽牌",
                    EffectType.MidDamageWeaken => "伤害+虚弱敌人(降低攻击)",
                    EffectType.MidShield => "中等护盾",
                    EffectType.MidShieldPermATK => "护盾+永久攻击力",
                    EffectType.MidHeal => "中等治疗",
                    EffectType.MidHealBoost => "治疗+下回合伤害提升",
                    EffectType.MidStepRecovery => "恢复较多步数",
                    EffectType.MidComboChain => "中等伤害+强连携",
                    EffectType.DamagePoison => "伤害+中毒(持续伤害)",
                    EffectType.DamageExecute => "伤害，敌人半血以下翻倍",
                    EffectType.DamageLifesteal => "伤害+吸血",
                    EffectType.ShieldBonusSteps => "护盾+额外步数",
                    EffectType.HeavyShield => "大量护盾",
                    EffectType.DamageShield => "伤害+护盾",
                    EffectType.HeavyHeal => "大量治疗",
                    EffectType.HeavyComboChain => "高伤害+强连携",
                    EffectType.DamagePierce => "伤害无视护盾",
                    EffectType.DamageStun => "伤害+禁锢(跳过敌人回合)",
                    EffectType.HeavyDamage => "高伤害",
                    EffectType.ShieldPermATKLarge => "大量护盾+永久攻击力",
                    EffectType.HealDamageBoost => "治疗+下回合高伤害加成",
                    EffectType.MassiveHeal => "巨量治疗",
                    EffectType.RefreshMultiplier => "恢复步数+步数倍率",
                    EffectType.DamagePerStep => "每剩余步数造成伤害",
                    EffectType.ShieldBurst => "护盾值转化为伤害",
                    EffectType.DamagePermATK => "巨额伤害+永久攻击力",
                    EffectType.DamageLifestealStrong => "高伤害+强吸血",
                    EffectType.DamageStunStrong => "高伤害+冰冻",
                    EffectType.MassiveDamage => "巨量伤害",
                    EffectType.RefreshMultiplierStrong => "大量步数+倍率",
                    EffectType.ExtraTurn => "伤害+获得额外回合",
                    EffectType.ShieldReflectStrong => "厚盾+强反弹",
                    EffectType.HealPermATK => "治疗+永久攻击力",
                    EffectType.Overkill => "伤害，击杀永久加攻",
                    EffectType.LuckyHit => "伤害，概率三倍暴击",
                    EffectType.TaskStepMultiplier => "任务卡：所有手牌置为差1步完成",
                    _ => ""
                };
                sb.AppendLine($"[{et}] {desc}");
            }
            contentText.text = sb.ToString();
        }

        void ShowMonsters()
        {
            var sb = new StringBuilder();
            sb.AppendLine("══ 怪物图鉴 ══");
            for (int stage = 0; stage < 3; stage++)
            {
                sb.AppendLine($"\n─ 第{stage+1}层 ─");
                foreach (var e in EnemyDatabase.Pools[stage])
                    sb.AppendLine($"  {e.name} HP:{e.baseHP} ATK:{e.baseATK} {e.region} {e.nativeElement?.ToString()??""}");
                sb.AppendLine("  [菁英]");
                foreach (var e in EnemyDatabase.ElitePools[stage])
                    sb.AppendLine($"  {e.name} HP:{e.baseHP} ATK:{e.baseATK} {e.region} {e.nativeElement?.ToString()??""}");
                sb.AppendLine("  [Boss]");
                foreach (var e in EnemyDatabase.BossPools[stage])
                    sb.AppendLine($"  {e.name} HP:{e.baseHP} ATK:{e.baseATK} {e.region}");
            }
            contentText.text = sb.ToString();
        }
    }
}
