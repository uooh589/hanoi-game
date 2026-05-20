using UnityEngine;
using UnityEngine.UIElements;

namespace HanoiGame.GenshinUI
{
    /// <summary>Demonstrates Genshin UI Toolkit system — login screen with health bar and buttons.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class ExampleUsage : MonoBehaviour
    {
        private UIDocument _doc;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            var root = _doc.rootVisualElement;
            root.Clear();
            root.AddToClassList("genshin-root");

            // Load USS
            var uss = GenshinResources.LoadUSS("GenshinStyle");
            if (uss != null) root.styleSheets.Add(uss);

            BuildLoginScreen(root);
        }

        void BuildLoginScreen(VisualElement root)
        {
            // Background
            var bg = new VisualElement();
            bg.style.flexGrow = 1;
            bg.style.backgroundColor = GenshinUIFactory.DarkBg;
            root.Add(bg);

            // Content wrapper
            var wrapper = new VisualElement();
            wrapper.style.position = Position.Absolute;
            wrapper.style.left = wrapper.style.top = wrapper.right = wrapper.bottom = 0;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.justifyContent = Justify.Center;
            bg.Add(wrapper);

            // Panel
            var panelContent = new VisualElement();
            panelContent.style.alignItems = Align.Center;

            // Title
            var title = GenshinUIFactory.CreateGlowingText("汉诺塔：轮回", "genshin-title-lg");
            title.style.marginBottom = 24;
            panelContent.Add(title);

            // Subtitle
            var sub = new Label("Roguelike 卡牌战斗");
            sub.style.color = GenshinUIFactory.WarmWhite * 0.7f;
            sub.style.fontSize = 14;
            sub.style.marginBottom = 32;
            panelContent.Add(sub);

            // Health bar demo
            var hpBar = GenshinUIFactory.CreateHealthBar(45, 80);
            hpBar.style.width = 280;
            hpBar.style.marginBottom = 20;
            panelContent.Add(hpBar);

            // Buttons
            var startBtn = GenshinUIFactory.CreateButton("新游戏", () => Debug.Log("[GenshinUI] New Game clicked"));
            startBtn.style.width = 260;

            var continueBtn = GenshinUIFactory.CreateButton("继续旅程", () => Debug.Log("[GenshinUI] Continue clicked"));
            continueBtn.style.width = 260;

            var quitBtn = GenshinUIFactory.CreateButton("退出", () => Application.Quit());
            quitBtn.style.width = 260;

            panelContent.Add(startBtn);
            panelContent.Add(continueBtn);
            panelContent.Add(quitBtn);

            // Icon frame demo
            var iconRow = GenshinUIFactory.FlexRow(
                GenshinUIFactory.CreateIconFrame(null, true),
                GenshinUIFactory.CreateIconFrame(null, false),
                GenshinUIFactory.CreateIconFrame(null, false)
            );
            iconRow.style.marginTop = 16;
            iconRow.style.justifyContent = Justify.SpaceAround;
            iconRow.style.width = 260;
            panelContent.Add(iconRow);

            var panel = GenshinUIFactory.CreatePanel("", panelContent);
            wrapper.Add(panel);
        }
    }
}
