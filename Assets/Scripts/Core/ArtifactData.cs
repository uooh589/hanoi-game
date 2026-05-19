using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    [System.Serializable]
    public class ArtifactData
    {
        public string name;
        public string description;
        public string effectCode; // identifies the effect in code
        public int tier; // 1-5 star
        public string iconName;

        // Current held artifacts
        public static List<ArtifactData> Inventory = new();

        // ── Effect application ──
        public static float GetDamageMultiplier(Element? elem)
        {
            float mult = 1f;
            if (Has("magician")) mult *= 1.3f; // 乐团
            if (Has("witch") && elem == Element.Pyro) mult *= 1.3f;
            if (Has("blizzard") && elem == Element.Cryo) mult *= 1.2f;
            return mult;
        }

        public static int GetBonusDamage(Element? elem)
        {
            int bonus = 0;
            if (Has("witch") && elem == Element.Pyro) bonus += 2;
            if (Has("thunder") && elem == Element.Electro) bonus += 2;
            return bonus;
        }

        public static float GetFatigueReduction()
        {
            if (Has("fate")) return 0.5f; // 绝缘 -50% fatigue
            if (Has("gladiator")) return 1f; // 角斗士 -100% fatigue
            return 0f;
        }

        public static int GetExtraShield()
        {
            if (Has("archaic")) return 5; // 磐岩
            return 0;
        }

        public static int GetExtraSteps()
        {
            if (Has("viridescent")) return 3; // 翠绿
            return 0;
        }

        public static int GetExtraStun()
        {
            if (Has("thunder")) return 1; // 如雷
            return 0;
        }

        public static int GetExtraPoison()
        {
            if (Has("deepwood")) return 1; // 深林
            return 0;
        }

        public static float GetHealMultiplier()
        {
            float mult = 1f;
            if (Has("heart")) mult *= 1.5f; // 沉沦
            if (Has("paradise")) mult *= 2f; // 乐园 (below 30% HP)
            return mult;
        }

        public static float GetFirstCardMultiplier()
        {
            return Has("shimenawa") ? 1.5f : 1f; // 追忆
        }

        public static bool HasDoubleEffectChance()
        {
            return Has("echo") && Random.value < 0.3f; // 来歆 30%
        }

        public static int GetSelfDamageATKBonus()
        {
            return Has("vermillion") ? 1 : 0; // 辰砂
        }

        public static float GetBloomMultiplier()
        {
            return Has("paradise_lost") ? 2f : 1f; // 乐园之花
        }

        static bool Has(string code) => Inventory.Exists(a => a.effectCode == code);

        // ── Pool ──
        public static readonly List<ArtifactData> Pool = new()
        {
            // 5★ legendary
            new(){name="角斗士的终幕礼",description="完全免疫疲劳效果",effectCode="gladiator",tier=5,iconName="arti_gladiator"},
            new(){name="流浪大地的乐团",description="所有元素伤害+30%",effectCode="magician",tier=5,iconName="arti_magician"},
            new(){name="绝缘之旗印",description="疲劳惩罚减半",effectCode="fate",tier=5,iconName="arti_fate"},
            new(){name="辰砂往生录",description="每次自伤永久攻击力+1",effectCode="vermillion",tier=5,iconName="arti_vermillion"},

            // 4★ epic
            new(){name="炽烈的炎之魔女",description="火元素卡伤害+2,火伤+30%",effectCode="witch",tier=4,iconName="arti_witch"},
            new(){name="冰风迷途的勇士",description="冰元素卡伤害+20%",effectCode="blizzard",tier=4,iconName="arti_blizzard"},
            new(){name="沉沦之心",description="水元素治疗+50%",effectCode="heart",tier=4,iconName="arti_heart"},
            new(){name="如雷的盛怒",description="雷元素卡伤害+2,眩晕+1回",effectCode="thunder",tier=4,iconName="arti_thunder"},
            new(){name="翠绿之影",description="风元素卡额外+3步",effectCode="viridescent",tier=4,iconName="arti_viridescent"},
            new(){name="悠古的磐岩",description="岩元素护盾+5",effectCode="archaic",tier=4,iconName="arti_archaic"},
            new(){name="深林的记忆",description="草元素中毒+1回合",effectCode="deepwood",tier=4,iconName="arti_deepwood"},
            new(){name="追忆之注连",description="每回合第一张卡×1.5伤害",effectCode="shimenawa",tier=4,iconName="arti_shimenawa"},
            new(){name="来歆的余响",description="30%概率效果触发2次",effectCode="echo",tier=4,iconName="arti_echo"},
            new(){name="乐园遗落之花",description="绽放草核伤害×2",effectCode="paradise_lost",tier=4,iconName="arti_paradise"},
        };

        public static ArtifactData RollRandom()
        {
            float r = Random.value;
            if (r < 0.15f && Pool.Count > 0) // 15% legendary
                return Pool[Random.Range(0, 4)]; // first 4 are legendary
            return Pool[Random.Range(0, Pool.Count)]; // any
        }

        public static bool CanAddMore => Inventory.Count < 6;
    }
}
