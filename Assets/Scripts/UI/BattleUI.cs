using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    /// <summary>
    /// Manages the battle UI: player/enemy stats, buttons, battle log, and Hanoi panels.
    /// Attach to the BattlePanel root GameObject.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("Player UI")]
        public Image playerHPBar;
        public Text playerHPText;
        public Image playerShieldBar;
        public Text playerShieldText;
        public Text playerATKText;
        public Text stepMultiplierText;

        [Header("Enemy UI")]
        public Text enemyNameText;
        public Image enemyPortrait;
        public Image enemyHPBar;
        public Text enemyHPText;
        public Image enemyShieldBar;
        public Text enemyShieldText;
        public Text enemyIntentText;
        public Text enemyPoisonText;

        [Header("Turn Controls")]
        public Text stepsRemainingText;
        public Text refreshText;
        public Button endTurnButton;
        public Button refreshButton;
        public Button deckButton;
        public GameObject deckViewerPanel;
        public GameObject statsPanel;

        [Header("Battle Log")]
        public Text battleLogText;
        public GameObject logOverlay;
        public Button logArrowBtn;
        public ScrollRect logScrollRect;

        [Header("Hanoi Panels")]
        public HanoiUI[] hanoiUIs;

        [Header("Announcements")]
        public Text turnAnnounceText;
        public Image playerAvatar;

        [Header("Task Overlay")]
        public GameObject taskOverlay;
        public HanoiUI taskHanoiUI;
        public Text taskStepText;
        public Button taskButton;
        public Button taskCloseButton;

        private BattleManager _battle;
        private float _updateTimer;
        private bool _wired;
        private System.Action<string, Color> _onReaction;
        private System.Action<int, bool> _onDamage;
        private System.Action<int> _onFlash;

        private void OnEnable()
        {
            _battle = GameManager.Instance?.GetBattleManager();
            if (_battle == null) { Debug.LogError("[BattleUI] No BattleManager!"); return; }

            if (hanoiUIs == null || hanoiUIs.Length == 0)
                hanoiUIs = GetComponentsInChildren<HanoiUI>();

            // Wire buttons once
            if (!_wired)
            {
                _wired = true;
                if (endTurnButton != null) endTurnButton.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); _battle.EndPlayerTurn(); });
                if (refreshButton != null) refreshButton.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); _battle.RefreshHand(); RebuildHanoiPanels(); });
            }
            else
            {
                if (endTurnButton != null) { endTurnButton.onClick.RemoveAllListeners(); endTurnButton.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); _battle.EndPlayerTurn(); }); }
                if (refreshButton != null) { refreshButton.onClick.RemoveAllListeners(); refreshButton.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); _battle.RefreshHand(); RebuildHanoiPanels(); }); }
            }

            _battle.OnBattleLog += OnLogMessage;
            _battle.OnStateChanged += OnStateHandler;
            _onReaction = (name, color) => ShowReaction(name, color);
            _onDamage = (dmg, crit) => ShowDamageNumber(dmg, crit);
            _onFlash = (idx) => FlashPanel(idx);
            _battle.OnReactionTriggered += _onReaction;
            _battle.OnDamageDealt += _onDamage;
            _battle.OnFlashPanel += _onFlash;

            _battle.BeginBattle();
            RebuildHanoiPanels();
            InitTaskPanel();
            RefreshAllUI();

            // BGM based on enemy
            if (_battle.enemy != null)
            {
                if (_battle.isBossBattle)
                    BGMPlayer.Instance?.PlayBoss(_battle.enemy.region);
                else
                    BGMPlayer.Instance?.PlayRegion(_battle.enemy.region);
            }

            // Apply Genshin-style UI textures at runtime
            ApplyUIStyles();

            // Task overlay button wiring
            // Wire player avatar to open stats panel
            if (playerAvatar != null)
            {
                var abtn = playerAvatar.GetComponent<Button>();
                if (abtn != null)
                {
                    abtn.onClick.RemoveAllListeners();
                    abtn.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); ShowStatsPanel(); });
                }
            }

            // Show turn announcement
            ShowAnnouncement("你的回合");

            // Wire enemy portrait to open stats
            if (enemyPortrait != null)
            {
                var pbtn = enemyPortrait.GetComponent<Button>();
                if (pbtn != null)
                {
                    pbtn.onClick.RemoveAllListeners();
                    pbtn.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); ShowStatsPanel(); });
                }
            }
            // Log arrow toggles overlay
            if (logArrowBtn != null)
            {
                logArrowBtn.onClick.RemoveAllListeners();
                logArrowBtn.onClick.AddListener(() =>
                {
                    if (logOverlay != null)
                    {
                        bool show = !logOverlay.activeSelf;
                        logOverlay.SetActive(show);
                        if (show) { logOverlay.transform.SetAsLastSibling(); UpdateBattleLogFull(); }
                    }
                });
            }
            // Log overlay close button
            var logClose = logOverlay?.transform.Find("LogCloseBtn");
            if (logClose != null)
            {
                var cb = logClose.GetComponent<Button>();
                if (cb != null) { cb.onClick.RemoveAllListeners(); cb.onClick.AddListener(() => logOverlay.SetActive(false)); }
            }
            if (deckButton != null)
            {
                deckButton.onClick.RemoveAllListeners();
                deckButton.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); ShowDeckViewer(); });
            }
            if (taskButton != null)
            {
                taskButton.onClick.RemoveAllListeners();
                taskButton.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); ShowTaskOverlay(); });
            }
            if (taskCloseButton != null)
            {
                taskCloseButton.onClick.RemoveAllListeners();
                taskCloseButton.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); HideTaskOverlay(); });
            }
            if (taskOverlay != null) taskOverlay.SetActive(false);

            // Set region background on the panel's own Image (after enemy is created)
            if (_battle.enemy != null)
            {
                var bg = PortraitGen.BattleBg(_battle.enemy.region, 1200, 800);
                if (bg != null)
                {
                    var panelImg = GetComponent<Image>();
                    if (panelImg != null) { panelImg.sprite = bg; panelImg.color = Color.white; }
                }
            }

            if (!TutorialUI.IsDone)
                gameObject.AddComponent<TutorialUI>();
        }

        private void OnDisable()
        {
            if (_battle != null)
            {
                _battle.OnBattleLog -= OnLogMessage;
                _battle.OnStateChanged -= OnStateHandler;
                if (_onReaction != null) _battle.OnReactionTriggered -= _onReaction;
                if (_onDamage != null) _battle.OnDamageDealt -= _onDamage;
                if (_onFlash != null) _battle.OnFlashPanel -= _onFlash;
            }
        }

        void OnStateHandler()
        {
            bool changed = false;
            for (int i = 0; i < 3 && i < hanoiUIs.Length; i++)
                if (hanoiUIs[i] != null && hanoiUIs[i].Puzzle != _battle.handPuzzles[i]) { changed = true; break; }
            if (changed) RebuildHanoiPanels();
        }

        private void Update()
        {
            if (_battle == null) return;

            _updateTimer -= Time.deltaTime;
            if (_updateTimer <= 0f)
            {
                _updateTimer = 0.15f;
                RefreshAllUI();
            }
        }

        private void RebuildHanoiPanels()
        {
            if (hanoiUIs == null) return;

            for (int i = 0; i < hanoiUIs.Length && i < 3; i++)
            {
                if (hanoiUIs[i] != null && _battle.handPuzzles[i] != null)
                {
                    hanoiUIs[i].Initialize(_battle.handPuzzles[i], _battle.currentHand[i], i, _battle);
                }
            }
        }

        void ApplyUIStyles()
        {
            if (playerAvatar != null)
            {
                var avTex = Resources.Load<Texture2D>("avatar_traveler");
                if (avTex != null) { playerAvatar.sprite = Sprite.Create(avTex, new Rect(0,0,avTex.width,avTex.height), Vector2.one*0.5f); playerAvatar.color = Color.white; }
            }
        }

        private void InitTaskPanel()
        {
            if (taskHanoiUI == null)
                taskHanoiUI = GetComponentsInChildren<HanoiUI>().FirstOrDefault(h => h.handIndex == -2);

            if (taskHanoiUI != null && _battle.taskPuzzle != null && _battle.taskCardInstance != null)
                taskHanoiUI.Initialize(_battle.taskPuzzle, _battle.taskCardInstance, -2, _battle);
        }

        void ShowTaskOverlay()
        {
            if (taskOverlay != null)
            {
                taskOverlay.transform.SetAsLastSibling();
                taskOverlay.SetActive(true);
                // Re-init task panel each time it opens
                if (taskHanoiUI != null && _battle.taskPuzzle != null)
                    taskHanoiUI.Initialize(_battle.taskPuzzle, _battle.taskCardInstance, -2, _battle);
                if (taskStepText != null)
                    taskStepText.text = $"任务步数: {_battle.taskStepsRemaining}";
            }
        }

        void HideTaskOverlay()
        {
            if (taskOverlay != null)
                taskOverlay.SetActive(false);
            // Save task state
            if (_battle.taskPuzzle != null && _battle.taskCardInstance != null)
            {
                _battle.taskPuzzle.SaveToCardData(_battle.taskCardInstance);
                GameManager.Instance.taskSteps = _battle.taskStepsRemaining;
            }
        }

        void ShowAnnouncement(string msg)
        {
            if (turnAnnounceText != null)
            {
                turnAnnounceText.text = msg;
                turnAnnounceText.gameObject.SetActive(true);
                turnAnnounceText.canvasRenderer.SetAlpha(1f);
                turnAnnounceText.transform.localScale = Vector3.one * 0.5f;
                // Scale up + fade
                LeanTween.scale(turnAnnounceText.gameObject, Vector3.one * 1.2f, 0.3f).setEaseOutBack();
                LeanTween.alphaText(turnAnnounceText.rectTransform, 0f, 1.8f).setDelay(0.5f);
                Invoke(nameof(HideAnnouncement), 2.5f);
            }
        }
        void HideAnnouncement() { if (turnAnnounceText != null) turnAnnounceText.gameObject.SetActive(false); }

        /// <summary>Show big elemental reaction announcement in center screen</summary>
        public void ShowReaction(string reactionName, Color elemColor)
        {
            if (turnAnnounceText != null)
            {
                turnAnnounceText.text = $"【{reactionName}】";
                turnAnnounceText.color = elemColor;
                turnAnnounceText.gameObject.SetActive(true);
                turnAnnounceText.canvasRenderer.SetAlpha(1f);
                turnAnnounceText.transform.localScale = Vector3.one * 0.3f;
                LeanTween.scale(turnAnnounceText.gameObject, Vector3.one * 1.5f, 0.4f).setEaseOutElastic();
                LeanTween.alphaText(turnAnnounceText.rectTransform, 0f, 2.5f).setDelay(1.2f);
                Invoke(nameof(HideAnnouncement), 3f);
            }
        }

        /// <summary>Floating damage number at enemy position</summary>
        public void ShowDamageNumber(int dmg, bool crit)
        {
            var go = new GameObject("DmgNum", typeof(Text));
            go.transform.SetParent(transform, false);
            var t = go.GetComponent<Text>();
            t.text = crit ? $"{dmg}!!" : $"{dmg}";
            t.fontSize = crit ? 28 : 20;
            t.color = crit ? new Color(1f, 0.5f, 0f) : Color.white;
            t.font = FontHelper.GetFont(20);
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.7f, 0.65f);
            rt.sizeDelta = new Vector2(120, 40);
            rt.anchoredPosition = new Vector2(Random.Range(-20, 20), Random.Range(-10, 30));
            LeanTween.moveY(rt, rt.anchoredPosition.y + 60f, 1f).setEaseOutQuad();
            LeanTween.alphaText(rt, 0f, 0.8f).setDelay(0.3f);
            Destroy(go, 1.5f);
        }

        /// <summary>Gold flash on Hanoi panel completion</summary>
        public void FlashPanel(int handIndex)
        {
            if (hanoiUIs != null && handIndex >= 0 && handIndex < hanoiUIs.Length && hanoiUIs[handIndex] != null)
            {
                var panel = hanoiUIs[handIndex].gameObject;
                var flash = new GameObject("Flash", typeof(Image));
                flash.transform.SetParent(panel.transform, false);
                var fi = flash.GetComponent<Image>();
                fi.color = new Color(1f, 0.84f, 0f, 0.5f);
                fi.raycastTarget = false;
                var frt = flash.GetComponent<RectTransform>();
                frt.anchorMin = frt.anchorMax = Vector2.one * 0.5f;
                frt.sizeDelta = new Vector2(400, 280);
                LeanTween.alpha(frt, 0f, 0.5f).setEaseOutQuad();
                Destroy(flash, 0.6f);
            }
        }

        void ShowStatsPanel()
        {
            if (statsPanel != null)
            {
                statsPanel.transform.SetAsLastSibling();
                statsPanel.SetActive(true);
                statsPanel.GetComponent<StatsPanelUI>()?.Show();
            }
        }

        void ShowDeckViewer()
        {
            if (deckViewerPanel != null)
            {
                deckViewerPanel.transform.SetAsLastSibling();
                deckViewerPanel.SetActive(true);
                deckViewerPanel.GetComponent<DeckViewerUI>()?.Refresh();
            }
        }

        void UpdateTaskSteps()
        {
            if (taskStepText != null && _battle != null)
                taskStepText.text = $"任务步数: {_battle.taskStepsRemaining}";
        }

        private void RefreshAllUI()
        {
            if (_battle == null) return;

            // Player stats
            int maxHP = _battle.playerMaxHP + GameManager.Instance.maxHPBonus;
            float hpPct = Mathf.Clamp01((float)_battle.playerHP / maxHP);
            if (playerHPBar != null)
            {
                playerHPBar.fillAmount = hpPct;
                playerHPBar.color = hpPct > 0.5f ? Color.green : (hpPct > 0.25f ? Color.yellow : Color.red);
            }
            if (playerHPText != null)
                playerHPText.text = $"HP: {_battle.playerHP}/{maxHP}";

            if (playerShieldBar != null)
                playerShieldBar.fillAmount = Mathf.Clamp01(_battle.playerShield / 50f);
            if (playerShieldText != null)
                playerShieldText.text = _battle.playerShield > 0 ? $"护盾: {_battle.playerShield}" : "";

            if (playerATKText != null)
            {
                int totalATK = _battle.baseATK + GameManager.Instance.permanentATKBonus;
                playerATKText.text = $"攻击力: {totalATK}";
            }

            if (stepMultiplierText != null)
                stepMultiplierText.text = $"步数倍率: ×{GameManager.Instance.stepMultiplier:F2}";

            // Enemy stats
            if (_battle.enemy != null)
            {
                if (enemyNameText != null)
                {
                    string elemTag = _battle.enemy.attachedElement != null ? $"【{_battle.enemy.attachedElement}】" : "";
                    enemyNameText.text = $"{elemTag}{_battle.enemy.enemyName}  [{_battle.enemy.region}]";
                }

                if (enemyPortrait != null)
                {
                    var sprite = PortraitGen.Generate(_battle.enemy.enemyName, _battle.enemy.region, 128, 128);
                    if (sprite != null)
                    {
                        enemyPortrait.sprite = sprite;
                        enemyPortrait.color = Color.white;
                    }
                }

                float ehpPct = Mathf.Clamp01((float)_battle.enemy.currentHP / _battle.enemy.maxHP);
                if (enemyHPBar != null)
                {
                    enemyHPBar.fillAmount = ehpPct;
                    enemyHPBar.color = ehpPct > 0.5f ? Color.red : (ehpPct > 0.25f ? new Color(1f, 0.3f, 0f) : Color.gray);
                }
                if (enemyHPText != null)
                    enemyHPText.text = $"HP: {_battle.enemy.currentHP}/{_battle.enemy.maxHP}";

                if (enemyShieldBar != null)
                    enemyShieldBar.fillAmount = Mathf.Clamp01(_battle.enemy.currentShield / 30f);
                if (enemyShieldText != null)
                    enemyShieldText.text = _battle.enemy.currentShield > 0 ? $"护盾: {_battle.enemy.currentShield}" : "";

                if (enemyIntentText != null)
                    enemyIntentText.text = _battle.IsPlayerTurn() ? (_battle.enemy.intentText ?? "思考中...") : "行动中...";

                if (enemyPoisonText != null)
                    enemyPoisonText.text = _battle.enemy.poisonTurns > 0
                        ? $"中毒 {_battle.enemy.poisonDamage}×{_battle.enemy.poisonTurns}回"
                        : "";
            }

            // Steps
            if (stepsRemainingText != null)
                stepsRemainingText.text = $"剩余步数: {_battle.stepsRemaining}";

            // Refresh
            if (refreshText != null)
                refreshText.text = $"刷新 ({_battle.refreshCharges}/{_battle.maxRefreshCharges})";

            if (refreshButton != null)
                refreshButton.interactable = _battle.refreshCharges > 0 && _battle.IsPlayerTurn();

            if (endTurnButton != null)
                endTurnButton.interactable = _battle.IsPlayerTurn();

            // Battle log
            UpdateBattleLog();

            // Task steps
            UpdateTaskSteps();

            // Status indicators
            UpdateStatusIndicators();
        }

        private readonly List<string> _displayedLog = new List<string>();
        private string _lastLogLine = "";

        private void OnLogMessage(string msg)
        {
            _lastLogLine = msg;
        }

        void UpdateBattleLogFull()
        {
            if (logOverlay == null || !logOverlay.activeSelf || _battle == null) return;
            var text = logOverlay.transform.Find("LogOverlayText")?.GetComponent<Text>();
            if (text == null) return;
            var log = _battle.GetBattleLog();
            var sb = new StringBuilder();
            for (int i = log.Count - 1; i >= 0; i--)
                sb.AppendLine(log[i]);
            text.text = sb.ToString();
        }

        private void UpdateBattleLog()
        {
            if (battleLogText == null || _battle == null) return;
            var log = _battle.GetBattleLog();
            string last = log.Count > 0 ? log[log.Count - 1] : "";
            if (last.Length > 30) last = last.Substring(0, 28) + "..";
            battleLogText.text = last;
        }

        private void UpdateStatusIndicators()
        {
            // Status effects could be shown as icons below player/enemy
            // For simplicity, they're reflected in the battle log
        }

        private void OnDestroy()
        {
            if (_battle != null)
                _battle.OnBattleLog -= OnLogMessage;
        }
    }
}
