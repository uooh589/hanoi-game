using UnityEngine;
using UnityEngine.UIElements;

namespace HanoiGame.GenshinUI
{
    /// <summary>Hybrid game root: UI Toolkit for menus/HUD + existing uGUI for Hanoi towers.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class GenshinGameRoot : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root, _menuScreen, _battleHud;
        private Label _hpLabel, _atkLabel, _enemyLabel, _stepsLabel;
        private VisualElement _hpBar;

        void Awake()
        {
            GenshinTween.SetRunner(this);
            _doc = GetComponent<UIDocument>();
            if (_doc.panelSettings == null)
                _doc.panelSettings = Resources.Load<PanelSettings>("GenshinPanelSettings");

            _root = _doc.rootVisualElement;
            _root.Clear();
            _root.style.flexGrow = 1;

            var uss = Resources.Load<StyleSheet>("GenshinStyle");
            if (uss != null) _root.styleSheets.Add(uss);

            BuildMainMenu();
            BuildBattleHUD();
        }

        void Start()
        {
            ShowScreen("menu");
        }

        void Update()
        {
            // Poll game state and update UI
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.currentState == GameManager.GameState.MainMenu)
                ShowScreen("menu");
            else if (gm.currentState == GameManager.GameState.Battle)
            {
                ShowScreen("battle");
                RefreshBattleHUD();
            }
            else if (_menuScreen.style.display == DisplayStyle.Flex)
                ShowScreen("menu");
        }

        #region ── Main Menu ──
        void BuildMainMenu()
        {
            _menuScreen = new VisualElement();
            _menuScreen.name = "MenuScreen";
            _menuScreen.style.flexGrow = 1;
            _menuScreen.style.alignItems = Align.Center;
            _menuScreen.style.justifyContent = Justify.Center;
            _menuScreen.style.backgroundColor = GenshinUIFactory.DarkBg;

            var wrapper = new VisualElement();
            wrapper.style.alignItems = Align.Center;

            var title = GenshinUIFactory.CreateGlowingText("汉诺塔：轮回", "genshin-title-lg");
            title.style.marginBottom = 16;
            wrapper.Add(title);

            var sub = new Label("Roguelike 卡牌战斗 · 原神主题");
            sub.style.color = GenshinUIFactory.WarmWhite * 0.6f;
            sub.style.fontSize = 14;
            sub.style.marginBottom = 40;
            wrapper.Add(sub);

            wrapper.Add(GenshinUIFactory.CreateButton("新游戏", () => {
                GameManager.Instance?.StartNewGame();
            }));
            wrapper.Add(GenshinUIFactory.CreateButton("继续", () => {
                GameManager.Instance?.ContinueGame();
            }));
            wrapper.Add(GenshinUIFactory.CreateButton("图书馆", () => ShowOverlay("图书馆", "203张卡牌 · 7元素 · 120事件 · 14圣遗物")));
            wrapper.Add(GenshinUIFactory.CreateButton("退出", () => Application.Quit()));

            _menuScreen.Add(wrapper);
            _root.Add(_menuScreen);
        }
        #endregion

        #region ── Battle HUD ──
        void BuildBattleHUD()
        {
            _battleHud = new VisualElement();
            _battleHud.name = "BattleHUD";
            _battleHud.style.flexGrow = 1;
            _battleHud.style.display = DisplayStyle.None;
            _battleHud.pickingMode = PickingMode.Ignore; // let uGUI handle clicks

            // Top bar
            var topBar = new VisualElement();
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.style.paddingLeft = 20; topBar.style.paddingRight = 20;
            topBar.style.paddingTop = 8; topBar.style.height = 80;

            // Player box
            var playerBox = new VisualElement();
            playerBox.style.width = 280; playerBox.style.height = 70;
            playerBox.style.backgroundColor = new Color(0, 0, 0, 0.35f);
            playerBox.style.paddingLeft = 12; playerBox.style.paddingTop = 6;
            playerBox.style.borderTopLeftRadius = playerBox.style.borderTopRightRadius = 8;
            playerBox.style.borderBottomLeftRadius = playerBox.style.borderBottomRightRadius = 8;

            _hpLabel = new Label("HP: 60/60") { style = { color = Color.green, fontSize = 15 } };
            _atkLabel = new Label("攻: 3") { style = { color = Color.white, fontSize = 13 } };
            playerBox.Add(_hpLabel); playerBox.Add(_atkLabel);
            _hpBar = GenshinUIFactory.CreateHealthBar(60, 60);
            _hpBar.style.width = 240; _hpBar.style.marginTop = 4;
            playerBox.Add(_hpBar);

            // Enemy box
            var enemyBox = new VisualElement();
            enemyBox.style.width = 280; enemyBox.style.height = 70;
            enemyBox.style.backgroundColor = new Color(0, 0, 0, 0.35f);
            enemyBox.style.paddingLeft = 12; enemyBox.style.paddingTop = 6;
            enemyBox.style.borderTopLeftRadius = enemyBox.style.borderTopRightRadius = 8;
            enemyBox.style.borderBottomLeftRadius = enemyBox.style.borderBottomRightRadius = 8;
            _enemyLabel = new Label("") { style = { color = Color.red, fontSize = 15 } };
            enemyBox.Add(_enemyLabel);

            topBar.Add(playerBox); topBar.Add(enemyBox);
            _battleHud.Add(topBar);

            // Bottom bar
            var botBar = new VisualElement();
            botBar.style.position = Position.Absolute;
            botBar.style.bottom = 8; botBar.style.left = 0; botBar.style.right = 0;
            botBar.style.height = 40;
            botBar.style.flexDirection = FlexDirection.Row;
            botBar.style.justifyContent = Justify.Center;
            botBar.style.alignItems = Align.Center;

            _stepsLabel = new Label("步数: 0") { style = { color = Color.white, fontSize = 13 } };
            botBar.Add(_stepsLabel);

            var endBtn = GenshinUIFactory.CreateButton("结束回合", () => {
                var bm = GameManager.Instance?.GetBattleManager();
                bm?.EndPlayerTurn();
            }, "genshin-button-sm");
            endBtn.style.marginLeft = 12;
            var refBtn = GenshinUIFactory.CreateButton("刷新", () => {
                var bm = GameManager.Instance?.GetBattleManager();
                bm?.RefreshHand();
            }, "genshin-button-sm");
            refBtn.style.marginLeft = 8;
            var taskBtn = GenshinUIFactory.CreateButton("任务", () => Debug.Log("Task"), "genshin-button-sm");
            taskBtn.style.marginLeft = 8;
            botBar.Add(endBtn); botBar.Add(refBtn); botBar.Add(taskBtn);

            _battleHud.Add(botBar);
            _root.Add(_battleHud);
        }

        void RefreshBattleHUD()
        {
            var bm = GameManager.Instance?.GetBattleManager();
            if (bm == null) return;

            int maxHP = bm.playerMaxHP + GameManager.Instance.maxHPBonus;
            _hpLabel.text = $"HP: {bm.playerHP}/{maxHP}";
            _hpLabel.style.color = bm.playerHP < maxHP * 0.3f ? Color.red :
                                   bm.playerHP < maxHP * 0.6f ? new Color(1f, 0.7f, 0f) : Color.green;
            _atkLabel.text = $"攻: {bm.baseATK + GameManager.Instance.permanentATKBonus}";
            _stepsLabel.text = $"步数: {bm.stepsRemaining}";

            if (bm.enemy != null)
            {
                _enemyLabel.text = $"{bm.enemy.enemyName}\nHP: {bm.enemy.currentHP}/{bm.enemy.maxHP}";
            }

            // Update HP bar
            if (_hpBar != null)
            {
                _hpBar.Clear();
                var newBar = GenshinUIFactory.CreateHealthBar(bm.playerHP, maxHP);
                newBar.style.width = 240; newBar.style.marginTop = 4;
                var parent = _hpBar.parent;
                int idx = _hpBar.parent.IndexOf(_hpBar);
                _hpBar.RemoveFromHierarchy();
                newBar.name = _hpBar.name;
                parent.Insert(idx, newBar);
                _hpBar = newBar;
            }
        }
        #endregion

        #region ── Overlay ──
        void ShowOverlay(string title, string body)
        {
            // Remove existing overlay
            var existing = _root.Q(name: "Overlay");
            existing?.RemoveFromHierarchy();

            var overlay = new VisualElement();
            overlay.name = "Overlay";
            overlay.style.position = Position.Absolute;
            overlay.style.top = overlay.style.left = overlay.style.right = overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;

            var card = new VisualElement();
            card.style.width = 500; card.style.minHeight = 200;
            card.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.95f);
            card.style.paddingTop = 20; card.style.paddingBottom = 20;
            card.style.paddingLeft = 30; card.style.paddingRight = 30;
            card.style.alignItems = Align.Center;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 12;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 12;

            var t = new Label(title) { style = { color = GenshinUIFactory.Gold, fontSize = 22 } };
            card.Add(t);
            var b = new Label(body) { style = { color = Color.white, fontSize = 14, marginTop = 12, marginBottom = 20, whiteSpace = WhiteSpace.Normal } };
            card.Add(b);
            var close = GenshinUIFactory.CreateButton("关闭", () => overlay.RemoveFromHierarchy());
            card.Add(close);

            overlay.Add(card);
            _root.Add(overlay);
        }
        #endregion

        #region ── Screen management ──
        void ShowScreen(string name)
        {
            if (_menuScreen != null)
                _menuScreen.style.display = name == "menu" ? DisplayStyle.Flex : DisplayStyle.None;
            if (_battleHud != null)
                _battleHud.style.display = name == "battle" ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion
    }
}
