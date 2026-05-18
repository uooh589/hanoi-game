using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class MapUI : MonoBehaviour
    {
        public RectTransform mapArea;
        public Text stageText;
        public Text moraText;
        public Button deckButton;
        public GameObject deckViewerPanel;

        private MapData _map;
        private List<GameObject> _spawned = new();

        private void OnEnable()
        {
            _map = GameManager.Instance?.CurrentMap;
            if (_map != null) Build();
            if (deckButton != null)
            {
                deckButton.onClick.RemoveAllListeners();
                var dvp = deckViewerPanel;
                deckButton.onClick.AddListener(() =>
                {
                    if (dvp != null)
                    {
                        dvp.transform.SetAsLastSibling();
                        dvp.SetActive(true);
                        dvp.GetComponent<DeckViewerUI>()?.Refresh();
                    }
                });
            }
        }

        private void OnDisable() { Clear(); }

        void Clear() { foreach (var g in _spawned) if (g) Destroy(g); _spawned.Clear(); }

        public void Build()
        {
            Clear();
            if (_map == null) return;

            float aw = mapArea != null ? mapArea.rect.width : 1160;
            float ah = mapArea != null ? mapArea.rect.height : 600;
            float px = 50, py = 40;
            float uw = aw - px * 2, uh = ah - py * 2;

            // Draw floor separator lines
            for (int f = 1; f < _map.totalFloors; f++)
            {
                float y = py + _map.floors[f][0].pos.y * uh;
                DrawHLine(y, new Color(0.3f, 0.3f, 0.4f, 0.5f), aw - px * 2);
            }

            // Draw nodes
            foreach (var floor in _map.floors)
                foreach (var node in floor)
                    CreateNode(node, Px(node.pos.x, px, uw), Py(node.pos.y, py, uh));

            if (stageText) stageText.text = $"第 {GameManager.Instance.currentStage} 层 — 提瓦特";
            if (moraText) moraText.text = $"摩拉: {GameManager.Instance.mora}";
        }

        float Px(float t, float p, float w) => p + t * w - (mapArea?.rect.width ?? 1160) * 0.5f;
        float Py(float t, float p, float h) => p + t * h - (mapArea?.rect.height ?? 600) * 0.5f;

        void DrawHLine(float y, Color color, float w)
        {
            var go = new GameObject("hline", typeof(Image));
            go.transform.SetParent(mapArea != null ? mapArea.transform : transform, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, 2f);
            rt.anchoredPosition = new Vector2(0, y);
            _spawned.Add(go);
        }

        static readonly string[] NodeIconNames = { "battle_node", "elite_node", "event_node", "chest_node", "rest_node", "shop_node", "boss_node" };

        void CreateNode(MapNode node, float x, float y)
        {
            float r = 16f;
            var go = new GameObject($"N{node.floor}_{node.index}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(mapArea != null ? mapArea.transform : transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(r * 2, r * 2);
            rt.anchoredPosition = new Vector2(x, y);

            var img = go.GetComponent<Image>();
            // Load real node icon from Resources, fallback to procedural circle
            string iconName = $"{node.type.ToString().ToLower()}_node";
            var icon = Resources.Load<Sprite>(iconName);
            if (icon != null)
            {
                img.sprite = icon;
                img.color = Color.white;
            }
            else
            {
                img.sprite = MakeCircle(Mathf.RoundToInt(r * 2), node.NodeColor);
            }
            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            if (node == _map.currentNode)
            {
                go.transform.localScale = Vector3.one * 1.3f;
            }
            else if (node.visited)
            {
                img.color = new Color(0.5f, 0.5f, 0.5f);
            }
            else if (!_map.CanMoveTo(node))
            {
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }

            var btn = go.GetComponent<Button>();
            btn.interactable = _map.CanMoveTo(node);
            btn.onClick.AddListener(() => OnClick(node));

            // Hover effect + tooltip
            var hover = go.AddComponent<NodeHover>();
            string info = node.type switch
            {
                MapNodeType.Battle => "普通战斗\n击败敌人获得卡牌",
                MapNodeType.Elite => "精英战斗\n更强敌人，更多奖励",
                MapNodeType.Event => "随机事件\n可能获得增益或卡牌",
                MapNodeType.Chest => "宝箱\n直接获得一张卡牌",
                MapNodeType.Rest => "七天神像\n恢复生命值",
                MapNodeType.Shop => "商人\n花费摩拉购买卡牌",
                MapNodeType.Boss => "周本Boss\n击败后进入下一层",
                _ => ""
            };
            hover.tooltipText = $"{node.Label} - 第{node.floor + 1}层\n{info}";

            // Symbol label
            var lbl = new GameObject("lbl", typeof(Text));
            lbl.transform.SetParent(go.transform, false);
            var t = lbl.GetComponent<Text>();
            t.text = node.Symbol;
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.font = FontHelper.GetFont(14);
            t.raycastTarget = false;
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.sizeDelta = Vector2.zero;

            // Floor label below
            if (node.floor > 0 && node.floor < _map.totalFloors - 1)
            {
                var fl = new GameObject("fl", typeof(Text));
                fl.transform.SetParent(go.transform, false);
                var ft = fl.GetComponent<Text>();
                ft.text = node.Label;
                ft.fontSize = 10;
                ft.alignment = TextAnchor.MiddleCenter;
                ft.color = new Color(0.7f, 0.7f, 0.7f);
                ft.font = t.font;
                ft.raycastTarget = false;
                var frt = fl.GetComponent<RectTransform>();
                frt.anchorMin = frt.anchorMax = Vector2.one * 0.5f;
                frt.sizeDelta = new Vector2(40, 16);
                frt.anchoredPosition = new Vector2(0, -r - 12);
            }

            _spawned.Add(go);
        }

        void OnClick(MapNode node)
        {
            if (!_map.CanMoveTo(node)) return;
            SimpleAudio.Instance?.PlayClick();
            node.visited = true;
            _map.currentNode = node;

            // Award task steps for visiting any non-start node
            var gm = GameManager.Instance;
            if (node.floor > 0)
            {
                int steps = node.type switch
                {
                    MapNodeType.Battle => 25,
                    MapNodeType.Elite  => 35,
                    MapNodeType.Boss   => 50,
                    _ => 20
                };
                gm.taskSteps += steps;
            }

            switch (node.type)
            {
                case MapNodeType.Battle:  gm.StartBattle(false); break;
                case MapNodeType.Elite:   gm.StartBattle(true);  break;
                case MapNodeType.Chest:   gm.OpenChest();        break;
                case MapNodeType.Rest:    gm.DoRest();           break;
                case MapNodeType.Event:   gm.TriggerEvent();     break;
                case MapNodeType.Shop:    gm.OpenShop();         break;
                case MapNodeType.Boss:    gm.StartBossBattle();  break;
            }
        }

        // Generate clean circle/ring sprites
        static Sprite MakeCircle(int size, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - half + 0.5f, dy = y - half + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > half - 1.5f) px[y * size + x] = Color.clear;
                    else px[y * size + x] = fill;
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        }

        static Sprite MakeRing(int size, Color color, float thick)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - half, dy = y - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > half - 1 || d < half - thick) px[y * size + x] = Color.clear;
                    else px[y * size + x] = color;
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        }
    }
}
