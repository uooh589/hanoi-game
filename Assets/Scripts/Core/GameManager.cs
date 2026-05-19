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
        public int maxPlayerHP = 60;
        public List<ArtifactData> artifacts = new();

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

        public void StartCoopGame()
        {
            // Co-op: both players share the map and take turns in battle
            StartNewGame();
            Debug.Log("[GameManager] Co-op game started");
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

            // Battle reward: Mora + artifact chance
            int baseGold = 30 + currentMapStage * 20;
            bool isElite = CurrentMap?.currentNode?.type == MapNodeType.Elite;
            bool isBoss = CurrentMap?.currentNode?.type == MapNodeType.Boss;
            if (isElite) baseGold = (int)(baseGold * 1.8f);
            else if (isBoss) baseGold = (int)(baseGold * 3f);
            mora += baseGold + Random.Range(0, 30);

            // Artifact drop: elite 30%, boss 100%
            if (ArtifactData.CanAddMore && (isBoss || (isElite && Random.value < 0.3f)))
            {
                var arti = ArtifactData.RollRandom();
                artifacts.Add(arti);
            }
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
            int r = Random.Range(0, 6);
            string title; string effect; System.Action action;
            switch (r)
            {
                case 0: title="七天神像·治疗"; effect="恢复30%生命";
                    action = () => { persistentHP = Mathf.Min(persistentHP + Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f), maxPlayerHP+maxHPBonus); }; break;
                case 1: title="七天神像·净化"; effect="清除负面状态，获得15护盾";
                    action = () => { maxHPBonus += 5; }; break;
                case 2: title="七天神像·强化"; effect="永久攻击力+2";
                    action = () => { permanentATKBonus += 2; }; break;
                case 3: title="七天神像·启示"; effect="步数倍率+0.03";
                    action = () => { stepMultiplier += 0.03f; }; break;
                case 4: title="七天神像·锻造"; effect="获得2张随机卡牌";
                    action = () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }; break;
                default: title="七天神像·牺牲"; effect="扣除15%生命，攻击力+3";
                    action = () => { persistentHP = Mathf.Max(1, persistentHP - Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f)); permanentATKBonus += 3; }; break;
            }
            action();
            SetState(GameState.Rest);
            HideAll();
            if (restPanel) restPanel.SetActive(true);
            var restUI = restPanel?.GetComponent<RestUI>();
            if (restUI != null) restUI.Show(title, effect);
            else StartCoroutine(DelayedShowMap());
            SaveManager.Save(this);
        }

        public void OnRestComplete() { ShowMap(); }

        public void TriggerRemoveCard()
        {
            if (Deck.cards.Count <= 1) { ShowMap(); return; }
            int idx = Random.Range(0, Deck.cards.Count);
            var removed = Deck.cards[idx];
            Deck.cards.RemoveAt(idx);
            SetState(GameState.Event);
            HideAll();
            if (eventPanel) eventPanel.SetActive(true);
            var ui = eventPanel?.GetComponent<EventUI>();
            if (ui != null) ui.Show($"你删除了卡牌: {removed.effectDescription}", new (string, System.Action)[] { ("确认", () => { SaveManager.Save(this); ShowMap(); }) });
            else { SaveManager.Save(this); ShowMap(); }
        }

        public void TriggerEvent()
        {
            // 52+ events with meaningful choices
            var events = new List<(string, (string, System.Action)[])>
            {
                ("派蒙发现了宝箱！", new (string, System.Action)[] {
                    ("打开宝箱 (攻+1)", () => { permanentATKBonus+=1; }),
                    ("谨慎观察 (无损)", () => { {} }),
                }),
                ("温迪在蒙德城弹唱…", new (string, System.Action)[] {
                    ("驻足倾听 (HP上限+5)", () => { maxHPBonus+=5; }),
                    ("请TA喝一杯 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("匆匆赶路 (无事)", () => { {} }),
                }),
                ("琴团长需要处理紧急公务", new (string, System.Action)[] {
                    ("协助处理 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("派侦察骑士 (攻+1,卡牌)", () => { permanentATKBonus+=1; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("婉拒 (无损)", () => { {} }),
                }),
                ("安柏在训练新兵", new (string, System.Action)[] {
                    ("参加训练 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("演示箭术 (倍率+0.01,卡牌)", () => { stepMultiplier+=0.01f; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("旁观 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                }),
                ("凯亚透露了深渊教团情报", new (string, System.Action)[] {
                    ("深入调查 (攻+2,倍率+0.02)", () => { permanentATKBonus+=2; stepMultiplier+=0.02f; }),
                    ("上报骑士团 (摩拉+60)", () => { mora+=60; }),
                    ("保守秘密 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("丽莎在图书馆打瞌睡…", new (string, System.Action)[] {
                    ("帮忙整理书籍 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("请教魔法 (攻+1,卡牌)", () => { permanentATKBonus+=1; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("悄悄离开 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("迪卢克的酒馆有特价！", new (string, System.Action)[] {
                    ("喝一杯 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("打听情报 (攻+1,倍率+0.01)", () => { permanentATKBonus+=1; stepMultiplier+=0.01f; }),
                    ("帮忙打工 (摩拉+50)", () => { mora+=50; }),
                }),
                ("诺艾尔在打扫骑士团", new (string, System.Action)[] {
                    ("帮忙打扫 (攻+1,恢复15%HP)", () => { permanentATKBonus+=1; persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                    ("学习女仆技艺 (护盾+10)", () => { maxHPBonus+=3; }),
                    ("鼓励她 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("芭芭拉在教堂祈祷", new (string, System.Action)[] {
                    ("一起祈祷 (恢复30%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f),maxPlayerHP+maxHPBonus); }),
                    ("请求祝福 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("捐款 (摩拉-30,卡牌)", () => { mora=Mathf.Max(0,mora-30); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("雷泽在森林中嗅到了危险", new (string, System.Action)[] {
                    ("并肩作战 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("分享烤肉 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("教他说人话 (倍率+0.01)", () => { stepMultiplier+=0.01f; }),
                }),
                ("阿贝多在雪山写生", new (string, System.Action)[] {
                    ("帮忙找颜料 (攻+1,2卡牌)", () => { permanentATKBonus+=1; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("欣赏画作 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("讨论炼金术 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("优菈在练习剑舞", new (string, System.Action)[] {
                    ("切磋剑术 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("学习舞步 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("观看表演 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                }),
                ("可莉在炸鱼！", new (string, System.Action)[] {
                    ("帮忙炸鱼 (攻+2,随机卡)", () => { permanentATKBonus+=2; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("提醒危险 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("躲起来 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("迪奥娜调了新品鸡尾酒", new (string, System.Action)[] {
                    ("品尝 (治疗30%,倍率-0.01)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f),maxPlayerHP+maxHPBonus); stepMultiplier=Mathf.Max(0.3f,stepMultiplier-0.01f); }),
                    ("拒绝 (攻+1)", () => { permanentATKBonus+=1; }),
                    ("帮忙采摘材料 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("砂糖在实验室研究", new (string, System.Action)[] {
                    ("帮忙实验 (攻+3,扣10%HP)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f)); }),
                    ("观察记录 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("借用器材 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("莫娜占卜出了吉兆！", new (string, System.Action)[] {
                    ("详细占卜 (倍率+0.04)", () => { stepMultiplier+=0.04f; }),
                    ("简单占卜 (攻+1,倍率+0.01)", () => { permanentATKBonus+=1; stepMultiplier+=0.01f; }),
                    ("不信占卜 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("班尼特冒险团招募队员！", new (string, System.Action)[] {
                    ("加入冒险 (攻+2,卡牌)", () => { permanentATKBonus+=2; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("资助他们 (摩拉-30,倍率+0.03)", () => { mora=Mathf.Max(0,mora-30); stepMultiplier+=0.03f; }),
                    ("婉拒 (无损)", () => { {} }),
                }),
                ("菲谢尔召唤了奥兹！", new (string, System.Action)[] {
                    ("接受雷鸟祝福 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("请求侦察 (倍率+0.02,卡牌)", () => { stepMultiplier+=0.02f; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("奥兹吓跑了 (无事)", () => { {} }),
                }),
                ("罗莎莉亚在暗中调查", new (string, System.Action)[] {
                    ("协助调查 (攻+2,倍率+0.01)", () => { permanentATKBonus+=2; stepMultiplier+=0.01f; }),
                    ("保守秘密 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("通知骑士团 (摩拉+50)", () => { mora+=50; }),
                }),
                ("钟离讲述古老契约…", new (string, System.Action)[] {
                    ("接受契约 (倍率-0.05,攻+2)", () => { permanentATKBonus+=2; stepMultiplier=Mathf.Max(0.5f,stepMultiplier-0.05f); }),
                    ("婉拒 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("凝光提出商业合作", new (string, System.Action)[] {
                    ("投资矿业 (倍率+0.03,攻+1)", () => { permanentATKBonus+=1; stepMultiplier+=0.03f; }),
                    ("投资贸易 (摩拉+80,2卡)", () => { mora+=80; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("婉拒 (无损)", () => { {} }),
                }),
                ("刻晴请你帮忙处理事务", new (string, System.Action)[] {
                    ("加班处理 (攻+2,倍率+0.01,扣25%HP)", () => { permanentATKBonus+=2; stepMultiplier+=0.01f; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f)); }),
                    ("推荐甘雨 (攻+1)", () => { permanentATKBonus+=1; }),
                    ("推给钟离 (无事)", () => { {} }),
                }),
                ("七七发现了古老药方！", new (string, System.Action)[] {
                    ("配制药方 (恢复30%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f),maxPlayerHP+maxHPBonus); }),
                    ("出售药方 (2卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("香菱研发了新菜谱！", new (string, System.Action)[] {
                    ("品尝料理 (恢复35%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.35f),maxPlayerHP+maxHPBonus); }),
                    ("学习烹饪 (攻+1,倍率+0.01)", () => { permanentATKBonus+=1; stepMultiplier+=0.01f; }),
                    ("帮忙采集食材 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("行秋在写小说需要灵感", new (string, System.Action)[] {
                    ("讲述冒险故事 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("推荐书籍 (获得卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("帮忙校对 (摩拉+40)", () => { mora+=40; }),
                }),
                ("重云在驱邪途中迷路了", new (string, System.Action)[] {
                    ("帮忙驱邪 (攻+1,治疗15%)", () => { permanentATKBonus+=1; persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                    ("指路 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("邀请同行 (攻+2)", () => { permanentATKBonus+=2; }),
                }),
                ("胡桃在推销棺材…", new (string, System.Action)[] {
                    ("购买优惠券 (摩拉-20,倍率+0.03)", () => { {} }),
                    ("帮忙宣传 (攻+1,卡牌)", () => { permanentATKBonus+=1; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("快跑 (无事)", () => { {} }),
                }),
                ("魈在高处守望璃月", new (string, System.Action)[] {
                    ("请求守护 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("献上杏仁豆腐 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("默默离开 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("甘雨在处理政务中睡着了", new (string, System.Action)[] {
                    ("帮忙处理 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("给她盖毯子 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("请教税收 (摩拉+70)", () => { mora+=70; }),
                }),
                ("云堇在排练新戏", new (string, System.Action)[] {
                    ("帮忙布景 (攻+1,卡牌)", () => { permanentATKBonus+=1; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("观看排练 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("学习唱腔 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("辛焱在街头开摇滚演唱会", new (string, System.Action)[] {
                    ("加入演唱 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("后台帮忙 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("捂住耳朵 (恢复10%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                }),
                ("烟绯在法律咨询", new (string, System.Action)[] {
                    ("请教法律 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("委托案子 (摩拉+60,卡牌)", () => { mora+=60; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("闲聊 (攻+1)", () => { permanentATKBonus+=1; }),
                }),
                ("北斗的船队靠岸了！", new (string, System.Action)[] {
                    ("出海冒险 (攻+3,扣15%HP)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f)); }),
                    ("港口贸易 (摩拉+90,卡牌)", () => { mora+=80; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("喝酒叙旧 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                }),
                ("申鹤在雪山修行", new (string, System.Action)[] {
                    ("一同修行 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("送温暖衣物 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("学习仙法 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("夜兰在收集情报", new (string, System.Action)[] {
                    ("交换情报 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("协助行动 (攻+2,卡牌)", () => { permanentATKBonus+=2; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("装作不认识 (恢复10%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                }),
                ("神里绫人邀请茶会", new (string, System.Action)[] {
                    ("参加茶会 (卡牌+倍率+0.01)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); stepMultiplier+=0.01f; }),
                    ("切磋剑术 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("拒绝 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("雷电将军在冥想…", new (string, System.Action)[] {
                    ("安静打坐 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("请求比武 (攻+3,扣20%HP)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f)); }),
                    ("偷偷溜走 (无损)", () => { {} }),
                }),
                ("八重神子在写轻小说", new (string, System.Action)[] {
                    ("提供灵感 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("帮忙推广 (摩拉+70)", () => { mora+=70; }),
                    ("预约签名版 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("托马在整理离岛贸易", new (string, System.Action)[] {
                    ("帮忙整理 (摩拉+80)", () => { mora+=80; }),
                    ("打听商路 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("喝茶歇息 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                }),
                ("九条裟罗训练天领奉行", new (string, System.Action)[] {
                    ("参加训练 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("战术讨论 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("巡检营地 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("宵宫在准备烟花大会", new (string, System.Action)[] {
                    ("帮忙制作烟花 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("欣赏烟花 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("购买特制烟花 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("五郎带领反抗军巡逻", new (string, System.Action)[] {
                    ("加入巡逻 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("提供补给 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("收集情报 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("早柚在树上睡着了…", new (string, System.Action)[] {
                    ("悄悄绕过 (倍率+0.01)", () => { stepMultiplier+=0.01f; }),
                    ("叫醒她 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("给她盖被子 (治疗10%)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                }),
                ("神里绫华在练习书法", new (string, System.Action)[] {
                    ("请求墨宝 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("切磋剑术 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("共饮茶道 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                }),
                ("荒泷一斗在打牌", new (string, System.Action)[] {
                    ("加入牌局 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("比武对决 (攻+2,扣10%HP)", () => { permanentATKBonus+=2; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f)); }),
                    ("请吃拉面 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("久岐忍在处理帮派事务", new (string, System.Action)[] {
                    ("法律咨询 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("帮忙调解 (摩拉+50,卡牌)", () => { mora+=80; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("提供医疗帮助 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                }),
                ("珊瑚宫心海在制定战略", new (string, System.Action)[] {
                    ("参与讨论 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("提供情报 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("休息调整 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                }),
                ("纳西妲在梦中低语…", new (string, System.Action)[] {
                    ("接受智慧 (倍率+0.04)", () => { stepMultiplier+=0.04f; }),
                    ("请求帮助 (2卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("须弥教令院发现稀有论文！", new (string, System.Action)[] {
                    ("精读论文 (卡牌+倍率+0.02)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); stepMultiplier+=0.02f; }),
                    ("出售论文 (攻+2)", () => { permanentATKBonus+=2; }),
                }),
                ("提纳里在巡林", new (string, System.Action)[] {
                    ("协助巡林 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("采集草药 (治疗20%)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("学习植物学 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("赛诺在打七圣召唤", new (string, System.Action)[] {
                    ("加入对局 (倍率+0.02,卡牌)", () => { stepMultiplier+=0.02f; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("旁观学习 (攻+1)", () => { permanentATKBonus+=1; }),
                    ("帮忙决斗 (攻+2)", () => { permanentATKBonus+=2; }),
                }),
                ("妮露在排练舞蹈", new (string, System.Action)[] {
                    ("欣赏舞蹈 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("帮忙布置舞台 (攻+1,倍率+0.01)", () => { permanentATKBonus+=1; stepMultiplier+=0.01f; }),
                    ("学习舞蹈 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("多莉在兜售稀有商品", new (string, System.Action)[] {
                    ("购买 (卡牌×3,摩拉-50)", () => { for(int i=0;i<3;i++)Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); mora=Mathf.Max(0,mora-50); }),
                    ("讨价还价 (卡牌,摩拉不变)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("不感兴趣 (无损)", () => { {} }),
                }),
                ("坎蒂丝守护着阿如村", new (string, System.Action)[] {
                    ("帮忙巡逻 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("休息片刻 (恢复30%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f),maxPlayerHP+maxHPBonus); }),
                    ("请教武艺 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("珐露珊在做机关研究", new (string, System.Action)[] {
                    ("协助研究 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("测试机关 (攻+2,扣15%HP)", () => { permanentATKBonus+=2; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f)); }),
                    ("收集零件 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("莱依拉在熬夜写论文", new (string, System.Action)[] {
                    ("帮忙校对 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("劝她休息 (治疗20%)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("帮忙实验 (攻+2)", () => { permanentATKBonus+=2; }),
                }),
                ("艾尔海森在读书", new (string, System.Action)[] {
                    ("一起读书 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("讨论学术 (攻+1,卡牌)", () => { permanentATKBonus+=1; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("安静不打扰 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("卡维在画建筑图纸", new (string, System.Action)[] {
                    ("帮忙计算 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("购买设计 (摩拉-20,卡牌×2)", () => { mora=Mathf.Max(0,mora-20); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("欣赏建筑 (恢复10%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                }),
                ("流浪者在四处游荡", new (string, System.Action)[] {
                    ("切磋武艺 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("请他喝茶 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("不加理睬 (无事)", () => { {} }),
                }),
                ("那维莱特在审判案件", new (string, System.Action)[] {
                    ("旁听审判 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("提供证词 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("协助调查 (卡牌+摩拉30)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); mora+=30; }),
                }),
                ("芙宁娜在表演歌剧", new (string, System.Action)[] {
                    ("观看演出 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("帮忙排练 (攻+1,倍率+0.02)", () => { permanentATKBonus+=1; stepMultiplier+=0.02f; }),
                    ("后台帮忙 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("林尼在表演魔术", new (string, System.Action)[] {
                    ("参与表演 (倍率+0.02,卡牌)", () => { stepMultiplier+=0.02f; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("破解魔术 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("喝彩 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("仆人·阿蕾奇诺设宴", new (string, System.Action)[] {
                    ("接受宴请 (恢复35%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.35f),maxPlayerHP+maxHPBonus); }),
                    ("讨论公务 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("婉拒 (攻+1)", () => { permanentATKBonus+=1; }),
                }),
                ("娜维娅在做甜点", new (string, System.Action)[] {
                    ("品尝甜点 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("帮忙制作 (攻+1,倍率+0.01)", () => { permanentATKBonus+=1; stepMultiplier+=0.01f; }),
                    ("购买材料 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("克洛琳德在维护治安", new (string, System.Action)[] {
                    ("协助巡逻 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("休息喝茶 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("学习枪术 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("夏洛蒂在寻找新闻素材", new (string, System.Action)[] {
                    ("提供线索 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("接受采访 (攻+1,摩拉+40)", () => { permanentATKBonus+=1; mora+=40; }),
                    ("婉拒 (无损)", () => { {} }),
                }),
                ("夏沃蕾在执行任务", new (string, System.Action)[] {
                    ("协助任务 (攻+2,卡牌)", () => { permanentATKBonus+=2; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("提供情报 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("婉拒 (治疗10%)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                }),
                ("玛拉妮在冲浪", new (string, System.Action)[] {
                    ("一起冲浪 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("沙滩休息 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("收集贝壳 (卡牌+摩拉20)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); mora+=30; }),
                }),
                ("基尼奇在山中狩猎", new (string, System.Action)[] {
                    ("协助狩猎 (攻+2,倍率+0.01)", () => { permanentATKBonus+=2; stepMultiplier+=0.01f; }),
                    ("分享猎物 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("学习追踪 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("愚人众设下陷阱！", new (string, System.Action)[] {
                    ("正面迎战 (攻+1, HP上限-5)", () => { permanentATKBonus+=1; maxHPBonus=Mathf.Max(-30,maxHPBonus-5); }),
                    ("绕道而行 (HP上限+3)", () => { maxHPBonus+=3; }),
                }),
                ("流浪的剑客请求比试", new (string, System.Action)[] {
                    ("全力应战 (攻+2,扣20%HP)", () => { permanentATKBonus+=2; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f)); }),
                    ("携手同行 (卡牌+治疗10%)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                    ("果断拒绝 (无损)", () => { {} }),
                }),
                ("炼金术士兜售药剂", new (string, System.Action)[] {
                    ("力量药剂 (攻+3,扣15%HP)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f)); }),
                    ("生命药剂 (恢复40%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.4f),maxPlayerHP+maxHPBonus); }),
                    ("神秘药剂 (3卡牌)", () => { for(int i=0;i<3;i++)Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("深渊法师设下埋伏！", new (string, System.Action)[] {
                    ("正面突破 (攻+3,扣25%HP)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f)); }),
                    ("绕路撤退 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("请求支援 (2卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("盗宝团在附近出没", new (string, System.Action)[] {
                    ("清剿盗宝团 (攻+2,摩拉+40)", () => { permanentATKBonus+=2; mora+=40; }),
                    ("小心绕过 (倍率+0.01)", () => { stepMultiplier+=0.01f; }),
                    ("跟踪他们 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("天空岛的使者在寻找你", new (string, System.Action)[] {
                    ("接受试炼 (攻+3,倍率+0.02)", () => { permanentATKBonus+=3; stepMultiplier+=0.02f; }),
                    ("婉拒试炼 (恢复30%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f),maxPlayerHP+maxHPBonus); }),
                    ("请教知识 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("梦见月瑞希在占卜命运", new (string, System.Action)[] {
                    ("详细占卜 (倍率+0.04)", () => { stepMultiplier+=0.04f; }),
                    ("简单问题 (攻+1,恢复10%)", () => { permanentATKBonus+=1; persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                    ("不信占卜 (无事)", () => { {} }),
                }),
                ("古老的遗迹中传来声音…", new (string, System.Action)[] {
                    ("探索遗迹 (攻+2,卡牌)", () => { permanentATKBonus+=2; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("记录铭文 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("献上祭品 (摩拉-20,恢复40%HP)", () => { mora=Mathf.Max(0,mora-20); persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.4f),maxPlayerHP+maxHPBonus); }),
                }),
                ("神秘的商人出现在路边", new (string, System.Action)[] {
                    ("购买珍品 (3卡牌,摩拉-60)", () => { for(int i=0;i<3;i++)Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); mora=Mathf.Max(0,mora-60); }),
                    ("交换物品 (1卡牌,无损)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("无视 (无事)", () => { {} }),
                }),
                ("一场突如其来的暴雨", new (string, System.Action)[] {
                    ("冒雨前行 (攻+1,扣5%HP)", () => { permanentATKBonus+=1; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f)); }),
                    ("寻找避雨处 (恢复10%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                    ("雨中修炼 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                }),
                ("丘丘人在路边跳舞", new (string, System.Action)[] {
                    ("加入跳舞 (攻+1)", () => { permanentATKBonus+=1; }),
                    ("赠送食物 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("悄悄通过 (倍率+0.01)", () => { stepMultiplier+=0.01f; }),
                }),
                ("你在地上发现了一袋摩拉", new (string, System.Action)[] {
                    ("据为己有 (摩拉+100)", () => { mora+=100; }),
                    ("寻找失主 (攻+1,倍率+0.01)", () => { permanentATKBonus+=1; stepMultiplier+=0.01f; }),
                }),
                ("一只小仙灵想跟着你", new (string, System.Action)[] {
                    ("接受仙灵 (倍率+0.02,卡牌)", () => { stepMultiplier+=0.02f; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("指引仙灵回家 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("给仙灵食物 (攻+1)", () => { permanentATKBonus+=1; }),
                }),
                ("地脉之花在路边绽放", new (string, System.Action)[] {
                    ("接触地脉 (3卡牌)", () => { for(int i=0;i<3;i++)Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("收集地脉能量 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("避开 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("一位来自至冬的商人", new (string, System.Action)[] {
                    ("购买武器 (攻+3,摩拉-40)", () => { permanentATKBonus+=3; mora=Mathf.Max(0,mora-40); }),
                    ("交易情报 (倍率+0.02,摩拉+20)", () => { stepMultiplier+=0.02f; mora+=20; }),
                    ("婉拒 (无损)", () => { {} }),
                }),
                ("你在路上遇到了另一个旅行者", new (string, System.Action)[] {
                    ("结伴同行 (攻+1,恢复15%)", () => { permanentATKBonus+=1; persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                    ("交换物资 (卡牌,摩拉+30)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); mora+=30; }),
                    ("分道扬镳 (无事)", () => { {} }),
                }),
                ("一阵元素乱流袭来！", new (string, System.Action)[] {
                    ("吸收能量 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("引导能量 (倍率+0.04)", () => { stepMultiplier+=0.04f; }),
                    ("躲避 (扣10%HP,无事)", () => { persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f)); }),
                }),
                ("你发现了一个宝盗团营地", new (string, System.Action)[] {
                    ("突袭营地 (攻+2,摩拉+80)", () => { permanentATKBonus+=2; mora+=80; }),
                    ("夜袭偷取 (3卡牌)", () => { for(int i=0;i<3;i++)Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("放弃 (无事)", () => { {} }),
                }),
                ("一位炼金术士的实验室着火了", new (string, System.Action)[] {
                    ("帮忙灭火 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("抢救器材 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("袖手旁观 (攻+1)", () => { permanentATKBonus+=1; }),
                }),
                ("温迪和钟离在酒馆拼酒", new (string, System.Action)[] {
                    ("加入拼酒 (恢复20%HP,倍率+0.01)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); stepMultiplier+=0.01f; }),
                    ("劝架 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("记账 (摩拉+60)", () => { mora+=60; }),
                }),
                ("纳西妲和雷电将军在辩论", new (string, System.Action)[] {
                    ("支持纳西妲 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("支持雷电将军 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("当裁判 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("芙宁娜和妮露在即兴表演", new (string, System.Action)[] {
                    ("观看表演 (恢复30%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f),maxPlayerHP+maxHPBonus); }),
                    ("参与演出 (倍率+0.02,卡牌)", () => { stepMultiplier+=0.02f; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("帮忙舞台 (攻+2)", () => { permanentATKBonus+=2; }),
                }),
                ("刻晴和赛诺在进行效率比赛", new (string, System.Action)[] {
                    ("帮助刻晴 (倍率+0.02,摩拉+30)", () => { stepMultiplier+=0.02f; mora+=20; }),
                    ("帮助赛诺 (攻+2,卡牌)", () => { permanentATKBonus+=2; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("当裁判 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("可莉和宵宫在比赛放烟花", new (string, System.Action)[] {
                    ("加入可莉 (攻+2,扣5%HP)", () => { permanentATKBonus+=2; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.05f)); }),
                    ("加入宵宫 (卡牌,倍率+0.01)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); stepMultiplier+=0.01f; }),
                    ("观看烟花 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                }),
                ("七七和白术在采集草药", new (string, System.Action)[] {
                    ("帮忙采集 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("学习药方 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("购买药材 (卡牌×2,摩拉-20)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); mora=Mathf.Max(0,mora-20); }),
                }),
                ("魈和流浪者在空中对峙", new (string, System.Action)[] {
                    ("帮助魈 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("帮助流浪者 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("调和矛盾 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("荒泷一斗向八重神子挑战", new (string, System.Action)[] {
                    ("支持一斗 (攻+2,扣10%HP)", () => { permanentATKBonus+=2; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f)); }),
                    ("支持神子 (倍率+0.02,摩拉+50)", () => { mora+=80; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("吃瓜看戏 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("迪卢克和凯亚在酒馆偶遇", new (string, System.Action)[] {
                    ("请客喝酒 (恢复30%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f),maxPlayerHP+maxHPBonus); }),
                    ("调和兄弟 (攻+2,倍率+0.01)", () => { permanentATKBonus+=2; stepMultiplier+=0.01f; }),
                    ("默默离开 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("甘雨和申鹤在雪山采药", new (string, System.Action)[] {
                    ("协助采药 (恢复35%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.35f),maxPlayerHP+maxHPBonus); }),
                    ("学习仙术 (倍率+0.02,攻+1)", () => { permanentATKBonus+=1; stepMultiplier+=0.02f; }),
                    ("帮忙背药 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("深渊的诅咒降临在你身上！", new (string, System.Action)[] {
                    ("承受诅咒 (攻+3,扣30%HP)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f)); }),
                    ("寻找牧师 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                    ("用元素力抵抗 (倍率+0.03,扣10%HP)", () => { stepMultiplier+=0.03f; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f)); }),
                }),
                ("你在野外吃到了有毒的蘑菇", new (string, System.Action)[] {
                    ("用元素力解毒 (攻+1,扣20%HP)", () => { permanentATKBonus+=1; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f)); }),
                    ("躺下休息 (恢复10%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                    ("硬扛 (倍率-0.01,无事)", () => { stepMultiplier=Mathf.Max(0.3f,stepMultiplier-0.01f); }),
                }),
                ("盗宝团偷走了你的物资！", new (string, System.Action)[] {
                    ("追回物资 (攻+2,扣15%HP)", () => { permanentATKBonus+=2; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f)); }),
                    ("购买替代品 (摩拉-50,卡牌)", () => { mora=Mathf.Max(0,mora-50); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("算了 (倍率+0.01)", () => { stepMultiplier+=0.01f; }),
                }),
                ("你被卷入了一场元素风暴", new (string, System.Action)[] {
                    ("硬抗风暴 (攻+2,扣25%HP)", () => { permanentATKBonus+=2; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f)); }),
                    ("寻找掩护 (恢复10%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                    ("引导风暴 (倍率+0.05,扣15%HP)", () => { stepMultiplier+=0.05f; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f)); }),
                }),
                ("愚人众执行官路过！", new (string, System.Action)[] {
                    ("隐藏气息 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("跟踪观察 (攻+1,扣10%HP)", () => { permanentATKBonus+=1; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f)); }),
                    ("正面碰撞 (攻+3,扣30%HP)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f)); }),
                }),
                ("艾莉丝寄来了神秘包裹", new (string, System.Action)[] {
                    ("拆开包裹 (随机效果)", () => { permanentATKBonus+=2; }),
                    ("转送他人 (摩拉+60)", () => { mora+=60; }),
                    ("退回 (倍率+0.01)", () => { stepMultiplier+=0.01f; }),
                }),
                ("你在树下发现了一本日记", new (string, System.Action)[] {
                    ("阅读日记 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("焚烧日记 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("收藏日记 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("一位老者请你帮忙送信", new (string, System.Action)[] {
                    ("帮忙送信 (攻+1,摩拉+30)", () => { permanentATKBonus+=1; mora+=40; }),
                    ("婉拒 (倍率+0.01)", () => { stepMultiplier+=0.01f; }),
                    ("自己拆开看 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
                ("风神像前有人在许愿", new (string, System.Action)[] {
                    ("一起许愿 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("捐献摩拉 (摩拉-20,恢复25%HP)", () => { mora=Mathf.Max(0,mora-20); persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("静静旁观 (攻+1)", () => { permanentATKBonus+=1; }),
                }),
                ("一群史莱姆在嬉戏", new (string, System.Action)[] {
                    ("捕捉史莱姆 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("观察史莱姆 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("绕开 (恢复10%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                }),
                ("你遇到了一位吟游诗人", new (string, System.Action)[] {
                    ("学习诗歌 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("打赏 (摩拉-20,卡牌)", () => { mora=Mathf.Max(0,mora-20); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("一起旅行 (攻+1,恢复10%)", () => { permanentATKBonus+=1; persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                }),
                ("悬崖边有一株发光的草", new (string, System.Action)[] {
                    ("冒险采摘 (攻+3,扣20%HP)", () => { permanentATKBonus+=3; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f)); }),
                    ("请求风之翼帮助 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("放弃 (恢复10%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f),maxPlayerHP+maxHPBonus); }),
                }),
                ("一个小孩在找他的猫", new (string, System.Action)[] {
                    ("帮忙找猫 (攻+1,卡牌)", () => { permanentATKBonus+=1; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("用法元素搜索 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("给小孩摩拉 (摩拉-20,无事)", () => { mora=Mathf.Max(0,mora-20); }),
                }),
                ("你感受到了一股元素共鸣", new (string, System.Action)[] {
                    ("引导共鸣 (倍率+0.05,扣10%HP)", () => { stepMultiplier+=0.05f; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.1f)); }),
                    ("吸收共鸣 (攻+2)", () => { permanentATKBonus+=2; }),
                    ("压制共鸣 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                }),
                ("天理在注视着你…", new (string, System.Action)[] {
                    ("仰望天空 (倍率+0.06)", () => { stepMultiplier+=0.06f; }),
                    ("低头前行 (攻+3)", () => { permanentATKBonus+=3; }),
                    ("寻找掩体 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                }),
                ("你在路边发现了一个传送锚点", new (string, System.Action)[] {
                    ("激活锚点 (倍率+0.02)", () => { stepMultiplier+=0.02f; }),
                    ("调查锚点 (卡牌)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("使用锚点 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                }),
                ("一位冒险家向你兜售藏宝图", new (string, System.Action)[] {
                    ("购买藏宝图 (摩拉-40,卡牌×3)", () => { mora=Mathf.Max(0,mora-40); for(int i=0;i<3;i++)Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("讨价还价 (摩拉-20,卡牌)", () => { mora=Mathf.Max(0,mora-20); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("不信 (无事)", () => { {} }),
                }),
                ("地脉异常引发地震", new (string, System.Action)[] {
                    ("稳定地脉 (倍率+0.03)", () => { stepMultiplier+=0.03f; }),
                    ("逃离区域 (扣5%HP,无事)", () => { persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.05f)); }),
                    ("收集逸散能量 (攻+2)", () => { permanentATKBonus+=2; }),
                }),
                ("你遇到了一个迷路的冒险家", new (string, System.Action)[] {
                    ("带路 (攻+1,倍率+0.01)", () => { permanentATKBonus+=1; stepMultiplier+=0.01f; }),
                    ("给他食物 (恢复15%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.15f),maxPlayerHP+maxHPBonus); }),
                    ("分享地图 (摩拉+30)", () => { mora+=30; }),
                }),
                ("一位商人在叫卖元素药剂", new (string, System.Action)[] {
                    ("购买攻击药剂 (攻+3,摩拉-30)", () => { permanentATKBonus+=3; mora=Mathf.Max(0,mora-30); }),
                    ("购买防御药剂 (恢复30%HP,摩拉-20)", () => { mora=Mathf.Max(0,mora-20); persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.3f),maxPlayerHP+maxHPBonus); }),
                    ("不买 (无事)", () => { {} }),
                }),
                ("一个受伤的NPC躺在路边", new (string, System.Action)[] {
                    ("用元素力治疗 (恢复20%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f),maxPlayerHP+maxHPBonus); }),
                    ("使用绷带 (攻+1,卡牌)", () => { permanentATKBonus+=1; Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                    ("呼叫医生 (摩拉-30,倍率+0.02)", () => { mora=Mathf.Max(0,mora-30); stepMultiplier+=0.02f; }),
                }),
                ("古老神殿的试炼之门打开了", new (string, System.Action)[] {
                    ("进入试炼 (攻+3,倍率+0.02,扣20%HP)", () => { permanentATKBonus+=3; stepMultiplier+=0.02f; persistentHP=Mathf.Max(1,persistentHP-Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.2f)); }),
                    ("在门外祈祷 (恢复25%HP)", () => { persistentHP=Mathf.Min(persistentHP+Mathf.CeilToInt((maxPlayerHP+maxHPBonus)*0.25f),maxPlayerHP+maxHPBonus); }),
                    ("记录符文 (卡牌×2)", () => { Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]); }),
                }),
            };

            var e = events[Random.Range(0, events.Count)];
            SetState(GameState.Event);
            HideAll();
            if (eventPanel) eventPanel.SetActive(true);

            var eventUI = eventPanel?.GetComponent<EventUI>();
            if (eventUI != null)
            {
                var eventText = e.Item1;
                var eventChoices = e.Item2;
                var choices = new (string, System.Action)[eventChoices.Length];
                for (int i = 0; i < eventChoices.Length; i++)
                {
                    var captured = eventChoices[i].Item2;
                    choices[i] = (eventChoices[i].Item1, () => { captured(); SaveManager.Save(this); ShowMap(); });
                }
                eventUI.Show(eventText, choices);
            }
            else
            {
                e.Item2[0].Item2();
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
