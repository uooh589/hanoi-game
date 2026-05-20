using UnityEngine;
using UnityEngine.UIElements;

namespace HanoiGame.GenshinUI
{
    [RequireComponent(typeof(UIDocument))]
    public class GenshinGameRoot : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root, _menuScreen, _battleHud;
        private Label _hpLabel, _atkLabel, _enemyLabel, _stepsLabel;

        void Awake()
        {
            GenshinTween.SetRunner(this);
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
            _root.Clear();
            _root.style.flexGrow = 1;

            var uss = Resources.Load<StyleSheet>("GenshinStyle");
            if (uss != null) _root.styleSheets.Add(uss);

            BuildMainMenu();
            BuildBattleHUD();
            ShowScreen("menu");
        }

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

            var sub = new Label("Roguelike 卡牌战斗");
            sub.style.color = GenshinUIFactory.WarmWhite * 0.6f;
            sub.style.fontSize = 14;
            sub.style.marginBottom = 40;
            wrapper.Add(sub);

            wrapper.Add(GenshinUIFactory.CreateButton("新游戏", OnNewGame));
            wrapper.Add(GenshinUIFactory.CreateButton("继续", OnContinue));
            wrapper.Add(GenshinUIFactory.CreateButton("图书馆", () => ShowOverlay("卡牌图鉴", "203 张卡牌 · 7 元素 · 120 事件 · 14 圣遗物")));
            wrapper.Add(GenshinUIFactory.CreateButton("退出", () => Application.Quit()));

            _menuScreen.Add(wrapper);
            _root.Add(_menuScreen);
        }

        void BuildBattleHUD()
        {
            _battleHud = new VisualElement();
            _battleHud.name = "BattleHUD";
            _battleHud.style.flexGrow = 1;
            _battleHud.style.display = DisplayStyle.None;

            // Top bar
            var topBar = new VisualElement();
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.style.paddingLeft = 20; topBar.style.paddingRight = 20;
            topBar.style.paddingTop = 12; topBar.style.height = 80;

            // Player box
            var playerBox = new VisualElement();
            playerBox.style.width = 280; playerBox.style.height = 72;
            playerBox.style.backgroundColor = new Color(0, 0, 0, 0.3f);
            playerBox.style.paddingLeft = 12; playerBox.style.paddingTop = 8;
            playerBox.style.borderTopLeftRadius = playerBox.style.borderTopRightRadius = 8;
            playerBox.style.borderBottomLeftRadius = playerBox.style.borderBottomRightRadius = 8;
            _hpLabel = new Label("HP: 60/60") { style = { color = Color.green, fontSize = 16 } };
            _atkLabel = new Label("攻: 3") { style = { color = Color.white, fontSize = 14 } };
            playerBox.Add(_hpLabel); playerBox.Add(_atkLabel);
            var hpBar = GenshinUIFactory.CreateHealthBar(60, 60);
            hpBar.style.width = 240; hpBar.style.marginTop = 4;
            playerBox.Add(hpBar);

            // Enemy box
            var enemyBox = new VisualElement();
            enemyBox.style.width = 280; enemyBox.style.height = 72;
            enemyBox.style.backgroundColor = new Color(0, 0, 0, 0.3f);
            enemyBox.style.paddingLeft = 12; enemyBox.style.paddingTop = 8;
            enemyBox.style.borderTopLeftRadius = enemyBox.style.borderTopRightRadius = 8;
            enemyBox.style.borderBottomLeftRadius = enemyBox.style.borderBottomRightRadius = 8;
            _enemyLabel = new Label("") { style = { color = Color.red, fontSize = 16 } };
            enemyBox.Add(_enemyLabel);

            topBar.Add(playerBox); topBar.Add(enemyBox);
            _battleHud.Add(topBar);

            // Bottom bar
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

        void ShowOverlay(string title, string body)
        {
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
            var b = new Label(body) { style = { color = Color.white, fontSize = 14, marginTop = 12, marginBottom = 20 } };
            card.Add(b);
            var close = GenshinUIFactory.CreateButton("关闭", () => overlay.RemoveFromHierarchy());
            card.Add(close);

            overlay.Add(card);
            _root.Add(overlay);
        }

        void ShowScreen(string name)
        {
            _menuScreen.style.display = name == "menu" ? DisplayStyle.Flex : DisplayStyle.None;
            _battleHud.style.display = name == "battle" ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnNewGame() { ShowScreen("battle"); Debug.Log("[GenshinUI] Starting new game"); }
        void OnContinue() { ShowScreen("battle"); Debug.Log("[GenshinUI] Continuing"); }
    }
}
