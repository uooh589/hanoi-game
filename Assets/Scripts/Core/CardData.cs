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
            public string descTemplate; // {0}=v1, {1}=v2, {2}=vf*100

            public string Format(int lv)
            {
                return string.Format(descTemplate, v1, v2, (int)(vf * 100));
            }
        }

        public static readonly Dictionary<int, List<EffectDef>> Pool = new Dictionary<int, List<EffectDef>>
        {
            [3] = new List<EffectDef>
            {
                new EffectDef { type = EffectType.PureDamage,       v1 = 5,  v2 = 0,  vf = 0,    descTemplate = "安柏·爆弹 — 造成{0}点伤害" },
                new EffectDef { type = EffectType.DamageDraw,       v1 = 4,  v2 = 1,  vf = 0,    descTemplate = "砂糖·转化 — {0}伤害，抽{1}牌" },
                new EffectDef { type = EffectType.PureShield,       v1 = 6,  v2 = 0,  vf = 0,    descTemplate = "诺艾尔·护心铠 — {0}护盾" },
                new EffectDef { type = EffectType.ShieldReflect,    v1 = 4,  v2 = 0,  vf = 0.2f, descTemplate = "北斗·捉浪 — {0}护盾，反弹{2}%" },
                new EffectDef { type = EffectType.PureHeal,         v1 = 8,  v2 = 0,  vf = 0,    descTemplate = "芭芭拉·歌声 — 恢复{0}生命" },
                new EffectDef { type = EffectType.HealCleanse,      v1 = 5,  v2 = 0,  vf = 0,    descTemplate = "班尼特·鼓舞 — 恢复{0}，清除元素" },
                new EffectDef { type = EffectType.StepRecovery,     v1 = 3,  v2 = 0,  vf = 0,    descTemplate = "枫原万叶·风引 — +{0}步" },
                new EffectDef { type = EffectType.ComboChain,       v1 = 3,  v2 = 0,  vf = 0.15f,descTemplate = "行秋·裁雨 — {0}伤害，连携+{2}%" },
            },
            [4] = new List<EffectDef>
            {
                new EffectDef { type = EffectType.MidDamage,         v1 = 9,  v2 = 0,  vf = 0,    descTemplate = "菲谢尔·夜巡 — 造成{0}点伤害" },
                new EffectDef { type = EffectType.MidDamageDraw,     v1 = 7,  v2 = 1,  vf = 0,    descTemplate = "柯莱·飞叶 — {0}伤害，抽{1}牌" },
                new EffectDef { type = EffectType.MidDamageWeaken,   v1 = 6,  v2 = 0,  vf = 0.3f, descTemplate = "丽莎·蔷薇 — {0}伤害，削弱{2}%" },
                new EffectDef { type = EffectType.MidShield,         v1 = 12, v2 = 0,  vf = 0,    descTemplate = "迪奥娜·猫爪 — {0}护盾" },
                new EffectDef { type = EffectType.MidShieldPermATK,  v1 = 8,  v2 = 1,  vf = 0,    descTemplate = "辛焱·摇滚 — {0}护盾，永久+{1}攻" },
                new EffectDef { type = EffectType.MidHeal,           v1 = 12, v2 = 0,  vf = 0,    descTemplate = "七七·仙法 — 恢复{0}生命" },
                new EffectDef { type = EffectType.MidHealBoost,      v1 = 8,  v2 = 0,  vf = 0.2f, descTemplate = "琴·蒲公英 — 恢复{0}，下回+{2}%" },
                new EffectDef { type = EffectType.MidStepRecovery,   v1 = 5,  v2 = 0,  vf = 0,    descTemplate = "早柚·疾风 — +{0}步" },
                new EffectDef { type = EffectType.MidComboChain,     v1 = 7,  v2 = 0,  vf = 0.25f,descTemplate = "重云·灵刃 — {0}伤害，连携+{2}%" },
            },
            [5] = new List<EffectDef>
            {
                new EffectDef { type = EffectType.DamagePoison,      v1 = 15, v2 = 3,  vf = 3f,  descTemplate = "胡桃·蝶引 — {0}伤害，灼烧3回" },
                new EffectDef { type = EffectType.DamageExecute,     v1 = 12, v2 = 0,  vf = 0,    descTemplate = "罗莎莉亚·处刑 — {0}伤害，半血翻倍" },
                new EffectDef { type = EffectType.DamageLifesteal,   v1 = 10, v2 = 0,  vf = 0.4f, descTemplate = "神里绫华·冰华 — {0}伤害，吸血{2}%" },
                new EffectDef { type = EffectType.ShieldBonusSteps,  v1 = 10, v2 = 5,  vf = 0,    descTemplate = "钟离·玉璋 — {0}护盾，+{1}步" },
                new EffectDef { type = EffectType.HeavyShield,       v1 = 15, v2 = 0,  vf = 0,    descTemplate = "莱依拉·星盾 — {0}护盾" },
                new EffectDef { type = EffectType.DamageShield,      v1 = 8,  v2 = 8,  vf = 0,    descTemplate = "凝光·璇玑 — {0}伤害，{1}护盾" },
                new EffectDef { type = EffectType.HeavyHeal,         v1 = 18, v2 = 0,  vf = 0,    descTemplate = "珊瑚宫心海·水母 — 恢复{0}" },
                new EffectDef { type = EffectType.HeavyComboChain,   v1 = 12, v2 = 0,  vf = 0.35f,descTemplate = "香菱·锅巴 — {0}伤害，连携+{2}%" },
            },
            [6] = new List<EffectDef>
            {
                new EffectDef { type = EffectType.DamagePierce,      v1 = 25, v2 = 0,  vf = 0,    descTemplate = "达达利亚·断流 — {0}伤害，无视护盾" },
                new EffectDef { type = EffectType.DamageStun,        v1 = 20, v2 = 1,  vf = 0,    descTemplate = "莫娜·星命 — {0}伤害，禁锢{1}回" },
                new EffectDef { type = EffectType.HeavyDamage,       v1 = 28, v2 = 0,  vf = 0,    descTemplate = "刻晴·星斗 — {0}伤害" },
                new EffectDef { type = EffectType.ShieldPermATKLarge,v1 = 18, v2 = 1,  vf = 0,    descTemplate = "荒泷一斗·鬼铠 — {0}护盾，+{1}攻" },
                new EffectDef { type = EffectType.HealDamageBoost,   v1 = 20, v2 = 0,  vf = 0.5f, descTemplate = "白术·长生 — 恢复{0}，下回+{2}%" },
                new EffectDef { type = EffectType.MassiveHeal,       v1 = 25, v2 = 0,  vf = 0,    descTemplate = "瑶瑶·月桂 — 恢复{0}生命" },
                new EffectDef { type = EffectType.RefreshMultiplier, v1 = 1,  v2 = 0,  vf = 0.03f,descTemplate = "鹿野院平藏·心算 — 刷新+{1}，倍率+{2}%" },
                new EffectDef { type = EffectType.DamagePerStep,     v1 = 2,  v2 = 0,  vf = 0,    descTemplate = "赛诺·审判 — 步数×{0}伤害" },
                new EffectDef { type = EffectType.ShieldBurst,       v1 = 0,  v2 = 0,  vf = 1.5f, descTemplate = "坎蒂丝·守护 — 护盾爆破{2}%" },
            },
            [7] = new List<EffectDef>
            {
                new EffectDef { type = EffectType.DamagePermATK,      v1 = 50, v2 = 3,  vf = 0,    descTemplate = "雷电将军·无想一刀 — {0}伤害，+{1}攻" },
                new EffectDef { type = EffectType.DamageLifestealStrong,v1=35, v2 = 0,  vf = 0.5f, descTemplate = "八重神子·狐灵 — {0}伤害，吸血{2}%" },
                new EffectDef { type = EffectType.DamageStunStrong,   v1 = 40, v2 = 1,  vf = 0,    descTemplate = "甘雨·霜华 — {0}伤害，冰封{1}回" },
                new EffectDef { type = EffectType.MassiveDamage,      v1 = 45, v2 = 0,  vf = 0,    descTemplate = "迪卢克·天焰 — {0}伤害" },
                new EffectDef { type = EffectType.RefreshMultiplierStrong,v1=2,v2 = 0,vf = 0.05f,descTemplate = "纳西妲·慧眼 — 刷新+{0}，倍率+{2}%" },
                new EffectDef { type = EffectType.ExtraTurn,          v1 = 30, v2 = 0,  vf = 0,    descTemplate = "神里绫人·镜花 — {0}伤害，额外回合" },
                new EffectDef { type = EffectType.ShieldReflectStrong,v1 = 30, v2 = 0,  vf = 0.5f, descTemplate = "艾尔海森·镜闪 — {0}护盾，反弹{2}%" },
                new EffectDef { type = EffectType.HealPermATK,        v1 = 35, v2 = 2,  vf = 0,    descTemplate = "夜兰·幽玄 — 恢复{0}生命，+{1}攻" },
                new EffectDef { type = EffectType.Overkill,           v1 = 40, v2 = 3,  vf = 0,    descTemplate = "魈·靖妖 — {0}伤害，击杀+{1}攻" },
                new EffectDef { type = EffectType.LuckyHit,           v1 = 30, v2 = 0,  vf = 0.4f, descTemplate = "可莉·轰轰火花 — {0}伤害，{2}%三倍" },
            },
            [8] = new List<EffectDef>
            {
                new EffectDef { type = EffectType.TaskStepMultiplier, v1 = 0,  v2 = 0,  vf = 0.1f, descTemplate = "天理·维系者 — 步数倍率×1.1(累乘)" },
            },
        };

        /// <summary>
        /// Generate a random CardData for the given tower level.
        /// </summary>
        static Element RollElement(EffectType t) => t switch
        {
            EffectType.PureDamage or EffectType.MidDamage or EffectType.DamagePoison or EffectType.HeavyDamage
                => (Element)UnityEngine.Random.Range(0, 3), // Pyro/Cryo/Hydro
            EffectType.DamageDraw or EffectType.MidDamageDraw => Element.Anemo,
            EffectType.PureShield or EffectType.MidShield or EffectType.HeavyShield or EffectType.ShieldBurst => Element.Geo,
            EffectType.ShieldReflect or EffectType.ShieldReflectStrong => Element.Electro,
            EffectType.PureHeal or EffectType.MidHeal or EffectType.HeavyHeal or EffectType.MassiveHeal or EffectType.HealCleanse => Element.Hydro,
            EffectType.StepRecovery or EffectType.MidStepRecovery => Element.Anemo,
            EffectType.ComboChain or EffectType.MidComboChain or EffectType.HeavyComboChain => Element.Cryo,
            EffectType.DamagePierce or EffectType.DamageStun or EffectType.DamageStunStrong => Element.Electro,
            EffectType.HealDamageBoost or EffectType.HealPermATK => Element.Pyro,
            EffectType.DamageExecute or EffectType.DamageLifesteal or EffectType.DamageLifestealStrong => Element.Dendro,
            EffectType.DamagePermATK or EffectType.ShieldPermATKLarge => Element.Geo,
            EffectType.ExtraTurn or EffectType.RefreshMultiplier or EffectType.RefreshMultiplierStrong => Element.Omni,
            EffectType.Overkill or EffectType.MassiveDamage => Element.Pyro,
            EffectType.LuckyHit or EffectType.DamagePerStep => Element.Electro,
            _ => (Element)UnityEngine.Random.Range(0, 7),
        };

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
                element = RollElement(def.type),
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
