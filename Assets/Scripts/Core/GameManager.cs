using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState { MainMenu, Map, Battle, CardReward, Event, Chest, Rest, GameOver }

        public GameState currentState;

        [Header("Permanent Stats")]
        public float stepMultiplier = 1.0f;
        public int permanentATKBonus;
        public int permanentThorns;
        public int maxHPBonus;
        public int currentStage = 1;
        public int currentMapStage;
        public int mora;
        public int taskSteps;
        public int persistentHP;
        public int maxPlayerHP = 60; // base max HP reference for events

        [Header("Deck")]
        public DeckManager Deck = new DeckManager();

        [Header("Map")]
        public MapData CurrentMap;

        [Header("UI Panels")]
        public GameObject mainMenuPanel;
        public GameObject mapPanel;
        public GameObject battlePanel;
        public GameObject cardRewardPanel;
        public GameObject eventPanel;
        public GameObject chestPanel;
        public GameObject restPanel;
        public GameObject shopPanel;
        public GameObject gameOverPanel;
        public GameObject escMenuPanel;

        private BattleManager _battleManager;
        private string _pendingEventText;
        private System.Action _pendingEventOption;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ShowMainMenu();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && escMenuPanel != null)
            {
                bool active = !escMenuPanel.activeSelf;
                if (active) escMenuPanel.transform.SetAsLastSibling();
                escMenuPanel.SetActive(active);
            }
        }

        #region State Transitions

        public void ShowMainMenu()
        {
            SetState(GameState.MainMenu);
            HideAll();
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
            BGMPlayer.Instance?.Play(BGMPlayer.Theme.MainMenu);
        }

        public void StartNewGame()
        {
            SaveManager.DeleteSave();
            Deck.InitStartingDeck();
            stepMultiplier = 1.0f;
            permanentATKBonus = 0;
            maxHPBonus = 0;
            currentStage = 1;
            currentMapStage = 0;
            CurrentMap = MapData.Generate(currentMapStage);
            ShowMap();
        }

        public void ContinueGame()
        {
            var data = SaveManager.Load();
            if (data != null)
            {
                SaveManager.ApplySaveData(this, data);
                CurrentMap = MapData.Generate(currentStage);
                ShowMap();
            }
            else StartNewGame();
        }

        public void ShowMap()
        {
            if (_battleManager != null) { Destroy(_battleManager.gameObject); _battleManager = null; }
            SetState(GameState.Map);
            HideAll();
            if (mapPanel) { mapPanel.SetActive(true); }
            BGMPlayer.Instance?.PlayRegion(currentMapStage switch { 0 => "蒙德", 1 => "璃月", _ => "须弥" });
        }

        public void StartBattle(bool isElite = false)
        {
            // Create BattleManager BEFORE activating panel
            if (_battleManager != null) { Destroy(_battleManager.gameObject); _battleManager = null; }
            var bmObj = new GameObject("BattleManager");
            bmObj.transform.SetParent(transform);
            _battleManager = bmObj.AddComponent<BattleManager>();
            _battleManager.isEliteBattle = isElite;

            SetState(GameState.Battle);
            HideAll();
            if (battlePanel) battlePanel.SetActive(true);
        }

        public void StartBossBattle()
        {
            if (_battleManager != null) { Destroy(_battleManager.gameObject); _battleManager = null; }
            var bmObj = new GameObject("BattleManager");
            bmObj.transform.SetParent(transform);
            _battleManager = bmObj.AddComponent<BattleManager>();
            _battleManager.isBossBattle = true;

            SetState(GameState.Battle);
            HideAll();
            if (battlePanel) battlePanel.SetActive(true);
        }

        public void OnBattleWon()
        {
            StartCoroutine(BattleWonRoutine());
        }

        private IEnumerator BattleWonRoutine()
        {
            yield return new WaitForSeconds(1.5f);

            // Battle reward: always give Mora
            int baseGold = 30 + currentMapStage * 20;
            if (CurrentMap?.currentNode?.type == MapNodeType.Elite) baseGold = (int)(baseGold * 1.8f);
            else if (CurrentMap?.currentNode?.type == MapNodeType.Boss) baseGold = (int)(baseGold * 3f);
            mora += baseGold + Random.Range(0, 30);
            SaveManager.Save(this);

            // Boss beaten → next map stage
            if (CurrentMap != null && CurrentMap.currentNode != null && CurrentMap.currentNode.type == MapNodeType.Boss)
            {
                currentMapStage++;
                if (currentMapStage >= 3)
                {
                    // Game complete! Show victory
                    SetState(GameState.GameOver);
                    HideAll();
                    if (gameOverPanel) gameOverPanel.SetActive(true);
                    var overUI = gameOverPanel?.GetComponent<GameOverUI>();
                    if (overUI != null) { overUI.resultText.text = "提瓦特之旅完成！"; overUI.statsText.text = $"最终步数倍率: ×{stepMultiplier:F2}\n卡组: {Deck.cards.Count}张"; }
                    yield break;
                }
                // Full heal
                maxHPBonus = 0;
                // Generate new map with harder enemies
                CurrentMap = MapData.Generate(currentMapStage);
                ShowReward(4); // boss gives 4 choices: levels 5-7 only
            }
            else
            {
                ShowReward(3);
            }
        }

        public void OnBattleLost()
        {
            StartCoroutine(BattleLostRoutine());
        }

        private IEnumerator BattleLostRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            SetState(GameState.GameOver);
            HideAll();
            if (gameOverPanel) gameOverPanel.SetActive(true);
            SaveManager.DeleteSave();
        }

        public void ShowReward(int count)
        {
            SetState(GameState.CardReward);
            HideAll();
            if (cardRewardPanel) cardRewardPanel.SetActive(true);
            var ui = cardRewardPanel?.GetComponent<CardRewardUI>();
            ui?.ShowRewards(currentStage, count);
        }

        public void OnCardSelected(CardData chosen)
        {
            Deck.AddCard(chosen);
            currentStage++;
            SaveManager.Save(this);
            ShowMap();
        }

        public void OpenChest()
        {
            var card = Deck.GenerateRewardChoices(currentStage)[0];
            Deck.AddCard(card);
            int gold = 50 + Random.Range(0, 51) + currentMapStage * 30;
            mora += gold;
            SaveManager.Save(this);

            SetState(GameState.Chest);
            HideAll();
            if (chestPanel) chestPanel.SetActive(true);
            var chestUI = chestPanel?.GetComponent<ChestUI>();
            if (chestUI != null) chestUI.Show(card, gold);
            else StartCoroutine(DelayedShowMap());
        }

        public void OnChestDone() { ShowMap(); }

        public void OpenShop()
        {
            SetState(GameState.Event);
            HideAll();
            if (shopPanel) shopPanel.SetActive(true);
            var shopUI = shopPanel?.GetComponent<ShopUI>();
            if (shopUI != null) shopUI.Show();
            else StartCoroutine(DelayedShowMap());
        }

        public void OnShopDone() { ShowMap(); }

        public void SaveManager_() { SaveManager.Save(this); }

        public void DoRest()
        {
            int heal = 10 + currentMapStage * 5;
            maxHPBonus += heal;
            SetState(GameState.Rest);
            HideAll();
            if (restPanel) restPanel.SetActive(true);
            var restUI = restPanel?.GetComponent<RestUI>();
            if (restUI != null) restUI.Show(heal, stepMultiplier, permanentATKBonus);
            else StartCoroutine(DelayedShowMap());
            SaveManager.Save(this);
        }

        public void OnRestComplete() { ShowMap(); }

        public void TriggerEvent()
        {
            var events = new List<(string text, (string, System.Action)[] choices)>
            {
                ("派蒙发现了宝箱！", new (string, System.Action)[] {
                    ("打开宝箱 (攻击力+1)", () => { permanentATKBonus += 1; }),
                    ("谨慎观察 (无变化)", () => {})
                }),
                ("钟离在璃月港讲述古老契约...", new (string, System.Action)[] {
                    ("接受契约 (步数倍率-0.05, 攻+2)", () => { stepMultiplier = Mathf.Max(0.5f, stepMultiplier-0.05f); permanentATKBonus += 2; }),
                    ("婉拒 (步数倍率+0.02)", () => { stepMultiplier += 0.02f; })
                }),
                ("温迪在蒙德城弹唱...", new (string, System.Action)[] {
                    ("驻足倾听 (生命上限+5)", () => { maxHPBonus += 5; }),
                    ("请TA喝一杯 (获得卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("匆匆赶路 (无事发生)", () => {})
                }),
                ("须弥教令院找到稀有论文！", new (string, System.Action)[] {
                    ("精读论文 (卡牌+倍率+0.02)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); stepMultiplier+=0.02f; }),
                    ("出售论文 (攻击力+2)", () => { permanentATKBonus += 2; })
                }),
                ("愚人众设下了陷阱！", new (string, System.Action)[] {
                    ("正面迎战 (攻+1, 生命上限-5)", () => { permanentATKBonus+=1; maxHPBonus=Mathf.Max(-30,maxHPBonus-5); }),
                    ("绕道而行 (生命上限+3)", () => { maxHPBonus+=3; })
                }),
                ("纳西妲在梦中低语...", new (string, System.Action)[] {
                    ("接受智慧祝福 (倍率+0.04)", () => { stepMultiplier += 0.04f; }),
                    ("请求帮助 (获得2张卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); })
                }),
                ("神里绫人邀请你参加茶会", new (string, System.Action)[] {
                    ("参加茶会 (卡牌+倍率+0.01)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); stepMultiplier+=0.01f; }),
                    ("切磋剑术 (攻击力+3)", () => { permanentATKBonus += 3; }),
                    ("拒绝邀请 (倍率+0.02)", () => { stepMultiplier += 0.02f; })
                }),
                ("七七发现了古老的药方！", new (string, System.Action)[] {
                    ("配制药方 (恢复生命上限30%)", () => { persistentHP = Mathf.Min(persistentHP + Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f), maxPlayerHP+maxHPBonus); }),
                    ("出售药方 (获得2张卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); })
                }),
                ("流浪的剑客请求与你比试", new (string, System.Action)[] {
                    ("全力应战 (攻+2, 扣20%生命)", () => { permanentATKBonus+=2; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f)); }),
                    ("携手同行 (获得卡牌+治疗10%)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f), maxPlayerHP+maxHPBonus); }),
                    ("果断拒绝 (无事发生)", () => {})
                }),
                ("神秘的炼金术士在兜售药剂", new (string, System.Action)[] {
                    ("购买力量药剂 (攻+3, 生命-15%)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f)); }),
                    ("购买生命药剂 (恢复40%生命)", () => { persistentHP = Mathf.Min(persistentHP + Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.4f), maxPlayerHP+maxHPBonus); }),
                    ("购买神秘药剂 (随机卡牌×3)", () => { for(int i=0;i<3;i++) Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); })
                }),
                ("凝光提出了商业合作", new (string, System.Action)[] {
                    ("投资矿业 (倍率+0.03, 攻+1)", () => { stepMultiplier+=0.03f; permanentATKBonus+=1; }),
                    ("投资贸易 (摩拉+80, 卡牌×2)", () => { mora+=80; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("婉拒投资 (无损)", () => {})
                }),
                ("刻晴请你帮忙解决璃月事务", new (string, System.Action)[] {
                    ("加班处理 (攻+2, 倍率+0.01, 扣25%HP)", () => { permanentATKBonus+=2; stepMultiplier+=0.01f; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f)); }),
                    ("推荐甘雨 (攻+1, 无损)", () => { permanentATKBonus+=1; }),
                    ("推给钟离 (无事发生)", () => {})
                }),
            };

            var e = events[Random.Range(0, events.Count)];
            SetState(GameState.Event);
            HideAll();
            if (eventPanel) eventPanel.SetActive(true);

            var eventUI = eventPanel?.GetComponent<EventUI>();
            if (eventUI != null)
            {
                var choices = new (string, System.Action)[e.choices.Length];
                for (int i = 0; i < e.choices.Length; i++)
                {
                    var captured = e.choices[i].Item2;
                    choices[i] = (e.choices[i].Item1, () => { captured(); SaveManager.Save(this); ShowMap(); });
                }
                eventUI.Show(e.text, choices);
            }
            else
            {
                e.choices[0].Item2();
                SaveManager.Save(this);
                StartCoroutine(DelayedShowMap());
            }
        }

        private IEnumerator DelayedShowMap()
        {
            yield return new WaitForSeconds(1.5f);
            ShowMap();
        }

        public void ReturnToMenu()
        {
            if (_battleManager != null) { Destroy(_battleManager.gameObject); _battleManager = null; }
            ShowMainMenu();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        private void SetState(GameState s) => currentState = s;

        private void HideAll()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (mapPanel) mapPanel.SetActive(false);
            if (battlePanel) battlePanel.SetActive(false);
            if (cardRewardPanel) cardRewardPanel.SetActive(false);
            if (eventPanel) eventPanel.SetActive(false);
            if (chestPanel) chestPanel.SetActive(false);
            if (restPanel) restPanel.SetActive(false);
            if (shopPanel) shopPanel.SetActive(false);
            if (gameOverPanel) gameOverPanel.SetActive(false);
        }

        public BattleManager GetBattleManager() => _battleManager;
    }
}
