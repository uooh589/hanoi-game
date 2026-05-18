using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    /// <summary>
    /// All possible card effects. Rich pool for roguelike variety.
    /// </summary>
    public enum EffectType
    {
        // Level 3 (weak)
        PureDamage,
        DamageDraw,
        PureShield,
        ShieldReflect,
        PureHeal,
        HealCleanse,
        StepRecovery,
        ComboChain,

        // Level 4
        MidDamage,
        MidDamageDraw,
        MidDamageWeaken,
        MidShield,
        MidShieldPermATK,
        MidHeal,
        MidHealBoost,
        MidStepRecovery,
        MidComboChain,

        // Level 5
        DamagePoison,
        DamageExecute,
        DamageLifesteal,
        ShieldBonusSteps,
        HeavyShield,
        DamageShield,
        HeavyHeal,
        HeavyComboChain,

        // Level 6
        DamagePierce,
        DamageStun,
        HeavyDamage,
        ShieldPermATKLarge,
        HealDamageBoost,
        MassiveHeal,
        RefreshMultiplier,
        DamagePerStep,
        ShieldBurst,

        // Level 7 (very strong)
        DamagePermATK,
        DamageLifestealStrong,
        DamageStunStrong,
        MassiveDamage,
        RefreshMultiplierStrong,
        ExtraTurn,
        ShieldReflectStrong,
        HealPermATK,
        Overkill,
        LuckyHit,

        // Task card
        TaskStepMultiplier
    }

    /// <summary>
    /// Serializable card data. Each card is a Hanoi tower puzzle + combat effect.
    /// </summary>
    [System.Serializable]
    public class CardData
    {
        public string id;
        public int towerLevel;      // 3-7 (or 8 for task)
        public EffectType effectType;
        public int effectValue1;    // Primary value (damage, shield, etc.)
        public int effectValue2;    // Secondary value (duration, percentage, etc.)
        public float effectValueF;  // Float value (multipliers)
        public bool isTaskCard;
        public Element element;
        public string effectDescription;

        // Transient puzzle state (serialized for task card persistence)
        public List<int> peg0State = new List<int>();
        public List<int> peg1State = new List<int>();
        public List<int> peg2State = new List<int>();
        public int taskStepsUsed;

        public CardData() { id = Guid.NewGuid().ToString().Substring(0, 8); }

        public CardData Clone()
        {
            var c = new CardData
            {
                id = Guid.NewGuid().ToString().Substring(0, 8),
                towerLevel = towerLevel,
                effectType = effectType,
                effectValue1 = effectValue1,
                effectValue2 = effectValue2,
                effectValueF = effectValueF,
                isTaskCard = isTaskCard,
                element = element,
                effectDescription = effectDescription
            };
            // Clone peg state for task card
            c.peg0State = new List<int>(peg0State);
            c.peg1State = new List<int>(peg1State);
            c.peg2State = new List<int>(peg2State);
            c.taskStepsUsed = taskStepsUsed;
            return c;
        }

        /// <summary>
        /// Optimal moves required: 2^n - 1
        /// </summary>
        public static int GetOptimalSteps(int level)
        {
            return (1 << level) - 1;
        }

        public int OptimalSteps => GetOptimalSteps(towerLevel);

        /// <summary>
        /// Restore peg state from saved lists.
        /// </summary>
        public void SetPegState(int peg, List<int> state)
        {
            switch (peg)
            {
                case 0: peg0State = new List<int>(state); break;
                case 1: peg1State = new List<int>(state); break;
                case 2: peg2State = new List<int>(state); break;
            }
        }

        public List<int> GetPegState(int peg)
        {
            switch (peg)
            {
                case 0: return peg0State;
                case 1: return peg1State;
                case 2: return peg2State;
                default: return null;
            }
        }
    }

    /// <summary>
    /// Defines the effect pool. Each entry binds a tower level to an effect preset.
    /// Used to generate random cards for rewards and the initial deck.
    /// </summary>
    public static class EffectPool
    {
        public class EffectDef
        {
            public EffectType type;
            public int v1, v2;
            public float vf;
            public Element element;
            public string descTemplate; // {0}=v1, {1}=v2, {2}=vf*100
            public string tooltip;

            public string Format(int lv)
            {
                return string.Format(descTemplate, v1, v2, (int)(vf * 100));
            }
        }

        // shorthand
        static EffectDef E(EffectType t, int v1, int v2, float vf, Element e, string desc, string tip) =>
            new() { type=t, v1=v1, v2=v2, vf=vf, element=e, descTemplate=desc, tooltip=tip };

        public static readonly Dictionary<int, List<EffectDef>> Pool = new()
        {
            [3] = new() {
                E(EffectType.PureDamage,   5, 0, 0,    Element.Pyro,    "安柏·爆弹 — {0}伤害",         "造成{0}点火焰伤害"),
                E(EffectType.DamageDraw,   4, 1, 0,    Element.Anemo,   "砂糖·转化 — {0}伤害，抽{1}牌",  "造成伤害并抽取卡牌"),
                E(EffectType.PureShield,   6, 0, 0,    Element.Geo,     "诺艾尔·护心铠 — {0}护盾",       "获得护盾吸收伤害"),
                E(EffectType.ShieldReflect,5, 0, 0.2f, Element.Electro, "北斗·捉浪 — {0}护盾，反弹{2}%", "护盾+反弹部分伤害"),
                E(EffectType.PureHeal,     8, 0, 0,    Element.Hydro,   "芭芭拉·歌声 — 恢复{0}生命",     "恢复生命值"),
                E(EffectType.HealCleanse,  5, 0, 0,    Element.Pyro,    "班尼特·鼓舞 — 恢复{0}，清除中毒", "治疗并清除所有负面状态"),
                E(EffectType.StepRecovery, 8, 0, 0,    Element.Anemo,   "枫原万叶·风引 — +{0}步",         "恢复步数"),
                E(EffectType.ComboChain,   3, 0, 0.2f, Element.Hydro,   "行秋·裁雨 — {0}伤害，连携+{2}%","伤害并提升下张牌伤害倍率"),
            },
            [4] = new() {
                E(EffectType.MidDamage,       9, 0, 0,    Element.Electro, "菲谢尔·夜巡 — {0}伤害",         "造成雷电伤害"),
                E(EffectType.MidDamageDraw,   7, 1, 0,    Element.Dendro,  "柯莱·飞叶 — {0}伤害，抽{1}牌",  "伤害并抽牌"),
                E(EffectType.MidDamageWeaken, 6, 0, 0.3f, Element.Electro,"丽莎·蔷薇 — {0}伤害，虚弱{2}%","伤害并降低敌人攻击力"),
                E(EffectType.MidShield,      12, 0, 0,   Element.Cryo,   "迪奥娜·猫爪 — {0}护盾",          "获得冰元素护盾"),
                E(EffectType.MidShieldPermATK,8, 2, 0,   Element.Pyro,    "辛焱·摇滚 — {0}护盾，永久+{1}攻","护盾并永久提升攻击力"),
                E(EffectType.MidHeal,        12, 0, 0,   Element.Cryo,    "七七·仙法 — 恢复{0}生命",        "恢复生命值"),
                E(EffectType.MidHealBoost,    8, 0, 0.25f,Element.Anemo,  "琴·蒲公英 — 恢复{0}，下回+{2}%","治疗并提升下回合伤害"),
                E(EffectType.MidStepRecovery,12, 0, 0,   Element.Anemo,   "早柚·疾风 — +{0}步",             "恢复大量步数"),
                E(EffectType.MidComboChain,   7, 0, 0.3f,Element.Cryo,   "重云·灵刃 — {0}伤害，连携+{2}%","伤害并提升下张牌伤害"),
            },
            [5] = new() {
                E(EffectType.DamagePoison,    15, 3, 3f,  Element.Pyro,    "胡桃·蝶引 — {0}伤害，灼烧{1}回","伤害并附加灼烧(每回3伤害)"),
                E(EffectType.DamageExecute,   12, 0, 0,   Element.Cryo,    "罗莎莉亚·处刑 — {0}伤害，半血×2","敌人半血以下伤害翻倍"),
                E(EffectType.DamageLifesteal, 10, 0, 0.4f,Element.Cryo,   "神里绫华·冰华 — {0}伤害，吸血{2}%","造成伤害并恢复生命"),
                E(EffectType.ShieldBonusSteps,10, 8, 0,  Element.Geo,     "钟离·玉璋 — {0}护盾，+{1}步",     "护盾并恢复步数"),
                E(EffectType.HeavyShield,    15, 0, 0,   Element.Cryo,    "莱依拉·星盾 — {0}护盾",          "获得厚实的冰盾"),
                E(EffectType.DamageShield,    8, 8, 0,   Element.Geo,     "凝光·璇玑 — {0}伤害+{1}护盾",    "伤害同时获得护盾"),
                E(EffectType.HeavyHeal,      18, 0, 0,   Element.Hydro,   "珊瑚宫心海·水母 — 恢复{0}生命",  "恢复大量生命"),
                E(EffectType.HeavyComboChain, 12, 0, 0.4f,Element.Pyro,   "香菱·锅巴 — {0}伤害，连携+{2}%","伤害并大幅提升下张牌"),
            },
            [6] = new() {
                E(EffectType.DamagePierce,    25, 0, 0,   Element.Hydro,   "达达利亚·断流 — {0}伤害无视护盾","无视敌人护盾直接造成伤害"),
                E(EffectType.DamageStun,      20, 1, 0,   Element.Hydro,   "莫娜·星命 — {0}伤害，禁锢{1}回","伤害并使敌人跳过回合"),
                E(EffectType.HeavyDamage,     28, 0, 0,   Element.Electro, "刻晴·星斗 — {0}伤害",            "造成大量雷电伤害"),
                E(EffectType.ShieldPermATKLarge,18,2,0,   Element.Geo,     "荒泷一斗·鬼铠 — {0}护盾，+{1}攻","护盾并永久提升攻击力"),
                E(EffectType.HealDamageBoost, 20, 0, 0.5f,Element.Dendro, "白术·长生 — 恢复{0}，下回+{2}%","治疗并大幅提升下回伤害"),
                E(EffectType.MassiveHeal,     25, 0, 0,   Element.Dendro,  "瑶瑶·月桂 — 恢复{0}生命",        "恢复巨量生命"),
                E(EffectType.RefreshMultiplier,10,0, 0.03f,Element.Anemo, "鹿野院平藏·心算 — +{0}步，倍率+{2}%","恢复步数并提升倍率"),
                E(EffectType.DamagePerStep,    3, 0, 0,   Element.Electro, "赛诺·审判 — 每步{0}伤害",        "剩余步数×每步伤害值"),
                E(EffectType.ShieldBurst,      0, 0, 1.5f,Element.Hydro,  "坎蒂丝·守护 — 护盾×{2}%伤害",    "当前护盾值转化为伤害"),
            },
            [7] = new() {
                E(EffectType.DamagePermATK,    50, 3, 0,   Element.Electro, "雷电将军·无想一刀 — {0}伤害+{1}攻","巨额伤害并永久提升攻击力"),
                E(EffectType.DamageLifestealStrong,35,0,0.5f,Element.Electro,"八重神子·狐灵 — {0}伤害，吸血{2}%","伤害并大量吸血"),
                E(EffectType.DamageStunStrong, 40, 1, 0,  Element.Cryo,    "甘雨·霜华 — {0}伤害，冰封{1}回","巨额冰伤并冻结敌人"),
                E(EffectType.MassiveDamage,    45, 0, 0,   Element.Pyro,    "迪卢克·天焰 — {0}伤害",          "造成巨额火焰伤害"),
                E(EffectType.RefreshMultiplierStrong,20,0,0.05f,Element.Dendro,"纳西妲·慧眼 — +{0}步，倍率+{2}%","恢复大量步数并提升倍率"),
                E(EffectType.ExtraTurn,        30, 0, 0,   Element.Hydro,   "神里绫人·镜花 — {0}伤害，额外回","伤害并获得额外回合"),
                E(EffectType.ShieldReflectStrong,30,0,0.5f,Element.Dendro, "艾尔海森·镜闪 — {0}护盾，反弹{2}%","厚盾并大幅反弹伤害"),
                E(EffectType.HealPermATK,      35, 2, 0,   Element.Hydro,   "夜兰·幽玄 — 恢复{0}生命，+{1}攻","治疗并永久提升攻击力"),
                E(EffectType.Overkill,         40, 3, 0,   Element.Anemo,   "魈·靖妖 — {0}伤害，击杀+{1}攻","伤害，击杀敌人永久加攻"),
                E(EffectType.LuckyHit,         30, 0, 0.4f,Element.Pyro,   "可莉·轰轰火花 — {0}伤害，{2}%×3","概率触发三倍伤害"),
            },
            [8] = new() {
                E(EffectType.TaskStepMultiplier,0,0,0.02f,Element.Omni,"天理·维系者 — 全卡牌预完成","所有手牌变为只差1步完成"),
            },
        };

        /// <summary>
        /// Generate a random CardData for the given tower level.
        /// </summary>
        public static CardData GenerateRandomCard(int level)
        {
            if (!Pool.ContainsKey(level) || Pool[level].Count == 0) return null;
            var def = Pool[level][UnityEngine.Random.Range(0, Pool[level].Count)];
            return new CardData
            {
                towerLevel = level,
                effectType = def.type,
                effectValue1 = def.v1,
                effectValue2 = def.v2,
                effectValueF = def.vf,
                isTaskCard = (level == 8),
                element = def.element,
                effectDescription = def.Format(level)
            };
        }

        /// <summary>
        /// Create the initial 3-card deck: 3-layer damage, 4-layer shield, 8-layer task.
        /// </summary>
        public static List<CardData> CreateInitialDeck()
        {
            var deck = new List<CardData>
            {
                new CardData
                {
                    towerLevel = 3, element = Element.Pyro,
                    effectType = EffectType.PureDamage,
                    effectValue1 = 5,
                    isTaskCard = false,
                    effectDescription = "安柏·爆弹 — 造成5点伤害"
                },
                new CardData
                {
                    towerLevel = 4, element = Element.Geo,
                    effectType = EffectType.PureShield,
                    effectValue1 = 8,
                    isTaskCard = false,
                    effectDescription = "诺艾尔·护心铠 — 获得8点护盾"
                },
                new CardData
                {
                    towerLevel = 8,
                    effectType = EffectType.TaskStepMultiplier,
                    effectValueF = 0.1f,
                    isTaskCard = true,
                    effectDescription = "天理·维系者 — 步数倍率×1.1(累乘)"
                },
            };
            return deck;
        }
    }
}
