using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    /// <summary>
    /// Controls all battle flow: turns, steps, hand management, effects.
    /// Attach to a GameObject in the battle scene.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("Player Stats")]
        public int playerMaxHP = 60;
        public int playerHP;
        public int playerShield;
        public int baseATK = 3;
        public float damageBonus = 0f;
        public Dictionary<string, int> cardsCompletedThisTurn = new(); // fatigue per effectType+towerLevel

        public float GetFatigueMultiplier(string key)
        {
            if (!cardsCompletedThisTurn.ContainsKey(key)) return 1f;
            int count = cardsCompletedThisTurn[key];
            return count switch { 0 => 1f, 1 => 0.6f, 2 => 0.4f, _ => 0.25f };
        }

        public void RecordCardCompletion(string key)
        {
            cardsCompletedThisTurn.TryGetValue(key, out int c);
            cardsCompletedThisTurn[key] = c + 1;
        } // temporary one-turn damage bonus (additive with ATK)

        [Header("Turn State")]
        public CardData[] currentHand = new CardData[3];
        public HanoiPuzzle[] handPuzzles = new HanoiPuzzle[3];
        public int stepsRemaining;
        public int stepsConsumedThisTurn;
        public int refreshCharges = 3;
        public int maxRefreshCharges = 3;

        // Status effects (player side)
        [HideInInspector] public int playerPoisonDamage;
        [HideInInspector] public int playerPoisonTurns;
        [HideInInspector] public int playerWeakenTurns;
        [HideInInspector] public float playerWeakenPercent;

        // Status effects (enemy side)
        [HideInInspector] public int enemyPoisonDamage;
        [HideInInspector] public int enemyPoisonTurns;
        [HideInInspector] public int enemyStunTurns;
        [HideInInspector] public float enemyWeakenPercent;
        [HideInInspector] public int enemyWeakenTurns;

        // Combo system
        [HideInInspector] public float comboMultiplier = 1f;
        [HideInInspector] public int comboCharges;

        // Reflect
        [HideInInspector] public float pendingReflectPercent;
        [HideInInspector] public int pendingReflectTurns;

        // Damage boost next turn
        [HideInInspector] public float nextTurnDamageBonus;

        // Extra turn
        [HideInInspector] public bool extraTurnPending;

        // Blocked peg
        [HideInInspector] public int blockedPegTurns;
        [HideInInspector] public int blockedPegIndex = -1;

        // Enemy
        public Enemy enemy;
        public int enemyStage;

        // Task card persistent reference
        public CardData taskCardInstance;
        public HanoiPuzzle taskPuzzle;
        public int taskStepsRemaining;

        // Events for UI
        public System.Action OnStateChanged;
        public System.Action<int> OnCardCompleted; // handIndex
        public System.Action OnTaskCompleted;
        public System.Action<string> OnBattleLog;

        public bool isEliteBattle;
        public bool isBossBattle;
        public bool fastMode;
        public float fastModeSpeed => fastMode ? 0.1f : 1f;

        // Undo support
        public HanoiPuzzle lastPuzzleState;
        public int lastHandIndex = -1;

        private List<string> _battleLog = new List<string>();
        private bool _playerTurn;
        private bool _battleEnded;

        private void Awake()
        {
            // Use persistent HP from GameManager, or init fresh
            int max = playerMaxHP + GameManager.Instance.maxHPBonus;
            if (GameManager.Instance.persistentHP <= 0 || GameManager.Instance.persistentHP > max)
                GameManager.Instance.persistentHP = max;
            playerHP = GameManager.Instance.persistentHP;
            enemyStage = GameManager.Instance.currentStage;
            taskCardInstance = GameManager.Instance.Deck.cards.Find(c => c.isTaskCard);

            // Init or restore task puzzle
            if (taskCardInstance != null)
            {
                taskPuzzle = new HanoiPuzzle(8);
                if (taskCardInstance.peg0State.Count > 0 || taskCardInstance.peg1State.Count > 0 ||
                    taskCardInstance.peg2State.Count > 0)
                    taskPuzzle.LoadFromCardData(taskCardInstance);
            }
            taskStepsRemaining = GameManager.Instance.taskSteps;
        }

        public void BeginBattle()
        {
            int stageIdx = GameManager.Instance.currentMapStage;
            enemy = Enemy.Create(stageIdx, isEliteBattle, isBossBattle);
            enemy.DecideAction(this);
            ElementReactions.Reset();
            StartPlayerTurn();
        }

        /// <summary>
        /// Draw 3 cards, calculate steps, begin player action phase.
        /// </summary>
        public void StartPlayerTurn()
        {
            if (_battleEnded) return;
            _playerTurn = true;
            stepsConsumedThisTurn = 0;
            cardsCompletedThisTurn.Clear();
            playerShield = 0; // shield resets each turn
            refreshCharges = maxRefreshCharges;

            // Apply player poison
            if (playerPoisonTurns > 0)
            {
                playerHP -= playerPoisonDamage;
                playerPoisonTurns--;
                AddBattleLog($"中毒受到{playerPoisonDamage}点伤害");
                if (playerHP <= 0) { EndBattle(false); return; }
            }

            // Draw hand
            DrawNewHand();

            // Calculate steps — skip 8-layer task card
            int baseSteps = 0;
            foreach (var puzzle in handPuzzles)
            {
                if (puzzle != null && puzzle.diskCount < 8)
                    baseSteps += puzzle.OptimalSteps;
            }
            stepsRemaining = Mathf.CeilToInt(baseSteps * GameManager.Instance.stepMultiplier);
            AddBattleLog($"回合开始，步数: {stepsRemaining}（倍率: ×{GameManager.Instance.stepMultiplier:F2}）");

            OnStateChanged?.Invoke();
        }

        private void DrawNewHand()
        {
            currentHand = new CardData[3];
            handPuzzles = new HanoiPuzzle[3];
            var drawn = GameManager.Instance.Deck.DrawHand(3, taskCardInstance);
            for (int i = 0; i < drawn.Count; i++)
            {
                currentHand[i] = drawn[i];
                handPuzzles[i] = new HanoiPuzzle(currentHand[i].towerLevel);
            }
        }

        /// <summary>
        /// Consume 1 step for a disk move. Returns false if no steps left.
        /// </summary>
        public bool UseStep()
        {
            if (stepsRemaining <= 0) return false;
            stepsRemaining--;
            stepsConsumedThisTurn++;
            OnStateChanged?.Invoke();
            return true;
        }

        public bool UseTaskStep()
        {
            if (taskStepsRemaining <= 0 || taskPuzzle == null) return false;
            taskStepsRemaining--;
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>Save current puzzle state for undo</summary>
        public void SaveForUndo(int handIndex, HanoiPuzzle puzzle)
        {
            if (puzzle == null) return;
            lastHandIndex = handIndex;
            lastPuzzleState = new HanoiPuzzle(puzzle.diskCount);
            lastPuzzleState.targetPeg = puzzle.targetPeg;
            lastPuzzleState.peg0 = new List<int>(puzzle.peg0);
            lastPuzzleState.peg1 = new List<int>(puzzle.peg1);
            lastPuzzleState.peg2 = new List<int>(puzzle.peg2);
        }

        /// <summary>Undo last move, returns true if successful</summary>
        public bool UndoMove()
        {
            if (lastPuzzleState == null || lastHandIndex < -1) return false;
            HanoiPuzzle puzzle;
            if (lastHandIndex == -1)
            {
                // Undo task panel move — need task steps
                if (taskStepsRemaining <= 0) return false;
                taskStepsRemaining++;
                puzzle = taskPuzzle;
            }
            else
            {
                if (stepsRemaining <= 0) return false;
                stepsRemaining--;
                puzzle = handPuzzles[lastHandIndex];
            }
            if (puzzle == null) return false;
            puzzle.peg0 = new List<int>(lastPuzzleState.peg0);
            puzzle.peg1 = new List<int>(lastPuzzleState.peg1);
            puzzle.peg2 = new List<int>(lastPuzzleState.peg2);
            puzzle.targetPeg = lastPuzzleState.targetPeg;
            lastPuzzleState = null;
            lastHandIndex = -2;
            OnStateChanged?.Invoke();
            return true;
        }

        public void OnTaskPuzzleCompleted()
        {
            if (taskCardInstance == null || taskPuzzle == null) return;

            SimpleAudio.Instance?.PlayComplete();
            string log = EffectManager.Execute(taskCardInstance, this, taskCardInstance.element, taskCardInstance.towerLevel);
            AddBattleLog($"[任务] {log}");

            // Save progress
            taskPuzzle.SaveToCardData(taskCardInstance);
            GameManager.Instance.taskSteps = taskStepsRemaining;
            SaveManager.Save(GameManager.Instance);

            // New random task
            taskPuzzle.GenerateRandomState();
            taskPuzzle.SaveToCardData(taskCardInstance);
            AddBattleLog("任务牌已更新，继续挑战吧！");

            OnTaskCompleted?.Invoke();
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Refresh: discard current hand, redraw, recalculate steps.
        /// </summary>
        public void RefreshHand()
        {
            if (refreshCharges <= 0) return;

            refreshCharges--;
            DrawNewHand();

            int baseSteps = 0;
            foreach (var puzzle in handPuzzles)
            {
                if (puzzle != null && puzzle.diskCount < 8) baseSteps += puzzle.OptimalSteps;
            }
            stepsRemaining = Mathf.CeilToInt(baseSteps * GameManager.Instance.stepMultiplier);
            AddBattleLog($"刷新手牌，剩余刷新: {refreshCharges}/{maxRefreshCharges}，步数: {stepsRemaining}");

            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Called when a Hanoi puzzle at handIndex is completed.
        /// Executes the card effect and regenerates the puzzle.
        /// </summary>
        public void OnPuzzleCompleted(int handIndex)
        {
            if (_battleEnded) return;
            if (currentHand[handIndex] == null) return;

            var card = currentHand[handIndex];
            var puzzle = handPuzzles[handIndex];

            SimpleAudio.Instance?.PlayComplete();

            // Execute effect (combo from PREVIOUS card applies here via GetDamageMultiplier)
            float savedCombo = comboMultiplier;
            if (savedCombo > 1f && card.effectType != EffectType.ComboChain && card.effectType != EffectType.MidComboChain && card.effectType != EffectType.HeavyComboChain)
            {
                AddBattleLog($"连携生效！伤害+{(int)((savedCombo-1f)*100)}%");
            }
            string fatigueKey = $"{card.effectType}_{card.towerLevel}";
            RecordCardCompletion(fatigueKey);
            string log = EffectManager.Execute(card, this, card.element, card.towerLevel, fatigueKey);

            // After execution, decrement combo charges
            if (comboCharges > 0)
            {
                comboCharges--;
                if (comboCharges <= 0) comboMultiplier = 1f;
            }
            AddBattleLog($"[{card.towerLevel}层{card.element}] {log}");

            // Elemental reaction
            if (!card.isTaskCard)
            {
                var reaction = ElementReactions.TryReact(card.element, this);
                if (reaction.bonusDmg > 0) DealDamageToEnemy(reaction.bonusDmg, true);
                if (reaction.shield > 0) AddShield(reaction.shield);
                if (reaction.heal > 0) HealPlayer(reaction.heal);
                if (reaction.stun > 0) enemyStunTurns = reaction.stun;
                if (reaction.burn > 0) { enemyPoisonDamage = Mathf.CeilToInt(reaction.burn * 1.5f); enemyPoisonTurns = reaction.burn; }
                if (reaction.extraSteps > 0) stepsRemaining += reaction.extraSteps;
                if (reaction.extraDraw > 0) DrawCards(reaction.extraDraw);
                if (reaction.bloom)
                {
                    nextTurnDamageBonus += Mathf.CeilToInt((baseATK + GameManager.Instance.permanentATKBonus) * 0.6f);
                    AddBattleLog("草核将在下回合爆炸！");
                }
            }

            // For task card: save progress persistently, then regenerate
            if (card.isTaskCard)
            {
                puzzle.SaveToCardData(card);
                puzzle.SaveToCardData(taskCardInstance);
                puzzle.GenerateRandomState();
                puzzle.SaveToCardData(taskCardInstance);
                SaveManager.Save(GameManager.Instance);
                AddBattleLog("任务牌进度已保存！");
            }
            else
            {
                puzzle.GenerateRandomState();
            }

            OnCardCompleted?.Invoke(handIndex);

            // Check enemy death
            if (enemy != null && enemy.currentHP <= 0)
            {
                EndBattle(true);
                return;
            }

            // Extra turn from this card?
            if (extraTurnPending)
            {
                extraTurnPending = false;
                // Extra turn: enemy doesn't act, player gets another full turn
                AddBattleLog("获得额外回合！");
                StartPlayerTurn();
            }
        }

        /// <summary>
        /// Player ends their turn voluntarily.
        /// </summary>
        public void EndPlayerTurn()
        {
            if (!_playerTurn || _battleEnded) return;

            // Save task card progress
            if (taskPuzzle != null && taskCardInstance != null)
            {
                taskPuzzle.SaveToCardData(taskCardInstance);
                GameManager.Instance.taskSteps = taskStepsRemaining;
                SaveManager.Save(GameManager.Instance);
            }

            _playerTurn = false;
            AddBattleLog("玩家回合结束");
            OnStateChanged?.Invoke();

            // Enemy turn
            Invoke(nameof(EnemyTurn), 0.5f);
        }

        private void EnemyTurn()
        {
            if (_battleEnded) return;

            // Check stun
            if (enemyStunTurns > 0)
            {
                enemyStunTurns--;
                AddBattleLog($"{enemy.enemyName}被眩晕，跳过行动！");
                enemy.DecideAction(this); // decide next intent anyway
            }
            else
            {
                // Execute the previously-decided action, then decide next
                enemy.ExecuteAction(this);
                enemy.DecideAction(this);
                OnStateChanged?.Invoke();
            }

            // Reset per-turn statuses
            playerWeakenTurns = Mathf.Max(0, playerWeakenTurns - 1);
            if (playerWeakenTurns <= 0) playerWeakenPercent = 0;

            enemyWeakenTurns = Mathf.Max(0, enemyWeakenTurns - 1);
            if (enemyWeakenTurns <= 0) enemyWeakenPercent = 0;

            if (comboCharges <= 0) comboMultiplier = 1f;

            // Check player death
            if (playerHP <= 0)
            {
                EndBattle(false);
                return;
            }

            // Check enemy death (poison)
            if (enemy.currentHP <= 0)
            {
                EndBattle(true);
                return;
            }

            OnStateChanged?.Invoke();

            // Start next player turn
            Invoke(nameof(StartPlayerTurn), 0.8f);
        }

        public int DealDamageToEnemy(int rawDamage, bool pierce, Element? attackElement = null)
        {
            if (enemy == null) return 0;

            int remaining = rawDamage;
            bool crit = false;

            if (attackElement != null && enemy.weakness == attackElement)
            {
                remaining *= 2;
                crit = true;
                AddBattleLog($"元素克制！{attackElement}→{enemy.enemyName}，伤害×2！");
            }

            if (!pierce && enemy.currentShield > 0)
            {
                int absorbed = Mathf.Min(enemy.currentShield, remaining);
                enemy.currentShield -= absorbed;
                remaining -= absorbed;
            }

            enemy.currentHP -= remaining;
            if (enemy.currentHP < 0) enemy.currentHP = 0;
            return crit ? rawDamage * 2 : rawDamage;
        }

        public void AddShield(int amount)
        {
            playerShield += amount;
        }

        public int HealPlayer(int amount)
        {
            int maxHP = playerMaxHP + GameManager.Instance.maxHPBonus;
            int before = playerHP;
            playerHP = Mathf.Min(maxHP, playerHP + amount);
            return playerHP - before;
        }

        public int DrawCards(int count)
        {
            int drawn = 0;
            for (int i = 0; i < 3; i++)
            {
                if (currentHand[i] == null && drawn < count)
                {
                    var cards = GameManager.Instance.Deck.DrawHand(1, taskCardInstance);
                    if (cards.Count > 0)
                    {
                        currentHand[i] = cards[0];
                        handPuzzles[i] = new HanoiPuzzle(currentHand[i].towerLevel);
                        if (currentHand[i].isTaskCard && taskCardInstance.peg0State.Count > 0)
                        {
                            handPuzzles[i].LoadFromCardData(taskCardInstance);
                        }
                        drawn++;
                    }
                }
            }
            return drawn;
        }

        /// <summary>
        /// Calculate damage multiplier from all sources.
        /// </summary>
        public float GetDamageMultiplier()
        {
            float mult = 1f;
            // ATK scaling: first 5 atk = 100%, each extra atk = 50% effective (diminishing)
            int totalATK = baseATK + GameManager.Instance.permanentATKBonus;
            float atkMult = Mathf.Min(totalATK, 5f) + Mathf.Max(0f, totalATK - 5f) * 0.5f;
            mult *= (1f + atkMult * 0.15f); // each effective ATK = +15% base damage

            if (playerWeakenTurns > 0) mult *= (1f - playerWeakenPercent);
            if (comboCharges > 0 && comboMultiplier > 1f) mult *= comboMultiplier;
            if (damageBonus > 0) { mult *= (1f + damageBonus); damageBonus = 0f; }
            if (nextTurnDamageBonus > 0) { mult *= (1f + nextTurnDamageBonus); nextTurnDamageBonus = 0f; }
            return mult;
        }

        public void AddBattleLog(string msg)
        {
            _battleLog.Add(msg);
            if (_battleLog.Count > 20) _battleLog.RemoveAt(0);
            OnBattleLog?.Invoke(msg);
        }

        public List<string> GetBattleLog() => _battleLog;

        public bool IsPlayerTurn() => _playerTurn;
        public bool IsBattleEnded() => _battleEnded;

        private void EndBattle(bool victory)
        {
            _battleEnded = true;
            // Save persistent HP
            GameManager.Instance.persistentHP = playerHP;
            if (taskPuzzle != null && taskCardInstance != null)
            {
                taskPuzzle.SaveToCardData(taskCardInstance);
                GameManager.Instance.taskSteps = taskStepsRemaining;
            }
            if (victory)
            {
                AddBattleLog("战斗胜利！");
                GameManager.Instance.OnBattleWon();
            }
            else
            {
                AddBattleLog("你被击败了...");
                GameManager.Instance.OnBattleLost();
            }
        }
    }
}
