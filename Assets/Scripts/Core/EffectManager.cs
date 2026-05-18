using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    /// <summary>
    /// Executes card effects during battle. All effect logic lives here
    /// for easy extension — add a new EffectType and its case below.
    /// </summary>
    public static class EffectManager
    {
        public static string Execute(CardData card, BattleManager battle)
        {
            return Execute(card, battle, card.element);
        }

        /// <summary>
        /// Execute a card's combat effect. Returns a log message describing what happened.
        /// </summary>
        public static string Execute(CardData card, BattleManager battle, Element? attackElement)
        {
            switch (card.effectType)
            {
                // ── Level 3 ──
                case EffectType.PureDamage:
                    return DoDamage(card.effectValue1, false, battle);

                case EffectType.DamageDraw:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        int drawn = battle.DrawCards(card.effectValue2);
                        if (drawn > 0) log += $"，抽了{drawn}张牌";
                        return log;
                    }

                case EffectType.PureShield:
                    {
                        battle.AddShield(card.effectValue1);
                        SimpleAudio.Instance?.PlayShield();
                        return $"获得{card.effectValue1}点护盾";
                    }

                case EffectType.ShieldReflect:
                    {
                        battle.AddShield(card.effectValue1);
                        battle.pendingReflectPercent = card.effectValueF;
                        battle.pendingReflectTurns = 1;
                        SimpleAudio.Instance?.PlayShield();
                        return $"获得{card.effectValue1}点护盾，反弹{(int)(card.effectValueF * 100)}%伤害";
                    }

                case EffectType.PureHeal:
                    return DoHeal(card.effectValue1, battle);

                case EffectType.HealCleanse:
                    {
                        string log = DoHeal(card.effectValue1, battle);
                        battle.playerPoisonDamage = 0;
                        battle.playerPoisonTurns = 0;
                        return log + "，清除了中毒";
                    }

                case EffectType.StepRecovery:
                    {
                        battle.stepsRemaining += card.effectValue1;
                        return $"恢复{card.effectValue1}步";
                    }

                case EffectType.ComboChain:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.comboMultiplier = 1f + card.effectValueF;
                        battle.comboCharges = 1;
                        return log + $"，下张牌伤害+{(int)(card.effectValueF * 100)}%";
                    }

                // ── Level 4 ──
                case EffectType.MidDamage:
                    return DoDamage(card.effectValue1, false, battle);

                case EffectType.MidDamageDraw:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        int drawn = battle.DrawCards(card.effectValue2);
                        if (drawn > 0) log += $"，抽了{drawn}张牌";
                        return log;
                    }

                case EffectType.MidDamageWeaken:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.enemyWeakenPercent = card.effectValueF;
                        battle.enemyWeakenTurns = 1;
                        return log + $"，敌人虚弱{(int)(card.effectValueF * 100)}%";
                    }

                case EffectType.MidShield:
                    {
                        battle.AddShield(card.effectValue1);
                        SimpleAudio.Instance?.PlayShield();
                        return $"获得{card.effectValue1}点护盾";
                    }

                case EffectType.MidShieldPermATK:
                    {
                        battle.AddShield(card.effectValue1);
                        GameManager.Instance.permanentATKBonus += card.effectValue2;
                        SimpleAudio.Instance?.PlayShield();
                        return $"获得{card.effectValue1}点护盾，永久攻击力+{card.effectValue2}";
                    }

                case EffectType.MidHeal:
                    return DoHeal(card.effectValue1, battle);

                case EffectType.MidHealBoost:
                    {
                        string log = DoHeal(card.effectValue1, battle);
                        battle.nextTurnDamageBonus = card.effectValueF;
                        return log + $"，下回合伤害+{(int)(card.effectValueF * 100)}%";
                    }

                case EffectType.MidStepRecovery:
                    {
                        battle.stepsRemaining += card.effectValue1;
                        return $"恢复{card.effectValue1}步";
                    }

                case EffectType.MidComboChain:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.comboMultiplier = 1f + card.effectValueF;
                        battle.comboCharges = 1;
                        return log + $"，下张牌伤害+{(int)(card.effectValueF * 100)}%";
                    }

                // ── Level 5 ──
                case EffectType.DamagePoison:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.enemyPoisonDamage = card.effectValue2;
                        battle.enemyPoisonTurns = (int)card.effectValueF;
                        return log + $"，敌人中毒({card.effectValue2}伤害×{(int)card.effectValueF}回合)";
                    }

                case EffectType.DamageExecute:
                    {
                        int dmg = card.effectValue1;
                        bool doubled = false;
                        if (battle.enemy != null && battle.enemy.currentHP < battle.enemy.maxHP * 0.5f)
                        {
                            dmg *= 2;
                            doubled = true;
                        }
                        string log = DoDamage(dmg, false, battle);
                        return doubled ? log + "（处决！）" : log;
                    }

                case EffectType.DamageLifesteal:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        int heal = Mathf.CeilToInt(card.effectValue1 * card.effectValueF);
                        battle.HealPlayer(heal);
                        SimpleAudio.Instance?.PlayHeal();
                        return log + $"，吸取了{heal}点生命";
                    }

                case EffectType.ShieldBonusSteps:
                    {
                        battle.AddShield(card.effectValue1);
                        battle.stepsRemaining += card.effectValue2;
                        SimpleAudio.Instance?.PlayShield();
                        return $"获得{card.effectValue1}点护盾，+{card.effectValue2}步";
                    }

                case EffectType.HeavyShield:
                    {
                        battle.AddShield(card.effectValue1);
                        SimpleAudio.Instance?.PlayShield();
                        return $"获得{card.effectValue1}点护盾";
                    }

                case EffectType.DamageShield:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.AddShield(card.effectValue2);
                        SimpleAudio.Instance?.PlayShield();
                        return log + $"，获得{card.effectValue2}点护盾";
                    }

                case EffectType.HeavyHeal:
                    return DoHeal(card.effectValue1, battle);

                case EffectType.HeavyComboChain:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.comboMultiplier = 1f + card.effectValueF;
                        battle.comboCharges = 1;
                        return log + $"，下张牌伤害+{(int)(card.effectValueF * 100)}%";
                    }

                // ── Level 6 ──
                case EffectType.DamagePierce:
                    return DoDamage(card.effectValue1, true, battle);

                case EffectType.DamageStun:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.enemyStunTurns += card.effectValue2;
                        return log + "，敌人被眩晕！";
                    }

                case EffectType.HeavyDamage:
                    return DoDamage(card.effectValue1, false, battle);

                case EffectType.ShieldPermATKLarge:
                    {
                        battle.AddShield(card.effectValue1);
                        GameManager.Instance.permanentATKBonus += card.effectValue2;
                        SimpleAudio.Instance?.PlayShield();
                        return $"获得{card.effectValue1}点护盾，永久攻击力+{card.effectValue2}";
                    }

                case EffectType.HealDamageBoost:
                    {
                        string log = DoHeal(card.effectValue1, battle);
                        battle.nextTurnDamageBonus = card.effectValueF;
                        return log + $"，下回合伤害+{(int)(card.effectValueF * 100)}%";
                    }

                case EffectType.MassiveHeal:
                    return DoHeal(card.effectValue1, battle);

                case EffectType.RefreshMultiplier:
                    {
                        battle.refreshCharges += card.effectValue2;
                        GameManager.Instance.stepMultiplier += card.effectValueF;
                        return $"刷新次数+{card.effectValue2}，步数倍率+{card.effectValueF:F2}";
                    }

                case EffectType.DamagePerStep:
                    {
                        int stepsConsumed = battle.stepsConsumedThisTurn;
                        int dmg = stepsConsumed * card.effectValue1;
                        if (dmg <= 0) dmg = card.effectValue1 * 5; // floor
                        return DoDamage(dmg, false, battle);
                    }

                case EffectType.ShieldBurst:
                    {
                        int shieldDmg = Mathf.CeilToInt(battle.playerShield * card.effectValueF);
                        battle.playerShield = 0;
                        return DoDamage(shieldDmg, true, battle) + "（护盾爆破！）";
                    }

                // ── Level 7 ──
                case EffectType.DamagePermATK:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        GameManager.Instance.permanentATKBonus += card.effectValue2;
                        return log + $"，永久攻击力+{card.effectValue2}";
                    }

                case EffectType.DamageLifestealStrong:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        int heal = Mathf.CeilToInt(card.effectValue1 * card.effectValueF);
                        battle.HealPlayer(heal);
                        SimpleAudio.Instance?.PlayHeal();
                        return log + $"，吸取了{heal}点生命";
                    }

                case EffectType.DamageStunStrong:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.enemyStunTurns += card.effectValue2;
                        return log + "，敌人被眩晕！";
                    }

                case EffectType.MassiveDamage:
                    return DoDamage(card.effectValue1, false, battle);

                case EffectType.RefreshMultiplierStrong:
                    {
                        battle.refreshCharges += card.effectValue2;
                        GameManager.Instance.stepMultiplier += card.effectValueF;
                        return $"刷新次数+{card.effectValue2}，步数倍率+{card.effectValueF:F2}";
                    }

                case EffectType.ExtraTurn:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        battle.extraTurnPending = true;
                        return log + "，获得额外回合！";
                    }

                case EffectType.ShieldReflectStrong:
                    {
                        battle.AddShield(card.effectValue1);
                        battle.pendingReflectPercent = card.effectValueF;
                        battle.pendingReflectTurns = 1;
                        SimpleAudio.Instance?.PlayShield();
                        return $"获得{card.effectValue1}点护盾，反弹{(int)(card.effectValueF * 100)}%伤害";
                    }

                case EffectType.HealPermATK:
                    {
                        string log = DoHeal(card.effectValue1, battle);
                        GameManager.Instance.permanentATKBonus += card.effectValue2;
                        return log + $"，永久攻击力+{card.effectValue2}";
                    }

                case EffectType.Overkill:
                    {
                        string log = DoDamage(card.effectValue1, false, battle);
                        if (battle.enemy != null && battle.enemy.currentHP <= 0)
                        {
                            GameManager.Instance.permanentATKBonus += card.effectValue2;
                            log += $"，击杀！永久攻击力+{card.effectValue2}";
                        }
                        return log;
                    }

                case EffectType.LuckyHit:
                    {
                        int dmg = card.effectValue1;
                        bool crit = Random.value < card.effectValueF;
                        if (crit) dmg *= 3;
                        string log = DoDamage(dmg, false, battle);
                        return crit ? log + "（会心一击！）" : log;
                    }

                // ── Task card ──
                case EffectType.TaskStepMultiplier:
                    {
                        int count = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            if (battle.handPuzzles[i] != null && !battle.handPuzzles[i].IsComplete())
                            {
                                battle.handPuzzles[i].SetOneMoveFromComplete();
                                count++;
                            }
                        }
                        if (battle.taskPuzzle != null)
                        {
                            battle.taskPuzzle.GenerateRandomState();
                            battle.taskPuzzle.SaveToCardData(battle.taskCardInstance);
                        }
                        return $"预完成{count}张手牌（仅差1步）！";
                    }

                default:
                    return "未知效果";
            }
        }

        private static string DoDamage(int baseDamage, bool pierce, BattleManager battle, Element? element = null)
        {
            int atk = battle.baseATK + GameManager.Instance.permanentATKBonus;
            int totalDamage = Mathf.CeilToInt((baseDamage + atk) * battle.GetDamageMultiplier());
            int actual = battle.DealDamageToEnemy(totalDamage, pierce, element);
            SimpleAudio.Instance?.PlayDamage();
            string extra = pierce ? "（无视护盾）" : "";
            return $"造成{actual}点伤害{extra}";
        }

        private static string DoHeal(int amount, BattleManager battle)
        {
            int actual = battle.HealPlayer(amount);
            SimpleAudio.Instance?.PlayHeal();
            return $"回复{actual}点生命";
        }
    }
}
