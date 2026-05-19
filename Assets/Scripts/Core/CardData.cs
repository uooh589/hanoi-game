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
                E(EffectType.PureDamage, 5, 0, 0f, Element.Pyro, "安柏·爆弹 — 造成{0}点火焰伤害", "造成{0}点火焰伤害"),
                E(EffectType.PureDamage, 5, 0, 0f, Element.Cryo, "凯亚·霜袭 — {0}伤害", "{0}伤害"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Cryo, "重云·冰爆 — {0}伤害，减速", "{0}伤害"),
                E(EffectType.PureShield, 7, 0, 0f, Element.Cryo, "迪奥娜·猫爪 — {0}护盾", "{0}护盾"),
                E(EffectType.PureHeal, 8, 0, 0f, Element.Hydro, "芭芭拉·歌声 — 恢复{0}生命", "恢复{0}生命"),
                E(EffectType.ComboChain, 3, 0, 0.15f, Element.Hydro, "行秋·裁雨 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.DamageDraw, 4, 1, 0f, Element.Hydro, "妮露·水月 — {0}伤害，抽{1}牌", "{0}伤害"),
                E(EffectType.PureDamage, 5, 0, 0f, Element.Electro, "丽莎·感电 — {0}伤害", "{0}伤害"),
                E(EffectType.ShieldReflect, 4, 0, 0.2f, Element.Electro, "北斗·捉浪 — {0}护盾，反弹{2}%", "{0}护盾"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Anemo, "砂糖·扩散 — {0}伤害，扩散", "{0}伤害"),
                E(EffectType.StepRecovery, 8, 0, 0f, Element.Anemo, "万叶·风引 — +{0}步", "+{0}步"),
                E(EffectType.PureShield, 8, 0, 0f, Element.Geo, "诺艾尔·护心 — {0}护盾", "{0}护盾"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Dendro, "柯莱·毒藤 — {0}伤害，中毒", "{0}伤害"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Pyro, "Pyro·基础攻击 — {0}伤害", "基本伤害"),
                E(EffectType.PureShield, 5, 0, 0f, Element.Pyro, "Pyro·基础护盾 — {0}护盾", "基本护盾"),
                E(EffectType.StepRecovery, 5, 0, 0f, Element.Pyro, "Pyro·基础步数 — +{0}步", "恢复步数"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Cryo, "Cryo·基础攻击 — {0}伤害", "基本伤害"),
                E(EffectType.PureShield, 5, 0, 0f, Element.Cryo, "Cryo·基础护盾 — {0}护盾", "基本护盾"),
                E(EffectType.StepRecovery, 5, 0, 0f, Element.Cryo, "Cryo·基础步数 — +{0}步", "恢复步数"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Hydro, "Hydro·基础攻击 — {0}伤害", "基本伤害"),
                E(EffectType.PureShield, 5, 0, 0f, Element.Hydro, "Hydro·基础护盾 — {0}护盾", "基本护盾"),
                E(EffectType.StepRecovery, 5, 0, 0f, Element.Hydro, "Hydro·基础步数 — +{0}步", "恢复步数"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Electro, "Electro·基础攻击 — {0}伤害", "基本伤害"),
                E(EffectType.PureShield, 5, 0, 0f, Element.Electro, "Electro·基础护盾 — {0}护盾", "基本护盾"),
                E(EffectType.StepRecovery, 5, 0, 0f, Element.Electro, "Electro·基础步数 — +{0}步", "恢复步数"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Anemo, "Anemo·基础攻击 — {0}伤害", "基本伤害"),
                E(EffectType.PureShield, 5, 0, 0f, Element.Anemo, "Anemo·基础护盾 — {0}护盾", "基本护盾"),
                E(EffectType.StepRecovery, 5, 0, 0f, Element.Anemo, "Anemo·基础步数 — +{0}步", "恢复步数"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Geo, "Geo·基础攻击 — {0}伤害", "基本伤害"),
                E(EffectType.PureShield, 5, 0, 0f, Element.Geo, "Geo·基础护盾 — {0}护盾", "基本护盾"),
                E(EffectType.StepRecovery, 5, 0, 0f, Element.Geo, "Geo·基础步数 — +{0}步", "恢复步数"),
                E(EffectType.PureDamage, 4, 0, 0f, Element.Dendro, "Dendro·基础攻击 — {0}伤害", "基本伤害"),
                E(EffectType.PureShield, 5, 0, 0f, Element.Dendro, "Dendro·基础护盾 — {0}护盾", "基本护盾"),
                E(EffectType.StepRecovery, 5, 0, 0f, Element.Dendro, "Dendro·基础步数 — +{0}步", "恢复步数"),
            },
            [4] = new() {
                E(EffectType.MidDamage, 14, 0, 0f, Element.Pyro, "迪卢克·天焰 — {0}伤害，自伤3", "{0}伤害"),
                E(EffectType.MidHealBoost, 8, 0, 0.2f, Element.Pyro, "班尼特·鼓舞 — 恢复{0}，下回+{2}%", "恢复{0}"),
                E(EffectType.MidShieldPermATK, 8, 2, 0f, Element.Pyro, "辛焱·摇滚 — {0}护盾，永久+{1}攻", "{0}护盾"),
                E(EffectType.MidComboChain, 7, 0, 0.25f, Element.Pyro, "烟绯·火印 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.MidDamageDraw, 7, 1, 0f, Element.Pyro, "宵宫·流火 — {0}伤害，抽{1}牌", "{0}伤害"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Pyro, "托马·炽盾 — {0}护盾", "{0}护盾"),
                E(EffectType.MidDamageWeaken, 6, 0, 0.3f, Element.Cryo, "罗莎莉亚·冰枪 — {0}伤害，虚弱{2}%", "{0}伤害"),
                E(EffectType.MidHeal, 12, 0, 0f, Element.Cryo, "七七·仙法 — 恢复{0}生命", "恢复{0}生命"),
                E(EffectType.MidComboChain, 7, 0, 0.3f, Element.Cryo, "申鹤·冰翎 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.MidDamage, 10, 0, 0f, Element.Cryo, "优菈·冰潮 — {0}伤害", "{0}伤害"),
                E(EffectType.MidStepRecovery, 8, 0, 0f, Element.Cryo, "米卡·霜矢 — +{0}步", "+{0}步"),
                E(EffectType.MidHealBoost, 14, 0, 0.3f, Element.Cryo, "迪奥娜·核心·特调 — 恢复{0}，下回+{2}%", "恢复{0}"),
                E(EffectType.MidDamageWeaken, 6, 0, 0.3f, Element.Hydro, "莫娜·泡影 — {0}伤害，虚弱{2}%", "{0}伤害"),
                E(EffectType.MidHeal, 12, 0, 0f, Element.Hydro, "心海·水母 — 恢复{0}生命", "恢复{0}生命"),
                E(EffectType.MidDamageDraw, 7, 1, 0f, Element.Hydro, "绫人·水镜 — {0}伤害，抽{1}牌", "{0}伤害"),
                E(EffectType.MidComboChain, 7, 0, 0.3f, Element.Hydro, "夜兰·幽玄 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.MidHealBoost, 10, 0, 0.25f, Element.Hydro, "芙宁娜·孤心 — 恢复{0}，下回+{2}%", "恢复{0}"),
                E(EffectType.MidDamage, 10, 0, 0f, Element.Electro, "菲谢尔·夜巡 — {0}伤害", "{0}伤害"),
                E(EffectType.MidDamage, 9, 0, 0f, Element.Electro, "雷泽·狼爪 — {0}伤害×2段", "{0}伤害×2段"),
                E(EffectType.MidDamageWeaken, 7, 0, 0.25f, Element.Electro, "九条·天狗 — {0}伤害，虚弱{2}%", "{0}伤害"),
                E(EffectType.MidHealBoost, 8, 0, 0.2f, Element.Electro, "久岐忍·雷疗 — 恢复{0}，下回+{2}%", "恢复{0}"),
                E(EffectType.MidStepRecovery, 12, 0, 0f, Element.Anemo, "早柚·疾风 — +{0}步", "+{0}步"),
                E(EffectType.MidHealBoost, 8, 0, 0.2f, Element.Anemo, "琴·蒲公英 — 恢复{0}，下回+{2}%", "恢复{0}"),
                E(EffectType.MidDamage, 10, 0, 0f, Element.Anemo, "魈·风轮 — {0}伤害", "{0}伤害"),
                E(EffectType.MidComboChain, 7, 0, 0.3f, Element.Anemo, "流浪者·狂岚 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.MidDamageDraw, 7, 1, 0f, Element.Anemo, "琳妮特·暗风 — {0}伤害，抽{1}牌", "{0}伤害"),
                E(EffectType.MidDamageWeaken, 6, 0, 0.3f, Element.Anemo, "珐露珊·风暴 — {0}伤害，虚弱{2}%", "{0}伤害"),
                E(EffectType.MidShieldPermATK, 8, 1, 0f, Element.Geo, "凝光·璇玑 — {0}护盾，+{1}攻", "{0}护盾"),
                E(EffectType.MidDamage, 9, 0, 0f, Element.Geo, "荒泷一斗·鬼角 — {0}伤害", "{0}伤害"),
                E(EffectType.MidShield, 12, 0, 0f, Element.Geo, "阿贝多·创生 — {0}护盾，荆棘", "{0}护盾"),
                E(EffectType.MidComboChain, 7, 0, 0.25f, Element.Geo, "云堇·旗印 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.MidDamageDraw, 7, 1, 0f, Element.Geo, "五郎·兽牙 — {0}伤害，抽{1}牌", "{0}伤害"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Geo, "卡维·筑梦 — 恢复{0}生命", "恢复{0}生命"),
                E(EffectType.MidDamageDraw, 7, 1, 0f, Element.Dendro, "提纳里·花箭 — {0}伤害，抽{1}牌", "{0}伤害"),
                E(EffectType.MidHeal, 12, 0, 0f, Element.Dendro, "白术·长生 — 恢复{0}生命", "恢复{0}生命"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Dendro, "瑶瑶·月桂 — 恢复{0}生命", "恢复{0}生命"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Dendro, "卡维·筑绿 — {0}护盾", "{0}护盾"),
                E(EffectType.MidStepRecovery, 8, 0, 0f, Element.Dendro, "绮良良·猫又 — +{0}步", "+{0}步"),
                E(EffectType.MidHealBoost, 8, 0, 0.2f, Element.Dendro, "多莉·菌灯 — 恢复{0}，下回+{2}%", "恢复{0}"),
                E(EffectType.MidDamageWeaken, 6, 0, 0.25f, Element.Dendro, "赛索斯·沙棘 — {0}伤害，虚弱{2}%", "{0}伤害"),
                E(EffectType.MidDamage, 8, 0, 0f, Element.Pyro, "Pyro·进阶攻击 — {0}伤害", "进阶伤害"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Pyro, "Pyro·进阶护盾 — {0}护盾", "进阶护盾"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Pyro, "Pyro·进阶治疗 — 恢复{0}生命", "进阶治疗"),
                E(EffectType.MidComboChain, 6, 0, 0.2f, Element.Pyro, "Pyro·进阶连携 — {0}伤害，连携+{2}%", "进阶连携"),
                E(EffectType.MidDamage, 8, 0, 0f, Element.Cryo, "Cryo·进阶攻击 — {0}伤害", "进阶伤害"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Cryo, "Cryo·进阶护盾 — {0}护盾", "进阶护盾"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Cryo, "Cryo·进阶治疗 — 恢复{0}生命", "进阶治疗"),
                E(EffectType.MidComboChain, 6, 0, 0.2f, Element.Cryo, "Cryo·进阶连携 — {0}伤害，连携+{2}%", "进阶连携"),
                E(EffectType.MidDamage, 8, 0, 0f, Element.Hydro, "Hydro·进阶攻击 — {0}伤害", "进阶伤害"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Hydro, "Hydro·进阶护盾 — {0}护盾", "进阶护盾"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Hydro, "Hydro·进阶治疗 — 恢复{0}生命", "进阶治疗"),
                E(EffectType.MidComboChain, 6, 0, 0.2f, Element.Hydro, "Hydro·进阶连携 — {0}伤害，连携+{2}%", "进阶连携"),
                E(EffectType.MidDamage, 8, 0, 0f, Element.Electro, "Electro·进阶攻击 — {0}伤害", "进阶伤害"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Electro, "Electro·进阶护盾 — {0}护盾", "进阶护盾"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Electro, "Electro·进阶治疗 — 恢复{0}生命", "进阶治疗"),
                E(EffectType.MidComboChain, 6, 0, 0.2f, Element.Electro, "Electro·进阶连携 — {0}伤害，连携+{2}%", "进阶连携"),
                E(EffectType.MidDamage, 8, 0, 0f, Element.Anemo, "Anemo·进阶攻击 — {0}伤害", "进阶伤害"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Anemo, "Anemo·进阶护盾 — {0}护盾", "进阶护盾"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Anemo, "Anemo·进阶治疗 — 恢复{0}生命", "进阶治疗"),
                E(EffectType.MidComboChain, 6, 0, 0.2f, Element.Anemo, "Anemo·进阶连携 — {0}伤害，连携+{2}%", "进阶连携"),
                E(EffectType.MidDamage, 8, 0, 0f, Element.Geo, "Geo·进阶攻击 — {0}伤害", "进阶伤害"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Geo, "Geo·进阶护盾 — {0}护盾", "进阶护盾"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Geo, "Geo·进阶治疗 — 恢复{0}生命", "进阶治疗"),
                E(EffectType.MidComboChain, 6, 0, 0.2f, Element.Geo, "Geo·进阶连携 — {0}伤害，连携+{2}%", "进阶连携"),
                E(EffectType.MidDamage, 8, 0, 0f, Element.Dendro, "Dendro·进阶攻击 — {0}伤害", "进阶伤害"),
                E(EffectType.MidShield, 10, 0, 0f, Element.Dendro, "Dendro·进阶护盾 — {0}护盾", "进阶护盾"),
                E(EffectType.MidHeal, 10, 0, 0f, Element.Dendro, "Dendro·进阶治疗 — 恢复{0}生命", "进阶治疗"),
                E(EffectType.MidComboChain, 6, 0, 0.2f, Element.Dendro, "Dendro·进阶连携 — {0}伤害，连携+{2}%", "进阶连携"),
            },
            [5] = new() {
                E(EffectType.DamagePoison, 10, 2, 2f, Element.Pyro, "香菱·锅巴 — {0}伤害，灼烧{1}回", "{0}伤害"),
                E(EffectType.DamageExecute, 12, 0, 0f, Element.Pyro, "胡桃·蝶引 — {0}伤害，半血翻倍", "{0}伤害"),
                E(EffectType.HeavyComboChain, 12, 0, 0.4f, Element.Pyro, "香菱·核心·旋火轮 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.HeavyShield, 15, 0, 0f, Element.Cryo, "莱依拉·星盾 — {0}护盾", "{0}护盾"),
                E(EffectType.HeavyComboChain, 12, 0, 0.35f, Element.Cryo, "申鹤·核心·神女 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.HeavyComboChain, 12, 0, 0.35f, Element.Anemo, "温迪·神风 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.ShieldBonusSteps, 10, 8, 0f, Element.Geo, "钟离·岩脊 — {0}护盾，+{1}步", "{0}护盾"),
                E(EffectType.HeavyShield, 15, 0, 0f, Element.Geo, "千织·织岩 — {0}护盾", "{0}护盾"),
                E(EffectType.HeavyComboChain, 12, 0, 0.4f, Element.Geo, "钟离·核心·天星 — {0}伤害，连携+{2}%", "{0}伤害"),
                E(EffectType.HeavyDamage, 15, 0, 0f, Element.Pyro, "Pyro·强力攻击 — {0}伤害", "强力伤害"),
                E(EffectType.HeavyShield, 13, 0, 0f, Element.Pyro, "Pyro·强力护盾 — {0}护盾", "强力护盾"),
                E(EffectType.DamageLifesteal, 10, 0, 0.3f, Element.Pyro, "Pyro·吸血 — {0}伤害，吸血{2}%", "吸血攻击"),
                E(EffectType.HeavyDamage, 15, 0, 0f, Element.Cryo, "Cryo·强力攻击 — {0}伤害", "强力伤害"),
                E(EffectType.HeavyShield, 13, 0, 0f, Element.Cryo, "Cryo·强力护盾 — {0}护盾", "强力护盾"),
                E(EffectType.DamageLifesteal, 10, 0, 0.3f, Element.Cryo, "Cryo·吸血 — {0}伤害，吸血{2}%", "吸血攻击"),
                E(EffectType.HeavyDamage, 15, 0, 0f, Element.Hydro, "Hydro·强力攻击 — {0}伤害", "强力伤害"),
                E(EffectType.HeavyShield, 13, 0, 0f, Element.Hydro, "Hydro·强力护盾 — {0}护盾", "强力护盾"),
                E(EffectType.DamageLifesteal, 10, 0, 0.3f, Element.Hydro, "Hydro·吸血 — {0}伤害，吸血{2}%", "吸血攻击"),
                E(EffectType.HeavyDamage, 15, 0, 0f, Element.Electro, "Electro·强力攻击 — {0}伤害", "强力伤害"),
                E(EffectType.HeavyShield, 13, 0, 0f, Element.Electro, "Electro·强力护盾 — {0}护盾", "强力护盾"),
                E(EffectType.DamageLifesteal, 10, 0, 0.3f, Element.Electro, "Electro·吸血 — {0}伤害，吸血{2}%", "吸血攻击"),
                E(EffectType.HeavyDamage, 15, 0, 0f, Element.Anemo, "Anemo·强力攻击 — {0}伤害", "强力伤害"),
                E(EffectType.HeavyShield, 13, 0, 0f, Element.Anemo, "Anemo·强力护盾 — {0}护盾", "强力护盾"),
                E(EffectType.DamageLifesteal, 10, 0, 0.3f, Element.Anemo, "Anemo·吸血 — {0}伤害，吸血{2}%", "吸血攻击"),
                E(EffectType.HeavyDamage, 15, 0, 0f, Element.Geo, "Geo·强力攻击 — {0}伤害", "强力伤害"),
                E(EffectType.HeavyShield, 13, 0, 0f, Element.Geo, "Geo·强力护盾 — {0}护盾", "强力护盾"),
                E(EffectType.DamageLifesteal, 10, 0, 0.3f, Element.Geo, "Geo·吸血 — {0}伤害，吸血{2}%", "吸血攻击"),
                E(EffectType.HeavyDamage, 15, 0, 0f, Element.Dendro, "Dendro·强力攻击 — {0}伤害", "强力伤害"),
                E(EffectType.HeavyShield, 13, 0, 0f, Element.Dendro, "Dendro·强力护盾 — {0}护盾", "强力护盾"),
                E(EffectType.DamageLifesteal, 10, 0, 0.3f, Element.Dendro, "Dendro·吸血 — {0}伤害，吸血{2}%", "吸血攻击"),
            },
            [6] = new() {
                E(EffectType.HealDamageBoost, 20, 0, 0.5f, Element.Pyro, "班尼特·核心·美妙旅程 — 恢复{0}，下回+{2}%", "恢复{0}"),
                E(EffectType.DamageStun, 20, 1, 0f, Element.Cryo, "甘雨·霜华 — {0}伤害，冰冻{1}回", "{0}伤害"),
                E(EffectType.DamagePierce, 25, 0, 0f, Element.Hydro, "达达利亚·断流 — {0}伤害，无视护盾", "{0}伤害"),
                E(EffectType.HeavyDamage, 28, 0, 0f, Element.Hydro, "那维莱特·潮涌 — {0}伤害", "{0}伤害"),
                E(EffectType.MassiveHeal, 30, 0, 0f, Element.Hydro, "心海·核心·海月 — 恢复{0}生命", "恢复{0}生命"),
                E(EffectType.DamagePerStep, 3, 0, 0f, Element.Electro, "赛诺·审判 — 步数×{0}伤害", "步数×{0}伤害"),
                E(EffectType.HeavyDamage, 28, 0, 0f, Element.Electro, "刻晴·星斗 — {0}伤害", "{0}伤害"),
                E(EffectType.DamagePierce, 25, 0, 0f, Element.Electro, "菲谢尔·核心·夜鸦 — {0}伤害，无视护盾", "{0}伤害"),
                E(EffectType.RefreshMultiplier, 1, 0, 0.03f, Element.Anemo, "鹿野院平藏·心算 — +10步，倍率+{2}%", "+10步"),
                E(EffectType.HeavyDamage, 28, 0, 0f, Element.Anemo, "流浪者·核心·空居 — {0}伤害，无视护盾", "{0}伤害"),
                E(EffectType.MassiveHeal, 25, 0, 0f, Element.Anemo, "琴·核心·狮牙 — 恢复{0}生命，清除负面", "恢复{0}生命"),
                E(EffectType.ShieldBurst, 0, 0, 1.5f, Element.Geo, "娜维娅·晶炮 — 护盾×{2}%伤害", "护盾×{2}%伤害"),
                E(EffectType.RefreshMultiplier, 1, 0, 0.03f, Element.Dendro, "纳西妲·慧眼 — +10步，倍率+{2}%", "+10步"),
                E(EffectType.MassiveHeal, 25, 0, 0f, Element.Dendro, "白术·核心·愈气 — 恢复{0}生命", "恢复{0}生命"),
                E(EffectType.HeavyDamage, 22, 0, 0f, Element.Pyro, "Pyro·史诗攻击 — {0}伤害", "史诗伤害"),
                E(EffectType.MassiveHeal, 20, 0, 0f, Element.Pyro, "Pyro·史诗治疗 — 恢复{0}生命", "史诗治疗"),
                E(EffectType.HeavyDamage, 22, 0, 0f, Element.Cryo, "Cryo·史诗攻击 — {0}伤害", "史诗伤害"),
                E(EffectType.MassiveHeal, 20, 0, 0f, Element.Cryo, "Cryo·史诗治疗 — 恢复{0}生命", "史诗治疗"),
                E(EffectType.HeavyDamage, 22, 0, 0f, Element.Hydro, "Hydro·史诗攻击 — {0}伤害", "史诗伤害"),
                E(EffectType.MassiveHeal, 20, 0, 0f, Element.Hydro, "Hydro·史诗治疗 — 恢复{0}生命", "史诗治疗"),
                E(EffectType.HeavyDamage, 22, 0, 0f, Element.Electro, "Electro·史诗攻击 — {0}伤害", "史诗伤害"),
                E(EffectType.MassiveHeal, 20, 0, 0f, Element.Electro, "Electro·史诗治疗 — 恢复{0}生命", "史诗治疗"),
                E(EffectType.HeavyDamage, 22, 0, 0f, Element.Anemo, "Anemo·史诗攻击 — {0}伤害", "史诗伤害"),
                E(EffectType.MassiveHeal, 20, 0, 0f, Element.Anemo, "Anemo·史诗治疗 — 恢复{0}生命", "史诗治疗"),
                E(EffectType.HeavyDamage, 22, 0, 0f, Element.Geo, "Geo·史诗攻击 — {0}伤害", "史诗伤害"),
                E(EffectType.MassiveHeal, 20, 0, 0f, Element.Geo, "Geo·史诗治疗 — 恢复{0}生命", "史诗治疗"),
                E(EffectType.HeavyDamage, 22, 0, 0f, Element.Dendro, "Dendro·史诗攻击 — {0}伤害", "史诗伤害"),
                E(EffectType.MassiveHeal, 20, 0, 0f, Element.Dendro, "Dendro·史诗治疗 — 恢复{0}生命", "史诗治疗"),
            },
            [7] = new() {
                E(EffectType.LuckyHit, 8, 0, 0.3f, Element.Pyro, "可莉·火花 — {0}伤害，{2}%三倍", "{0}伤害"),
                E(EffectType.MassiveDamage, 45, 0, 0f, Element.Pyro, "迪卢克·核心·黎明 — {0}伤害", "{0}伤害"),
                E(EffectType.Overkill, 35, 3, 0f, Element.Pyro, "胡桃·核心·血蝶 — {0}伤害，击杀+{1}攻", "{0}伤害"),
                E(EffectType.LuckyHit, 30, 0, 0.5f, Element.Pyro, "可莉·核心·轰轰火花 — {0}伤害，{2}%三倍", "{0}伤害"),
                E(EffectType.DamageStunStrong, 40, 1, 0f, Element.Cryo, "甘雨·核心·霜华矢 — {0}伤害，冰封{1}回", "{0}伤害"),
                E(EffectType.DamageLifestealStrong, 35, 0, 0.5f, Element.Cryo, "绫华·核心·冰华 — {0}伤害，吸血{2}%", "{0}伤害"),
                E(EffectType.ShieldReflectStrong, 30, 0, 0.5f, Element.Cryo, "莱依拉·核心·星流 — {0}护盾，反弹{2}%", "{0}护盾"),
                E(EffectType.DamagePermATK, 50, 3, 0f, Element.Hydro, "达达利亚·核心·魔王武装 — {0}伤害，+{1}攻", "{0}伤害"),
                E(EffectType.HealPermATK, 35, 2, 0f, Element.Hydro, "夜兰·核心·渊图 — 恢复{0}，+{1}攻", "恢复{0}"),
                E(EffectType.ExtraTurn, 30, 0, 0f, Element.Hydro, "绫人·核心·镜花 — {0}伤害，额外回合", "{0}伤害"),
                E(EffectType.RefreshMultiplierStrong, 20, 0, 0.05f, Element.Hydro, "芙宁娜·核心·万众狂欢 — +{0}步，倍率+{2}%", "+{0}步"),
                E(EffectType.DamagePermATK, 50, 3, 0f, Element.Electro, "雷电将军·无想 — {0}伤害，+{1}攻", "{0}伤害"),
                E(EffectType.DamageLifestealStrong, 35, 0, 0.5f, Element.Electro, "八重神子·狐灵 — {0}伤害，吸血{2}%", "{0}伤害"),
                E(EffectType.MassiveDamage, 45, 0, 0f, Element.Electro, "雷电将军·核心·无想一刀 — {0}伤害", "{0}伤害"),
                E(EffectType.LuckyHit, 30, 0, 0.5f, Element.Electro, "八重神子·核心·天狐 — {0}伤害，{2}%三倍", "{0}伤害"),
                E(EffectType.ExtraTurn, 30, 0, 0f, Element.Electro, "刻晴·核心·星斗归位 — {0}伤害，额外回合", "{0}伤害"),
                E(EffectType.Overkill, 40, 3, 0f, Element.Electro, "赛诺·核心·启途 — {0}伤害，击杀+{1}攻", "{0}伤害"),
                E(EffectType.Overkill, 40, 3, 0f, Element.Anemo, "魈·核心·靖妖傩舞 — {0}伤害，击杀+{1}攻", "{0}伤害"),
                E(EffectType.RefreshMultiplierStrong, 20, 0, 0.05f, Element.Anemo, "万叶·核心·千早振 — +{0}步，倍率+{2}%", "+{0}步"),
                E(EffectType.ExtraTurn, 30, 0, 0f, Element.Anemo, "温迪·核心·风神之诗 — {0}伤害，额外回合", "{0}伤害"),
                E(EffectType.DamagePermATK, 50, 3, 0f, Element.Geo, "荒泷一斗·核心·鬼王 — {0}伤害，+{1}攻", "{0}伤害"),
                E(EffectType.ShieldReflectStrong, 30, 0, 0.5f, Element.Geo, "阿贝多·核心·阳华 — {0}护盾，反弹{2}%", "{0}护盾"),
                E(EffectType.MassiveDamage, 45, 0, 0f, Element.Geo, "娜维娅·核心·伞弹 — {0}伤害", "{0}伤害"),
                E(EffectType.HealPermATK, 35, 2, 0f, Element.Geo, "诺艾尔·核心·大扫除 — 恢复{0}，+{1}攻", "恢复{0}"),
                E(EffectType.ShieldReflectStrong, 30, 0, 0.5f, Element.Dendro, "艾尔海森·镜闪 — {0}护盾，反弹{2}%", "{0}护盾"),
                E(EffectType.RefreshMultiplierStrong, 20, 0, 0.05f, Element.Dendro, "纳西妲·核心·心景 — +{0}步，倍率+{2}%", "+{0}步"),
                E(EffectType.DamageLifestealStrong, 35, 0, 0.5f, Element.Dendro, "艾尔海森·核心·裁叶 — {0}伤害，吸血{2}%", "{0}伤害"),
                E(EffectType.DamageStunStrong, 40, 1, 0f, Element.Dendro, "提纳里·核心·缠藤 — {0}伤害，缠绕{1}回", "{0}伤害"),
                E(EffectType.HealPermATK, 35, 2, 0f, Element.Dendro, "瑶瑶·核心·桂子 — 恢复{0}，+{1}攻", "恢复{0}"),
                E(EffectType.MassiveDamage, 38, 0, 0f, Element.Pyro, "Pyro·传说攻击 — {0}伤害", "传说伤害"),
                E(EffectType.DamagePermATK, 42, 2, 0f, Element.Pyro, "Pyro·传说突破 — {0}伤害，+{1}攻", "传说突破"),
                E(EffectType.MassiveDamage, 38, 0, 0f, Element.Cryo, "Cryo·传说攻击 — {0}伤害", "传说伤害"),
                E(EffectType.DamagePermATK, 42, 2, 0f, Element.Cryo, "Cryo·传说突破 — {0}伤害，+{1}攻", "传说突破"),
                E(EffectType.MassiveDamage, 38, 0, 0f, Element.Hydro, "Hydro·传说攻击 — {0}伤害", "传说伤害"),
                E(EffectType.DamagePermATK, 42, 2, 0f, Element.Hydro, "Hydro·传说突破 — {0}伤害，+{1}攻", "传说突破"),
                E(EffectType.MassiveDamage, 38, 0, 0f, Element.Electro, "Electro·传说攻击 — {0}伤害", "传说伤害"),
                E(EffectType.DamagePermATK, 42, 2, 0f, Element.Electro, "Electro·传说突破 — {0}伤害，+{1}攻", "传说突破"),
                E(EffectType.MassiveDamage, 38, 0, 0f, Element.Anemo, "Anemo·传说攻击 — {0}伤害", "传说伤害"),
                E(EffectType.DamagePermATK, 42, 2, 0f, Element.Anemo, "Anemo·传说突破 — {0}伤害，+{1}攻", "传说突破"),
                E(EffectType.MassiveDamage, 38, 0, 0f, Element.Geo, "Geo·传说攻击 — {0}伤害", "传说伤害"),
                E(EffectType.DamagePermATK, 42, 2, 0f, Element.Geo, "Geo·传说突破 — {0}伤害，+{1}攻", "传说突破"),
                E(EffectType.MassiveDamage, 38, 0, 0f, Element.Dendro, "Dendro·传说攻击 — {0}伤害", "传说伤害"),
                E(EffectType.DamagePermATK, 42, 2, 0f, Element.Dendro, "Dendro·传说突破 — {0}伤害，+{1}攻", "传说突破"),
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
