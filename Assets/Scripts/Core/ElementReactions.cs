using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    public enum Element { Pyro, Cryo, Hydro, Electro, Anemo, Geo, Dendro, Omni }

    public static class ElementReactions
    {
        // Track last completed element for reaction chaining
        public static Element? LastElement;

        public static readonly Dictionary<(Element, Element), (string name, string desc)> Reactions = new()
        {
            // Amplifying
            { (Element.Pyro, Element.Cryo), ("融化", "伤害倍率×2.0") },
            { (Element.Cryo, Element.Pyro), ("融化", "伤害倍率×1.5") },
            { (Element.Pyro, Element.Hydro), ("蒸发", "伤害倍率×1.5") },
            { (Element.Hydro, Element.Pyro), ("蒸发", "伤害倍率×2.0") },

            // Transformative
            { (Element.Pyro, Element.Electro), ("超载", "额外范围伤害+敌人弱化") },
            { (Element.Electro, Element.Pyro), ("超载", "额外范围伤害+敌人弱化") },
            { (Element.Cryo, Element.Electro), ("超导", "范围冰伤+减防") },
            { (Element.Electro, Element.Cryo), ("超导", "范围冰伤+减防") },
            { (Element.Cryo, Element.Hydro), ("冻结", "冻结敌人，跳过1回合") },
            { (Element.Hydro, Element.Cryo), ("冻结", "冻结敌人，跳过1回合") },

            // Swirl and Crystallize
            { (Element.Anemo, Element.Pyro), ("扩散·火", "附加灼烧3回合") },
            { (Element.Anemo, Element.Cryo), ("扩散·冰", "步数恢复+3") },
            { (Element.Anemo, Element.Hydro), ("扩散·水", "治疗10%最大生命") },
            { (Element.Anemo, Element.Electro), ("扩散·雷", "额外抽1张牌") },

            // Dendro reactions
            { (Element.Pyro, Element.Dendro), ("燃烧", "持续灼烧5回合") },
            { (Element.Dendro, Element.Pyro), ("燃烧", "持续灼烧5回合") },
            { (Element.Electro, Element.Dendro), ("激化", "本次伤害+50%攻击力") },
            { (Element.Dendro, Element.Electro), ("激化", "本次伤害+50%攻击力") },
            { (Element.Hydro, Element.Dendro), ("绽放", "生成草核，下回合爆炸") },
            { (Element.Dendro, Element.Hydro), ("绽放", "生成草核，下回合爆炸") },

            // Geo
            { (Element.Geo, Element.Pyro), ("结晶·火", "获得火元素护盾") },
            { (Element.Geo, Element.Cryo), ("结晶·冰", "获得冰元素护盾") },
            { (Element.Geo, Element.Hydro), ("结晶·水", "获得水元素护盾") },
            { (Element.Geo, Element.Electro), ("结晶·雷", "获得雷元素护盾") },
            { (Element.Geo, Element.Dendro), ("结晶·草", "获得草元素护盾") },
        };

        public static (string name, float damageMult, int bonusDmg, int shield, int heal, int stun, int burn, int extraSteps, int extraDraw, bool bloom)
            TryReact(Element current, BattleManager battle)
        {
            if (LastElement == null) { LastElement = current; return default; }

            var key = (LastElement.Value, current);
            if (!Reactions.TryGetValue(key, out var reaction))
            {
                LastElement = current;
                return default;
            }

            LastElement = null; // consume reaction
            var (name, _) = reaction;
            float dmgMult = 1f;
            int bonusDmg = 0, shield = 0, heal = 0, stun = 0, burn = 0, extraSteps = 0, extraDraw = 0;
            bool bloom = false;

            switch (name)
            {
                case "融化":
                    dmgMult = (key.Item1 == Element.Pyro) ? 2.0f : 1.5f;
                    battle.AddBattleLog($"元素反应：【融化】伤害倍率×{dmgMult:F1}！");
                    break;
                case "蒸发":
                    dmgMult = (key.Item1 == Element.Hydro) ? 2.0f : 1.5f;
                    battle.AddBattleLog($"元素反应：【蒸发】伤害倍率×{dmgMult:F1}！");
                    break;
                case "超载":
                    int atk = battle.baseATK + GameManager.Instance.permanentATKBonus;
                    bonusDmg = Mathf.CeilToInt(atk * 0.8f);
                    battle.enemyWeakenTurns = 1;
                    battle.enemyWeakenPercent = 0.15f;
                    battle.AddBattleLog($"元素反应：【超载】额外{bonusDmg}范围伤害，敌人弱化1回合！");
                    break;
                case "超导":
                    bonusDmg = Mathf.CeilToInt((battle.baseATK + GameManager.Instance.permanentATKBonus) * 0.5f);
                    battle.enemyWeakenTurns = 2;
                    battle.enemyWeakenPercent = 0.25f;
                    battle.AddBattleLog($"元素反应：【超导】{bonusDmg}冰伤，敌人防御降低25%！");
                    break;
                case "冻结":
                    stun = 1;
                    battle.AddBattleLog($"元素反应：【冻结】敌人被冻结，跳过下一回合！");
                    break;
                case "扩散·火": burn = 3; battle.AddBattleLog($"元素反应：【扩散·火】附加灼烧3回合！"); break;
                case "扩散·冰": extraSteps = 3; battle.AddBattleLog($"元素反应：【扩散·冰】恢复3步！"); break;
                case "扩散·水": heal = Mathf.CeilToInt((battle.playerMaxHP + GameManager.Instance.maxHPBonus) * 0.1f); battle.AddBattleLog($"元素反应：【扩散·水】治疗{heal}点生命！"); break;
                case "扩散·雷": extraDraw = 1; battle.AddBattleLog($"元素反应：【扩散·雷】抽1张牌！"); break;
                case "燃烧": burn = 5; battle.AddBattleLog($"元素反应：【燃烧】持续灼烧5回合！"); break;
                case "激化": dmgMult = 1.5f; battle.AddBattleLog($"元素反应：【激化】伤害倍率×1.5！"); break;
                case "绽放": bloom = true; battle.AddBattleLog($"元素反应：【绽放】生成草核，下回合爆炸造成范围伤害！"); break;
                default:
                    if (name.StartsWith("结晶")) { shield = 20 + battle.baseATK * 2; battle.AddBattleLog($"元素反应：【{name}】获得{shield}点护盾！"); }
                    break;
            }

            return (name, dmgMult, bonusDmg, shield, heal, stun, burn, extraSteps, extraDraw, bloom);
        }

        public static void Reset() => LastElement = null;

        // Map disk size to element (8 sizes = 8 elements)
        public static Element FromDiskSize(int size) => size switch
        {
            1 => Element.Pyro,
            2 => Element.Cryo,
            3 => Element.Hydro,
            4 => Element.Electro,
            5 => Element.Anemo,
            6 => Element.Geo,
            7 => Element.Dendro,
            _ => Element.Omni,
        };
    }
}
