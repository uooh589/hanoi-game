using UnityEngine;
using UnityEngine.UIElements;

namespace HanoiGame.GenshinUI
{
    [RequireComponent(typeof(UIDocument))]
    public class ExampleUsage : MonoBehaviour
    {
        void Awake()
        {
            GenshinTween.SetRunner(this);
            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1;
            root.style.backgroundColor = GenshinUIFactory.DarkBg;

            var uss = Resources.Load<StyleSheet>("GenshinStyle");
            if (uss != null) root.styleSheets.Add(uss);

            var wrapper = new VisualElement();
            wrapper.style.flexGrow = 1;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.justifyContent = Justify.Center;
            root.Add(wrapper);

            var title = GenshinUIFactory.CreateGlowingText("汉诺塔：轮回", "genshin-title-lg");
            title.style.marginBottom = 24;
            wrapper.Add(title);

            var hpBar = GenshinUIFactory.CreateHealthBar(45, 80);
            hpBar.style.width = 280;
            hpBar.style.marginBottom = 24;
            wrapper.Add(hpBar);

            wrapper.Add(GenshinUIFactory.CreateButton("新游戏", () => Debug.Log("New Game"), ""));
            wrapper.Add(GenshinUIFactory.CreateButton("继续", () => Debug.Log("Continue"), ""));
            wrapper.Add(GenshinUIFactory.CreateButton("退出", () => Application.Quit(), ""));
        }
    }
}
