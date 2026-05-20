using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using HanoiGame.GenshinUI;

namespace HanoiGame
{
    /// <summary>Hybrid game root: UI Toolkit for menus/HUD + uGUI overlay for Hanoi towers.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class GenshinGameRoot : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root, _menuScreen, _battleHud, _mapScreen, _overlayPanel;
        private Label _hpLabel, _atkLabel, _enemyLabel, _stepsLabel;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
            _root.Clear();
            _root.AddToClassList("genshin-root");

            var uss = Resources.Load<StyleSheet>("GenshinStyle");
            if (uss != null) _root.styleSheets.Add(uss);

            BuildMainMenu();
            BuildBattleHUD();
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

            var bg = new VisualElement();
            bg.style.position = Position.Absolute;
            bg.style.top = bg.style.left = bg.style.right = bg.style.bottom = 0;
            bg.style.backgroundColor = GenshinUIFactory.DarkBg;
            _menuScreen.Add(bg);

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

            var startBtn = GenshinUIFactory.CreateButton("新游戏", OnNewGame);
            startBtn.style.width = 260;
            var contBtn = GenshinUIFactory.CreateButton("继续", OnContinue);
            contBtn.style.width = 260;
            var quitBtn = GenshinUIFactory.CreateButton("退出", () => Application.Quit());
            quitBtn.style.width = 260;
            var libBtn = GenshinUIFactory.CreateButton("图书馆", () => ShowOverlay("卡牌图鉴", "共 203 张卡牌，7 大元素流派"));
            libBtn.style.width = 260;

            wrapper.Add(startBtn); wrapper.Add(contBtn); wrapper.Add(libBtn); wrapper.Add(quitBtn);
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

            // Top bar: player + enemy info
            var topBar = new VisualElement();
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.style.paddingLeft = 20; topBar.style.paddingRight = 20;
            topBar.style.paddingTop = 12; topBar.style.height = 80;

            // Player info
            var playerBox = new VisualElement();
            playerBox.AddToClassList("genshin-glass");
            playerBox.style.width = 280; playerBox.style.height = 72;
            playerBox.style.paddingLeft = 12; playerBox.style.paddingTop = 8;
            _hpLabel = new Label("HP: 60/60") { style = { color = Color.green, fontSize = 16 } };
            _atkLabel = new Label("攻: 3") { style = { color = Color.white, fontSize = 14 } };
            playerBox.Add(_hpLabel); playerBox.Add(_atkLabel);
            // HP bar
            var hpBar = GenshinUIFactory.CreateHealthBar(60, 60);
            hpBar.style.width = 240; hpBar.style.marginTop = 4;
            playerBox.Add(hpBar);

            // Enemy info
            var enemyBox = new VisualElement();
            enemyBox.AddToClassList("genshin-glass");
            enemyBox.style.width = 280; enemyBox.style.height = 72;
            enemyBox.style.paddingLeft = 12; enemyBox.style.paddingTop = 8;
            enemyBox.style.alignItems = Align.FlexEnd;
            _enemyLabel = new Label("") { style = { color = Color.red, fontSize = 16 } };
            enemyBox.Add(_enemyLabel);

            topBar.Add(playerBox); topBar.Add(enemyBox);
            _battleHud.Add(topBar);

            // Bottom bar: steps + buttons
            var botBar = new VisualElement();
            botBar.style.position = Position.Absolute;
            botBar.style.bottom = 12; botBar.style.left = 0; botBar.style.right = 0;
            botBar.style.height = 44;
            botBar.style.flexDirection = FlexDirection.Row;
            botBar.style.justifyContent = Justify.Center;
            botBar.style.alignItems = Align.Center;

            _stepsLabel = new Label("步数: 0") { style = { color = Color.white, fontSize = 14 } };
            botBar.Add(_stepsLabel);

            var endBtn = GenshinUIFactory.CreateButton("结束回合", () => Debug.Log("End Turn"), "genshin-button-sm");
            endBtn.style.marginLeft = 16;
            var refBtn = GenshinUIFactory.CreateButton("刷新", () => Debug.Log("Refresh"), "genshin-button-sm");
            refBtn.style.marginLeft = 8;
            botBar.Add(endBtn); botBar.Add(refBtn);

            _battleHud.Add(botBar);
            _root.Add(_battleHud);
        }
        #endregion

        #region ── Overlay ──
        void ShowOverlay(string title, string body)
        {
            if (_overlayPanel != null) _overlayPanel.RemoveFromHierarchy();

            var content = new VisualElement();
            var bodyLabel = new Label(body) { style = { color = Color.white, fontSize = 14, whiteSpace = WhiteSpace.Normal } };
            content.Add(bodyLabel);
            var closeBtn = GenshinUIFactory.CreateButton("关闭", () => _overlayPanel?.RemoveFromHierarchy());
            content.Add(closeBtn);

            _overlayPanel = GenshinUIFactory.CreatePanel(title, content);
            _root.Add(_overlayPanel);
        }
        #endregion

        #region ── Screen management ──
        void ShowScreen(string name)
        {
            _menuScreen.style.display = name == "menu" ? DisplayStyle.Flex : DisplayStyle.None;
            _battleHud.style.display = name == "battle" ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnNewGame() { ShowScreen("battle"); Debug.Log("[GenshinUI] Starting new game"); }
        void OnContinue() { ShowScreen("battle"); Debug.Log("[GenshinUI] Continuing"); }
        #endregion
    }
}
