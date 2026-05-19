using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    public class Enemy
    {
        public string enemyName, region;
        public int maxHP, baseAttack, currentHP, currentShield, level;
        public string intentText;
        public int poisonDamage, poisonTurns;
        public Element? attachedElement;
        public Element? weakness;
        public int hitShield;  // blocks first N hits (each hit = 1 dmg)
        public int thorns;     // reflects X damage per hit
        public float evasion;  // dodge chance 0-1
        public float enrageThreshold = 0.5f; // HP% below which enrage triggers
        public bool enraged;
        public int regenPerTurn; // HP healed each enemy turn
        public bool firstAction = true;

        private EnemyDef _def;
        private int _patternIndex;

        public static Enemy Create(int stage, bool elite, bool boss)
        {
            var def = EnemyDatabase.GetRandom(stage, elite, boss);
            float scaling = 1f + stage * 0.35f;
            int hp = Mathf.CeilToInt(def.baseHP * scaling * def.hpScale);
            int atk = Mathf.CeilToInt(def.baseATK * scaling * def.atkScale);

            return new Enemy
            {
                _def = def,
                _patternIndex = 0,
                enemyName = def.name,
                region = def.region,
                maxHP = hp,
                baseAttack = atk,
                currentHP = hp,
                level = stage + 1,
                currentShield = 0,
                poisonDamage = 0,
                poisonTurns = 0,
                attachedElement = def.nativeElement,
                weakness = def.weakTo,
                hitShield = def.hitShield,
                thorns = def.thorns,
                evasion = def.evasion,
                regenPerTurn = def.regen,
                firstAction = true,
            };
        }

        public void DecideAction(BattleManager battle)
        {
            if (poisonTurns > 0)
            {
                currentHP -= poisonDamage;
                if (currentHP < 0) currentHP = 0;
                poisonTurns--;
                battle.AddBattleLog($"{enemyName}受到{poisonDamage}点元素伤害");
            }

            if (_def == null) { intentText = "攻击"; return; }

            // Use first-turn pattern if available
            var activePattern = (firstAction && _def.firstTurnPattern != null && _def.firstTurnPattern.Count > 0)
                ? _def.firstTurnPattern : _def.pattern;
            if (activePattern == null || activePattern.Count == 0) { intentText = "攻击"; return; }

            int idx = firstAction && _def.firstTurnPattern != null ? 0 : _patternIndex;
            var act = activePattern[idx];
            if (firstAction && _def.firstTurnPattern != null)
                firstAction = false;
            else
                _patternIndex = (_patternIndex + 1) % activePattern.Count;

            int val = Mathf.CeilToInt(act.value * (1f + level * 0.1f));
            intentText = act.action switch
            {
                EnemyAction.Attack => $"攻击 {val}伤害",
                EnemyAction.HeavyAttack => $"重击 {Mathf.CeilToInt(val * 1.5f)}伤害",
                EnemyAction.Defend => $"防御 +{val}护盾",
                EnemyAction.Buff => $"强化 攻击+{val}",
                EnemyAction.Debuff => $"减益 削弱攻击力",
                EnemyAction.ReduceSteps => $"干扰 -{val}步",
                EnemyAction.BlockPeg => "封锁柱子 1回合",
                EnemyAction.DiscardCard => "击飞 弃1张牌",
                EnemyAction.HealSelf => $"回复 {val}生命",
                EnemyAction.PoisonPlayer => $"施毒 {val}伤害×3回合",
                EnemyAction.WeakenPlayer => "削弱 降低攻击力",
                EnemyAction.Summon => $"召唤 +{val}护盾",
                _ => act.intentText
            };

            _currentAct = act;
        }

        private EnemyPattern _currentAct;

        public void ExecuteAction(BattleManager battle)
        {
            if (_currentAct == null) { battle.AddBattleLog($"{enemyName}在观察你"); return; }

            var act = _currentAct;
            int val = Mathf.CeilToInt(act.value * (1f + level * 0.1f));

            switch (act.action)
            {
                case EnemyAction.Attack:
                    DealDamage(battle, val);
                    break;

                case EnemyAction.HeavyAttack:
                    DealDamage(battle, Mathf.CeilToInt(val * 1.5f));
                    break;

                case EnemyAction.Defend:
                    currentShield += val;
                    battle.AddBattleLog($"{enemyName}获得{val}点护盾");
                    break;

                case EnemyAction.Buff:
                    baseAttack += val;
                    battle.AddBattleLog($"{enemyName}攻击力提升了{val}点！");
                    break;

                case EnemyAction.Debuff:
                    battle.playerWeakenTurns = 2;
                    battle.playerWeakenPercent = 0.25f + level * 0.05f;
                    battle.AddBattleLog($"{enemyName}削弱了你的攻击力");
                    break;

                case EnemyAction.ReduceSteps:
                    battle.stepsRemaining = Mathf.Max(0, battle.stepsRemaining - val);
                    battle.AddBattleLog($"{enemyName}干扰！-{val}步");
                    break;

                case EnemyAction.BlockPeg:
                    battle.blockedPegTurns = 1;
                    battle.blockedPegIndex = Random.Range(0, 3);
                    battle.AddBattleLog($"{enemyName}封锁了一根柱子！");
                    break;

                case EnemyAction.DiscardCard:
                    for (int i = 0; i < 3; i++)
                    {
                        if (battle.currentHand[i] != null && !battle.currentHand[i].isTaskCard)
                        {
                            battle.AddBattleLog($"{enemyName}击飞了一张手牌！");
                            var nc = GameManager.Instance.Deck.DrawHand(1, battle.taskCardInstance);
                            battle.currentHand[i] = nc.Count > 0 ? nc[0] : null;
                            break;
                        }
                    }
                    break;

                case EnemyAction.HealSelf:
                    int healed = Mathf.Min(val, maxHP - currentHP);
                    currentHP += healed;
                    battle.AddBattleLog($"{enemyName}恢复了{healed}点生命");
                    break;

                case EnemyAction.PoisonPlayer:
                    battle.playerPoisonDamage = val;
                    battle.playerPoisonTurns = 3;
                    battle.AddBattleLog($"{enemyName}施加了元素侵蚀！");
                    break;

                case EnemyAction.WeakenPlayer:
                    battle.playerWeakenTurns = 2;
                    battle.playerWeakenPercent = 0.3f;
                    battle.AddBattleLog($"{enemyName}削弱了你的力量");
                    break;

                case EnemyAction.Summon:
                    currentShield += val;
                    battle.AddBattleLog($"{enemyName}召唤了元素护盾");
                    break;
            }
        }

        void DealDamage(BattleManager battle, int rawDmg)
        {
            int dmg = rawDmg;
            if (battle.enemyWeakenTurns > 0)
            {
                dmg = Mathf.CeilToInt(dmg * (1f - battle.enemyWeakenPercent));
                battle.enemyWeakenTurns--;
            }

            int reflected = 0;
            if (battle.pendingReflectTurns > 0)
            {
                reflected = Mathf.CeilToInt(dmg * battle.pendingReflectPercent);
                battle.pendingReflectTurns--;
            }

            int remaining = dmg;
            if (battle.playerShield > 0)
            {
                int a = Mathf.Min(battle.playerShield, remaining);
                battle.playerShield -= a;
                remaining -= a;
            }
            battle.playerHP -= remaining;
            battle.AddBattleLog($"{enemyName}造成{dmg}点伤害（吸收{dmg - remaining}）");

            if (reflected > 0)
            {
                if (currentShield > 0) { int a = Mathf.Min(currentShield, reflected); currentShield -= a; reflected -= a; }
                currentHP -= reflected;
                battle.AddBattleLog($"元素反射{reflected}点伤害");
            }

            int thorns = GameManager.Instance.permanentThorns;
            if (thorns > 0) { currentHP -= thorns; battle.AddBattleLog($"荆棘{thorns}点"); }
        }
    }
}
