using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    public enum EnemyAction { Attack, HeavyAttack, Defend, Buff, Debuff, ReduceSteps, BlockPeg, DiscardCard, HealSelf, PoisonPlayer, WeakenPlayer, Summon }

    [System.Serializable]
    public class EnemyPattern
    {
        public EnemyAction action;
        public int value;
        public string intentText;
    }

    [System.Serializable]
    public class EnemyDef
    {
        public string name, region;
        public int baseHP, baseATK;
        public float hpScale = 1f, atkScale = 1f;
        public List<EnemyPattern> pattern;
        public List<EnemyPattern> firstTurnPattern; // optional: override first turn
        public bool isElite, isBoss;
        public Element? nativeElement;
        public Element? weakTo; // takes 2x damage from this
    }

    /// <summary>
    /// Complete Genshin Impact enemy database with StS-style fixed action patterns.
    /// Each enemy has a consistent pattern that repeats.
    /// </summary>
    public static class EnemyDatabase
    {
        // ── Utility ──
        static EnemyPattern A(EnemyAction a, int v, string txt) => new() { action = a, value = v, intentText = txt };

        // ── Enemy pools by stage ──
        // Helper: element-typed attack intent
        static string Elem(Element e) => e switch
        {
            Element.Pyro => "火", Element.Cryo => "冰", Element.Hydro => "水",
            Element.Electro => "雷", Element.Anemo => "风", Element.Geo => "岩",
            Element.Dendro => "草", _ => ""
        };

        public static readonly List<EnemyDef>[] Pools = new List<EnemyDef>[3];
        public static readonly List<EnemyDef>[] ElitePools = new List<EnemyDef>[3];
        public static readonly List<EnemyDef>[] BossPools = new List<EnemyDef>[3];

        static EnemyDatabase()
        {
            // ==================== STAGE 1: 蒙德 Mondstadt ====================
            Pools[0] = new List<EnemyDef>
            {
                // Slimes — permanent elemental aura
                new() { name = "火史莱姆", region = "蒙德", baseHP = 28, baseATK = 6, nativeElement = Element.Pyro, weakTo = Element.Hydro,
                    pattern = new() { A(EnemyAction.Attack, 6, "火焰撞击"), A(EnemyAction.Attack, 5, "元素喷射"), A(EnemyAction.Defend, 4, "元素凝聚") }},
                new() { name = "冰史莱姆", region = "蒙德", baseHP = 30, baseATK = 5, nativeElement = Element.Cryo, weakTo = Element.Pyro,
                    pattern = new() { A(EnemyAction.Attack, 5, "冰锥射击"), A(EnemyAction.Defend, 5, "冰甲凝成"), A(EnemyAction.Attack, 5, "冰锥射击") }},
                new() { name = "水史莱姆", region = "蒙德", baseHP = 26, baseATK = 5, nativeElement = Element.Hydro, weakTo = Element.Electro,
                    pattern = new() { A(EnemyAction.Attack, 5, "水弹"), A(EnemyAction.HealSelf, 5, "水元素自愈"), A(EnemyAction.Attack, 5, "水弹") }},
                new() { name = "雷史莱姆", region = "蒙德", baseHP = 24, baseATK = 7, nativeElement = Element.Electro, weakTo = Element.Cryo,
                    pattern = new() { A(EnemyAction.Attack, 7, "放电"), A(EnemyAction.Attack, 6, "电击"), A(EnemyAction.ReduceSteps, 2, "麻痹干扰") }},
                new() { name = "岩史莱姆", region = "蒙德", baseHP = 34, baseATK = 5, nativeElement = Element.Geo, weakTo = Element.Geo,
                    pattern = new() { A(EnemyAction.Defend, 6, "岩甲"), A(EnemyAction.Attack, 5, "岩弹"), A(EnemyAction.Attack, 5, "岩弹") }},

                // Hilichurls
                new() { name = "丘丘人", region = "蒙德", baseHP = 30, baseATK = 6,
                    pattern = new() { A(EnemyAction.Attack, 6, "棒击"), A(EnemyAction.Attack, 6, "爪击") }},
                new() { name = "打手丘丘人", region = "蒙德", baseHP = 32, baseATK = 7,
                    pattern = new() { A(EnemyAction.Attack, 7, "连打"), A(EnemyAction.Attack, 8, "重击") }},
                new() { name = "木盾丘丘人", region = "蒙德", baseHP = 34, baseATK = 5,
                    pattern = new() { A(EnemyAction.Defend, 5, "举盾"), A(EnemyAction.Attack, 5, "盾击"), A(EnemyAction.Attack, 5, "盾击") }},
                new() { name = "爆弹丘丘人", region = "蒙德", baseHP = 26, baseATK = 8,
                    pattern = new() { A(EnemyAction.HeavyAttack, 9, "投掷爆弹"), A(EnemyAction.Attack, 5, "挥击") }},
                new() { name = "射手丘丘人", region = "蒙德", baseHP = 24, baseATK = 7,
                    pattern = new() { A(EnemyAction.Attack, 7, "射箭"), A(EnemyAction.Attack, 7, "精准射击") }},

                // Treasure Hoarders
                new() { name = "盗宝团·斥候", region = "蒙德", baseHP = 28, baseATK = 6,
                    pattern = new() { A(EnemyAction.Attack, 6, "匕首"), A(EnemyAction.Attack, 5, "飞刀"), A(EnemyAction.Debuff, 0, "扬沙致盲") }},
                new() { name = "盗宝团·神射手", region = "蒙德", baseHP = 26, baseATK = 8,
                    pattern = new() { A(EnemyAction.Attack, 7, "射击"), A(EnemyAction.HeavyAttack, 9, "蓄力射击") }},
                new() { name = "盗宝团·药剂师", region = "蒙德", baseHP = 24, baseATK = 7,
                    pattern = new() { A(EnemyAction.PoisonPlayer, 3, "投掷毒剂"), A(EnemyAction.Attack, 5, "挥击"), A(EnemyAction.PoisonPlayer, 2, "毒雾") }},
            };

            ElitePools[0] = new List<EnemyDef>
            {
                new() { name = "木盾丘丘暴徒", region = "蒙德", baseHP = 55, baseATK = 9, isElite = true,
                    pattern = new() { A(EnemyAction.Defend, 8, "木盾格挡"), A(EnemyAction.HeavyAttack, 12, "盾牌冲撞"), A(EnemyAction.Attack, 8, "横扫"), A(EnemyAction.BlockPeg, 1, "盾牌压制") }},
                new() { name = "火斧丘丘暴徒", region = "蒙德", baseHP = 50, baseATK = 11, isElite = true,
                    pattern = new() { A(EnemyAction.HeavyAttack, 14, "火焰劈斩"), A(EnemyAction.Attack, 9, "横砍"), A(EnemyAction.Buff, 3, "怒火"), A(EnemyAction.HeavyAttack, 12, "旋转劈斩") }},
                new() { name = "丘丘岩盔王", region = "蒙德", baseHP = 65, baseATK = 10, isElite = true, nativeElement = Element.Geo,
                    firstTurnPattern = new() { A(EnemyAction.Defend, 15, "岩铠附身·护盾"), A(EnemyAction.Buff, 4, "岩元素强化") },
                    pattern = new() { A(EnemyAction.HeavyAttack, 15, "岩拳重击"), A(EnemyAction.Attack, 8, "挥拳"), A(EnemyAction.Debuff, 3, "地面震颤"), A(EnemyAction.HeavyAttack, 13, "岩刺") }},
                new() { name = "丘丘霜铠王", region = "蒙德", baseHP = 60, baseATK = 11, isElite = true, nativeElement = Element.Cryo,
                    firstTurnPattern = new() { A(EnemyAction.Defend, 12, "冰铠附身·护盾"), A(EnemyAction.Buff, 3, "冰元素强化") },
                    pattern = new() { A(EnemyAction.Attack, 9, "冰拳"), A(EnemyAction.HeavyAttack, 14, "冰爆"), A(EnemyAction.ReduceSteps, 3, "冻结领域"), A(EnemyAction.Attack, 10, "寒冰冲击") }},
                new() { name = "火深渊法师", region = "蒙德", baseHP = 45, baseATK = 10, isElite = true, nativeElement = Element.Pyro,
                    firstTurnPattern = new() { A(EnemyAction.Defend, 10, "火元素护盾") },
                    pattern = new() { A(EnemyAction.HeavyAttack, 14, "烈焰喷涌"), A(EnemyAction.Attack, 9, "火球"), A(EnemyAction.PoisonPlayer, 4, "灼烧"), A(EnemyAction.Attack, 10, "火焰爆弹") }},
                new() { name = "狂风之核", region = "蒙德", baseHP = 50, baseATK = 10, isElite = true, nativeElement = Element.Anemo,
                    pattern = new() { A(EnemyAction.ReduceSteps, 3, "狂风"), A(EnemyAction.Attack, 8, "风刃"), A(EnemyAction.HeavyAttack, 13, "龙卷风"), A(EnemyAction.Attack, 8, "风刃") }},
                new() { name = "遗迹守卫", region = "蒙德", baseHP = 60, baseATK = 11, isElite = true,
                    pattern = new() { A(EnemyAction.HeavyAttack, 15, "导弹齐射"), A(EnemyAction.Attack, 10, "铁拳"), A(EnemyAction.BlockPeg, 1, "锁定目标"), A(EnemyAction.HeavyAttack, 13, "旋转攻击") }},
            };

            BossPools[0] = new List<EnemyDef>
            {
                new() { name = "风魔龙·特瓦林", region = "蒙德", baseHP = 120, baseATK = 14, isBoss = true,
                    pattern = new() { A(EnemyAction.Defend, 12, "龙鳞护体"), A(EnemyAction.HeavyAttack, 18, "龙息"), A(EnemyAction.ReduceSteps, 4, "风暴"), A(EnemyAction.Attack, 13, "俯冲"), A(EnemyAction.DiscardCard, 1, "风压"), A(EnemyAction.HeavyAttack, 22, "终天闭幕曲") }},
                new() { name = "北风狼·安德留斯", region = "蒙德", baseHP = 140, baseATK = 13, isBoss = true,
                    pattern = new() { A(EnemyAction.Buff, 4, "狼灵觉醒"), A(EnemyAction.HeavyAttack, 16, "利爪撕裂"), A(EnemyAction.Attack, 12, "突进"), A(EnemyAction.Debuff, 0, "冰风怒吼"), A(EnemyAction.HeavyAttack, 20, "终幕冰尘") }},
            };

            // ==================== STAGE 2: 璃月/稻妻 Liyue/Inazuma ====================
            Pools[1] = new List<EnemyDef>
            {
                new() { name = "愚人众·火铳游击兵", region = "璃月", baseHP = 38, baseATK = 9,
                    pattern = new() { A(EnemyAction.Attack, 8, "火铳射击"), A(EnemyAction.HeavyAttack, 11, "火焰喷射"), A(EnemyAction.Attack, 8, "火铳射击") }},
                new() { name = "愚人众·风拳前锋军", region = "璃月", baseHP = 40, baseATK = 10,
                    pattern = new() { A(EnemyAction.Defend, 6, "格挡"), A(EnemyAction.Attack, 9, "冲拳"), A(EnemyAction.HeavyAttack, 13, "旋风拳") }},
                new() { name = "野伏众·无宿", region = "稻妻", baseHP = 34, baseATK = 9,
                    pattern = new() { A(EnemyAction.Attack, 9, "居合斩"), A(EnemyAction.Attack, 8, "连斩") }},
                new() { name = "海乱鬼·炎威", region = "稻妻", baseHP = 42, baseATK = 11,
                    pattern = new() { A(EnemyAction.HeavyAttack, 14, "火焰斩"), A(EnemyAction.Buff, 4, "炎威"), A(EnemyAction.HeavyAttack, 12, "烈火斩") }},
                new() { name = "浮游水蕈兽", region = "须弥", baseHP = 32, baseATK = 8,
                    pattern = new() { A(EnemyAction.Attack, 8, "水弹"), A(EnemyAction.HealSelf, 6, "孢子自愈"), A(EnemyAction.Attack, 8, "水弹") }},
                new() { name = "嗜雷·兽境幼兽", region = "稻妻", baseHP = 30, baseATK = 9,
                    pattern = new() { A(EnemyAction.Attack, 9, "撕咬"), A(EnemyAction.Debuff, 0, "侵蚀"), A(EnemyAction.Attack, 8, "扑击") }},
                new() { name = "镀金旅团·叩问人", region = "须弥", baseHP = 36, baseATK = 10,
                    pattern = new() { A(EnemyAction.Attack, 9, "弯刀"), A(EnemyAction.Defend, 5, "沙盾"), A(EnemyAction.Attack, 10, "旋风斩") }},
                new() { name = "机关·侦察记录型", region = "枫丹", baseHP = 35, baseATK = 9,
                    pattern = new() { A(EnemyAction.Attack, 8, "激光"), A(EnemyAction.ReduceSteps, 2, "信号干扰"), A(EnemyAction.Attack, 9, "精准射击") }},
            };

            ElitePools[1] = new List<EnemyDef>
            {
                new() { name = "雷萤术士", region = "璃月", baseHP = 55, baseATK = 12, isElite = true,
                    pattern = new() { A(EnemyAction.Summon, 5, "召唤雷萤"), A(EnemyAction.Attack, 11, "雷击"), A(EnemyAction.HeavyAttack, 16, "雷暴"), A(EnemyAction.WeakenPlayer, 0, "麻痹") }},
                new() { name = "火之债务处理人", region = "璃月", baseHP = 52, baseATK = 13, isElite = true,
                    pattern = new() { A(EnemyAction.HeavyAttack, 16, "暗杀"), A(EnemyAction.Debuff, 0, "标记"), A(EnemyAction.HeavyAttack, 18, "处决"), A(EnemyAction.Attack, 10, "影袭") }},
                new() { name = "藏镜仕女", region = "稻妻", baseHP = 58, baseATK = 11, isElite = true,
                    pattern = new() { A(EnemyAction.Defend, 10, "镜返"), A(EnemyAction.HeavyAttack, 15, "镜碎"), A(EnemyAction.BlockPeg, 1, "镜界封锁"), A(EnemyAction.Attack, 10, "镜光") }},
                new() { name = "岩龙蜥", region = "璃月", baseHP = 62, baseATK = 12, isElite = true,
                    pattern = new() { A(EnemyAction.Defend, 10, "岩甲"), A(EnemyAction.HeavyAttack, 17, "滚动冲撞"), A(EnemyAction.Attack, 10, "甩尾"), A(EnemyAction.HeavyAttack, 15, "岩刺") }},
                new() { name = "遗迹猎者", region = "稻妻", baseHP = 56, baseATK = 13, isElite = true,
                    pattern = new() { A(EnemyAction.HeavyAttack, 16, "导弹轰炸"), A(EnemyAction.BlockPeg, 1, "锁定"), A(EnemyAction.Attack, 11, "激光"), A(EnemyAction.HeavyAttack, 15, "扫射") }},
                new() { name = "深渊咏者·渊火", region = "璃月", baseHP = 50, baseATK = 14, isElite = true,
                    pattern = new() { A(EnemyAction.Defend, 10, "深渊火盾"), A(EnemyAction.HeavyAttack, 18, "渊火喷涌"), A(EnemyAction.PoisonPlayer, 5, "深渊灼烧"), A(EnemyAction.HeavyAttack, 16, "烈焰风暴") }},
                new() { name = "黑蛇骑士·斩风", region = "层岩", baseHP = 60, baseATK = 13, isElite = true,
                    pattern = new() { A(EnemyAction.Attack, 11, "风剑"), A(EnemyAction.Buff, 4, "黑蛇强化"), A(EnemyAction.HeavyAttack, 17, "暗影斩"), A(EnemyAction.Attack, 12, "连斩") }},
                new() { name = "圣骸毒蝎", region = "须弥", baseHP = 58, baseATK = 14, isElite = true,
                    pattern = new() { A(EnemyAction.PoisonPlayer, 5, "毒刺"), A(EnemyAction.HeavyAttack, 16, "尾刺"), A(EnemyAction.Defend, 8, "甲壳"), A(EnemyAction.HeavyAttack, 18, "毒爆") }},
            };

            BossPools[1] = new List<EnemyDef>
            {
                new() { name = "公子·达达利亚", region = "璃月", baseHP = 170, baseATK = 15, isBoss = true,
                    pattern = new() { A(EnemyAction.Attack, 12, "水刃"), A(EnemyAction.Buff, 5, "魔王武装"), A(EnemyAction.HeavyAttack, 20, "鲸吞噬灭"), A(EnemyAction.Attack, 14, "双刃连斩"), A(EnemyAction.ReduceSteps, 5, "激流"), A(EnemyAction.HeavyAttack, 25, "极恶技·尽灭闪") }},
                new() { name = "雷电将军·祸津御建", region = "稻妻", baseHP = 190, baseATK = 16, isBoss = true,
                    pattern = new() { A(EnemyAction.Defend, 15, "祸津铠"), A(EnemyAction.HeavyAttack, 22, "无想的一刀"), A(EnemyAction.Attack, 14, "雷斩"), A(EnemyAction.BlockPeg, 1, "眼差封印"), A(EnemyAction.HeavyAttack, 26, "梦想真说") }},
            };

            // ==================== STAGE 3: 须弥/枫丹/纳塔 Sumeru/Fontaine/Natlan ====================
            Pools[2] = new List<EnemyDef>
            {
                new() { name = "镀金旅团·疾迅勇士", region = "须弥", baseHP = 44, baseATK = 11,
                    pattern = new() { A(EnemyAction.Attack, 11, "弯刃"), A(EnemyAction.HeavyAttack, 14, "冲刺斩"), A(EnemyAction.Attack, 10, "飞刀") }},
                new() { name = "元能构装体·力场", region = "须弥", baseHP = 46, baseATK = 10,
                    pattern = new() { A(EnemyAction.Defend, 8, "力场护盾"), A(EnemyAction.HeavyAttack, 14, "能量冲击"), A(EnemyAction.Attack, 10, "光束") }},
                new() { name = "圣骸赤鹫", region = "须弥", baseHP = 42, baseATK = 12,
                    pattern = new() { A(EnemyAction.Attack, 11, "俯冲"), A(EnemyAction.Debuff, 3, "撕裂"), A(EnemyAction.HeavyAttack, 15, "啄击") }},
                new() { name = "浊水喷吐幻灵", region = "枫丹", baseHP = 40, baseATK = 11,
                    pattern = new() { A(EnemyAction.PoisonPlayer, 4, "浊水喷射"), A(EnemyAction.Attack, 10, "水弹"), A(EnemyAction.HealSelf, 6, "水元素吸收") }},
                new() { name = "隙境原体·狂蔓", region = "枫丹", baseHP = 44, baseATK = 12,
                    pattern = new() { A(EnemyAction.HeavyAttack, 15, "蔓藤抽击"), A(EnemyAction.Debuff, 3, "缠绕"), A(EnemyAction.Attack, 11, "鞭击") }},
                new() { name = "嗜雷·兽境猎犬", region = "稻妻", baseHP = 40, baseATK = 12,
                    pattern = new() { A(EnemyAction.Attack, 10, "雷牙"), A(EnemyAction.Debuff, 4, "侵蚀"), A(EnemyAction.HeavyAttack, 15, "雷爪撕裂") }},
                new() { name = "突角龙武士·破空", region = "纳塔", baseHP = 46, baseATK = 12,
                    pattern = new() { A(EnemyAction.HeavyAttack, 15, "龙角突刺"), A(EnemyAction.Buff, 5, "龙血沸腾"), A(EnemyAction.Attack, 11, "横扫") }},
            };

            ElitePools[2] = new List<EnemyDef>
            {
                new() { name = "圣骸角鳄", region = "须弥", baseHP = 70, baseATK = 15, isElite = true,
                    pattern = new() { A(EnemyAction.Defend, 14, "厚皮"), A(EnemyAction.HeavyAttack, 20, "死亡翻滚"), A(EnemyAction.Attack, 13, "咬合"), A(EnemyAction.HeavyAttack, 22, "巨颚粉碎") }},
                new() { name = "深渊使徒·激流", region = "须弥", baseHP = 60, baseATK = 14, isElite = true,
                    pattern = new() { A(EnemyAction.Defend, 12, "激流护盾"), A(EnemyAction.DiscardCard, 1, "水流冲击"), A(EnemyAction.HeavyAttack, 18, "深渊激流"), A(EnemyAction.Attack, 12, "水刃") }},
                new() { name = "兆载永劫龙兽", region = "须弥", baseHP = 75, baseATK = 15, isElite = true,
                    pattern = new() { A(EnemyAction.HeavyAttack, 18, "龙息炮"), A(EnemyAction.Defend, 12, "龙鳞"), A(EnemyAction.BlockPeg, 1, "锁定"), A(EnemyAction.HeavyAttack, 22, "毁灭之光") }},
                new() { name = "冰风组曲·科培琉司", region = "枫丹", baseHP = 68, baseATK = 14, isElite = true,
                    pattern = new() { A(EnemyAction.Attack, 12, "冰刃"), A(EnemyAction.ReduceSteps, 4, "冰风"), A(EnemyAction.HeavyAttack, 18, "冰风暴"), A(EnemyAction.Defend, 10, "冰墙") }},
                new() { name = "熔岩辉龙像", region = "纳塔", baseHP = 72, baseATK = 16, isElite = true,
                    pattern = new() { A(EnemyAction.HeavyAttack, 19, "岩浆喷发"), A(EnemyAction.Defend, 14, "熔岩甲"), A(EnemyAction.PoisonPlayer, 6, "灼热"), A(EnemyAction.HeavyAttack, 24, "陨石坠落") }},
            };

            BossPools[2] = new List<EnemyDef>
            {
                new() { name = "正机之神·散兵", region = "须弥", baseHP = 220, baseATK = 17, isBoss = true,
                    pattern = new() { A(EnemyAction.Defend, 18, "正机之铠"), A(EnemyAction.HeavyAttack, 25, "雷光炮"), A(EnemyAction.ReduceSteps, 6, "虚空封锁"), A(EnemyAction.Attack, 16, "机甲重击"), A(EnemyAction.HeavyAttack, 30, "寂照·万物成灰") }},
                new() { name = "仆人·阿蕾奇诺", region = "枫丹", baseHP = 240, baseATK = 18, isBoss = true,
                    pattern = new() { A(EnemyAction.HeavyAttack, 22, "血影斩"), A(EnemyAction.Buff, 7, "红死之宴"), A(EnemyAction.DiscardCard, 1, "生命之契"), A(EnemyAction.HeavyAttack, 26, "赤月舞踏"), A(EnemyAction.WeakenPlayer, 0, "蚀命印记"), A(EnemyAction.HeavyAttack, 35, "终幕·血之谢礼") }},
            };
        }

        /// <summary>Pick a random enemy from the pool for the given stage and elite/boss status.</summary>
        public static EnemyDef GetRandom(int stage, bool elite, bool boss)
        {
            int idx = Mathf.Clamp(stage, 0, 2);
            var pool = boss ? BossPools[idx] : (elite ? ElitePools[idx] : Pools[idx]);
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
