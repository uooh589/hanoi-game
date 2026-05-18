using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public static class SceneBuilder
    {
        [MenuItem("Tools/Build Hanoi Game Scene")]
        public static void Build()
        {
            // Fix sprite import settings for generated PNGs
            foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { "Assets/Resources" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("_node.png"))
                {
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp != null && imp.textureType != TextureImporterType.Sprite)
                    {
                        imp.textureType = TextureImporterType.Sprite;
                        imp.spriteImportMode = SpriteImportMode.Single;
                        imp.SaveAndReimport();
                    }
                }
            }
            AssetDatabase.Refresh();

            // Clean existing scene objects
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                if (!go.scene.IsValid() || go.hideFlags != HideFlags.None) continue;
                else if (go.scene == EditorSceneManager.GetActiveScene()) Object.DestroyImmediate(go);

            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/NotoSansSC.ttf")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ========== CORE ==========
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.12f, 0.22f);
            cam.orthographic = true; cam.orthographicSize = 5;
            camGo.transform.position = new Vector3(0, 0, -10);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200, 800);
            scaler.matchWidthOrHeight = 1f; // match height, no vertical clipping at any resolution

            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var ctrlGo = new GameObject("GameController");
            var gm = ctrlGo.AddComponent<GameManager>();
            ctrlGo.AddComponent<SimpleAudio>();
            ctrlGo.GetComponent<AudioSource>().playOnAwake = false;

            var bgmGo = new GameObject("BGMPlayer", typeof(AudioSource));
            bgmGo.transform.SetParent(ctrlGo.transform);
            bgmGo.GetComponent<AudioSource>().playOnAwake = false;
            bgmGo.AddComponent<BGMPlayer>();

            ctrlGo.AddComponent<VersionManager>();
            ctrlGo.AddComponent<LogManager>();

            // ========== MAIN MENU ==========
            var menuPanel = NewPanel("MainMenuPanel", canvas.transform, false);
            menuPanel.GetComponent<Image>().sprite = LoadBgSprite("bg_mainmenu");
            menuPanel.GetComponent<Image>().color = Color.white;
            var menuUI = menuPanel.AddComponent<MainMenuUI>();
            menuUI.titleText = Txt("TitleText", menuPanel.transform, "汉诺塔：轮回", 48, new Vector2(0, 120), font);
            menuUI.newGameButton = Btn("NewGameButton", menuPanel.transform, "新游戏", new Vector2(0, 30), font);
            menuUI.continueButton = Btn("ContinueButton", menuPanel.transform, "继续", new Vector2(0, -40), font);
            menuUI.quitButton = Btn("QuitButton", menuPanel.transform, "退出", new Vector2(0, -110), font);

            // ========== STATS PANEL (shared overlay) ==========
            var statsPanel = NewPanel("StatsPanel", canvas.transform, false);
            statsPanel.SetActive(false);
            var statsUI = statsPanel.AddComponent<StatsPanelUI>();
            statsUI.statsText = Txt("StatsText", statsPanel.transform, "", 13, new Vector2(0, 30), font, 560, 560);
            statsUI.closeButton = Btn("StatsCloseBtn", statsPanel.transform, "关闭", new Vector2(0, -360), font, 180, 40);

            // ========== DECK VIEWER (shared, create first so refs work) ==========
            var deckPanel = NewPanel("DeckViewerPanel", canvas.transform, false);
            deckPanel.SetActive(false);
            var deckUI = deckPanel.AddComponent<DeckViewerUI>();
            deckUI.cardListText = Txt("DeckListText", deckPanel.transform, "", 14, new Vector2(0, 40), font, 500, 580);
            deckUI.closeButton = Btn("DeckCloseBtn", deckPanel.transform, "关闭", new Vector2(0, -360), font, 180, 40);

            // ========== ESC MENU (shared) ==========
            var escPanel = NewPanel("EscMenuPanel", canvas.transform, false);
            escPanel.SetActive(false);
            Txt("EscTitle", escPanel.transform, "菜单", 36, new Vector2(0, 220), font, 300, 50);
            var resumeBtn = Btn("EscResumeBtn", escPanel.transform, "返回游戏", new Vector2(0, 120), font, 220, 44);
            var checkBtn = Btn("EscCheckBtn", escPanel.transform, "检查更新", new Vector2(0, 60), font, 220, 44);
            var logBtn = Btn("EscLogBtn", escPanel.transform, "保存日志", new Vector2(0, 0), font, 220, 44);
            var menuBtn = Btn("EscMenuBtn", escPanel.transform, "返回主菜单", new Vector2(0, -60), font, 220, 44);
            var genshinBtn = Btn("EscGenshinBtn", escPanel.transform, "游戏官网", new Vector2(0, -120), font, 220, 44);
            var quitBtn = Btn("EscQuitBtn", escPanel.transform, "退出游戏", new Vector2(0, -180), font, 220, 44);
            var escHandler = escPanel.AddComponent<EscMenuHandler>();
            escHandler.resumeBtn = resumeBtn;
            escHandler.checkBtn = checkBtn;
            escHandler.logBtn = logBtn;
            escHandler.menuBtn = menuBtn;
            escHandler.genshinBtn = genshinBtn;
            escHandler.quitBtn = quitBtn;
            escHandler.escPanel = escPanel;
            // Volume controls
            escHandler.musicVolText = Txt("EscMusicVol", escPanel.transform, "BGM: 30%", 11, new Vector2(-100, -230), font, 120, 20);
            escHandler.sfxVolText = Txt("EscSfxVol", escPanel.transform, "音效: 10%", 11, new Vector2(100, -230), font, 120, 20);
            escHandler.musicUpBtn = Btn("EscMusicUp", escPanel.transform, "+", new Vector2(-150, -230), font, 40, 20);
            escHandler.musicDownBtn = Btn("EscMusicDn", escPanel.transform, "-", new Vector2(-50, -230), font, 40, 20);
            escHandler.sfxUpBtn = Btn("EscSfxUp", escPanel.transform, "+", new Vector2(50, -230), font, 40, 20);
            escHandler.sfxDnBtn = Btn("EscSfxDn", escPanel.transform, "-", new Vector2(150, -230), font, 40, 20);

            // ========== MAP ==========
            var mapPanel = NewPanel("MapPanel", canvas.transform, false);
            mapPanel.GetComponent<Image>().sprite = LoadBgSprite("bg_map");
            mapPanel.GetComponent<Image>().color = Color.white;
            var mapUI = mapPanel.AddComponent<MapUI>();
            var mapAreaGo = new GameObject("MapArea", typeof(RectTransform));
            mapAreaGo.transform.SetParent(mapPanel.transform, false);
            var maRt = mapAreaGo.GetComponent<RectTransform>();
            maRt.anchorMin = new Vector2(0, 0); maRt.anchorMax = new Vector2(1, 1);
            maRt.offsetMin = new Vector2(0, 100); maRt.offsetMax = new Vector2(0, -80);
            mapUI.mapArea = maRt;
            mapUI.stageText = Txt("StageText", mapPanel.transform, "第 1 层", 22, new Vector2(0, 375), font);
            mapUI.moraText = Txt("MoraText_map", mapPanel.transform, "摩拉: 0", 14, new Vector2(400, 375), font, 160, 24);
            mapUI.deckButton = Btn("MapDeckBtn", mapPanel.transform, "卡组", new Vector2(500, 375), font, 90, 30);
            mapUI.deckViewerPanel = deckPanel;

            // ========== BATTLE ==========
            BuildBattlePanel(canvas.transform, font, deckPanel);

            // ========== CARD REWARD ==========
            var rewardPanel = NewPanel("CardRewardPanel", canvas.transform, false);
            var rewardUI = rewardPanel.AddComponent<CardRewardUI>();
            rewardUI.titleText = Txt("RewardTitle", rewardPanel.transform, "战斗胜利！选择一张卡牌", 22, new Vector2(0, 150), font);
            rewardUI.descText = Txt("RewardDesc", rewardPanel.transform, "", 14, new Vector2(0, -100), font, 400, 50);
            // Hanoi preview area
            var previewArea = new GameObject("HanoiPreview", typeof(RectTransform));
            previewArea.transform.SetParent(rewardPanel.transform, false);
            var paRt = previewArea.GetComponent<RectTransform>();
            paRt.sizeDelta = new Vector2(360, 260);
            paRt.anchoredPosition = new Vector2(0, 30);
            rewardUI.hanoiPreviewArea = paRt;
            rewardUI.confirmButton = Btn("RewardConfirm", rewardPanel.transform, "选择这张卡牌", new Vector2(-120, -150), font);
            rewardUI.nextButton = Btn("RewardNext", rewardPanel.transform, "换一张 →", new Vector2(120, -150), font, 160, 40);

            // ========== EVENT ==========
            var eventPanel = NewPanel("EventPanel", canvas.transform, false);
            var eventUI = eventPanel.AddComponent<EventUI>();
            eventUI.eventText = Txt("EventText", eventPanel.transform, "事件", 18, new Vector2(0, 60), font, 500, 130);
            eventUI.choiceButtons = new Button[3];
            eventUI.choiceLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Btn($"EventChoice{i}", eventPanel.transform, "选项", new Vector2(0, -20 - i * 55), font, 350, 40);
                eventUI.choiceButtons[i] = b;
                eventUI.choiceLabels[i] = b.GetComponentInChildren<Text>();
            }

            // ========== CHEST ==========
            var chestPanel = NewPanel("ChestPanel", canvas.transform, false);
            var chestUI = chestPanel.AddComponent<ChestUI>();
            chestUI.infoText = Txt("ChestTitle", chestPanel.transform, "发现宝箱！", 28, new Vector2(0, 140), font);
            // Large desc text for card info
            chestUI.cardText = Txt("ChestDesc", chestPanel.transform, "", 16, new Vector2(0, 20), font, 500, 100);
            chestUI.okButton = Btn("ChestOk", chestPanel.transform, "领取", new Vector2(0, -130), font);

            // ========== REST ==========
            var restPanel = NewPanel("RestPanel", canvas.transform, false);
            var restUI = restPanel.AddComponent<RestUI>();
            restUI.infoText = Txt("RestTitle", restPanel.transform, "七天神像的祝福", 24, new Vector2(0, 60), font);
            restUI.effectText = Txt("RestEffect", restPanel.transform, "", 18, new Vector2(0, -30), font, 400, 80);
            restUI.continueButton = Btn("RestOk", restPanel.transform, "继续旅程", new Vector2(0, -130), font);

            // ========== SHOP ==========
            var shopPanel = NewPanel("ShopPanel", canvas.transform, false);
            var shopUI = shopPanel.AddComponent<ShopUI>();
            shopUI.titleText = Txt("ShopTitle", shopPanel.transform, "商人", 24, new Vector2(0, 140), font);
            shopUI.moraText = Txt("MoraText", shopPanel.transform, "摩拉: 0", 16, new Vector2(0, 100), font);
            shopUI.buyButtons = new Button[3];
            shopUI.buyLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float x = -230 + i * 230;
                var cardGo = NewPanel("ShopCard" + i, shopPanel.transform, false, 200, 130, new Vector2(x, -20));
                shopUI.buyButtons[i] = cardGo.AddComponent<Button>();
                shopUI.buyLabels[i] = Txt("ShopCardLabel" + i, cardGo.transform, "", 12, Vector2.zero, font, 180, 100);
            }
            shopUI.leaveButton = Btn("ShopLeave", shopPanel.transform, "离开", new Vector2(0, -150), font);

            // ========== GAME OVER ==========
            var overPanel = NewPanel("GameOverPanel", canvas.transform, false);
            var overUI = overPanel.AddComponent<GameOverUI>();
            overUI.resultText = Txt("ResultText", overPanel.transform, "败北", 48, new Vector2(0, 100), font);
            overUI.statsText = Txt("StatsText", overPanel.transform, "", 18, new Vector2(0, -20), font);
            overUI.returnButton = Btn("ReturnButton", overPanel.transform, "返回主菜单", new Vector2(0, -120), font);

            // ========== GAMEMANAGER REFS ==========
            gm.mainMenuPanel = menuPanel;
            gm.mapPanel = mapPanel;
            gm.battlePanel = GameObject.Find("BattlePanel");
            gm.cardRewardPanel = rewardPanel;
            gm.eventPanel = eventPanel;
            gm.chestPanel = chestPanel;
            gm.restPanel = restPanel;
            gm.shopPanel = shopPanel;
            gm.gameOverPanel = overPanel;
            gm.escMenuPanel = escPanel;

            // Start with main menu only
            mapPanel.SetActive(false);
            if (gm.battlePanel) gm.battlePanel.SetActive(false);
            rewardPanel.SetActive(false);
            eventPanel.SetActive(false);
            chestPanel.SetActive(false);
            restPanel.SetActive(false);
            shopPanel.SetActive(false);
            overPanel.SetActive(false);

            var scene = EditorSceneManager.GetActiveScene();
            string spath = string.IsNullOrEmpty(scene.path) ? "Assets/Main.unity" : scene.path;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, spath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] 场景已保存: " + spath);
        }

        private static void BuildBattlePanel(Transform canvasParent, Font font, GameObject deckPanel)
        {
            var panel = NewPanel("BattlePanel", canvasParent, false);

            // Player info - top left with avatar + card border
            var pi = new GameObject("PlayerInfo", typeof(RectTransform));
            pi.transform.SetParent(panel.transform, false);
            var pirt = pi.GetComponent<RectTransform>();
            pirt.anchorMin = pirt.anchorMax = new Vector2(0, 1);
            pirt.pivot = new Vector2(0, 1);
            pirt.sizeDelta = new Vector2(300, 110);
            pirt.anchoredPosition = new Vector2(15, -10);
            // Card-style dark backdrop
            var piBg = pi.gameObject.AddComponent<Image>();
            piBg.color = new Color(0.1f, 0.08f, 0.06f, 0.8f);
            piBg.raycastTarget = false;

            // Traveler avatar
            // Avatar at left edge of PlayerInfo panel
            var pAvatar = NewImage(pi.transform, "PlayerAvatar", Color.white, new Vector2(56, 56), Vector2.zero);
            pAvatar.raycastTarget = true;
            pAvatar.rectTransform.anchorMin = pAvatar.rectTransform.anchorMax = new Vector2(0, 0.5f);
            pAvatar.rectTransform.pivot = new Vector2(0, 0.5f);
            pAvatar.rectTransform.anchoredPosition = new Vector2(8, 0);
            pAvatar.gameObject.AddComponent<Button>();
            pAvatar.type = Image.Type.Simple; pAvatar.preserveAspect = true; pAvatar.raycastTarget = true;
            pAvatar.gameObject.AddComponent<Button>();

            Txt("PlayerHPText", pi.transform, "HP: 60/60", 14, new Vector2(110, -15), font, 170, 20);
            Txt("PlayerShieldText", pi.transform, "", 12, new Vector2(110, -38), font, 170, 18);
            Txt("PlayerATKText", pi.transform, "攻: 3", 12, new Vector2(110, -60), font, 170, 18);
            Txt("StepMultiplierText", pi.transform, "倍率: x1.00", 11, new Vector2(110, -80), font, 170, 18);

            // ── Enemy info: top-right, portrait left + text right ──
            var ei = new GameObject("EnemyInfo", typeof(RectTransform));
            ei.transform.SetParent(panel.transform, false);
            var eirt = ei.GetComponent<RectTransform>();
            eirt.anchorMin = eirt.anchorMax = new Vector2(1, 1);
            eirt.pivot = new Vector2(1, 1);
            eirt.sizeDelta = new Vector2(360, 150);
            eirt.anchoredPosition = new Vector2(-30, -30);

            var ePortrait = NewImage(ei.transform, "EnemyPortrait", Color.clear, new Vector2(80, 80), new Vector2(-250, -15));
            ePortrait.type = Image.Type.Simple;
            ePortrait.raycastTarget = true;
            var portraitBtn = ePortrait.gameObject.AddComponent<Button>();
            Txt("EnemyNameText", ei.transform, "敌人", 16, new Vector2(-130, 40), font, 220, 24);
            Txt("EnemyHPText", ei.transform, "", 13, new Vector2(-130, 12), font, 200, 20);
            Txt("EnemyShieldText", ei.transform, "", 12, new Vector2(-130, -8), font, 200, 18);
            Txt("EnemyIntentText", ei.transform, "准备中...", 13, new Vector2(-130, -28), font, 200, 20);
            Txt("EnemyPoisonText", ei.transform, "", 11, new Vector2(-130, -46), font, 200, 18);

            // ── Hanoi towers: bottom-center. 3 hand panels + 1 task panel ──
            var hanoiArea = new GameObject("HanoiArea", typeof(RectTransform));
            hanoiArea.transform.SetParent(panel.transform, false);
            var haRt = hanoiArea.GetComponent<RectTransform>();
            haRt.anchorMin = new Vector2(0.5f, 0);
            haRt.anchorMax = new Vector2(0.5f, 0);
            haRt.sizeDelta = new Vector2(1160, 260);
            haRt.anchoredPosition = new Vector2(0, 180);

            float[] xs = { -386, 0, 386 };
            for (int i = 0; i < 3; i++)
            {
                var hPanel = NewPanel($"HanoiPanel{i}", hanoiArea.transform, true, 370, 240, new Vector2(xs[i], 0));
                hPanel.AddComponent<HanoiUI>().handIndex = i;
            }

            // Task card overlay (hidden, shown via button)
            var taskOverlay = NewPanel("TaskOverlay", panel.transform, false);
            taskOverlay.name = "TaskOverlay";
            taskOverlay.SetActive(false);
            var taskHanoi = NewPanel("TaskHanoiArea", taskOverlay.transform, true, 500, 400, Vector2.zero);
            taskHanoi.AddComponent<HanoiUI>().handIndex = -2;
            Txt("TaskStepText", taskOverlay.transform, "任务步数: 0", 18, new Vector2(0, 220), font, 400, 36);
            Btn("TaskCloseBtn", taskOverlay.transform, "返回战斗", new Vector2(0, -230), font, 180, 40);
            Txt("TaskTitleText", taskOverlay.transform, "8层任务卡牌", 22, new Vector2(0, 260), font, 400, 36);

            // ── Battle log arrow at top-center ──
            var logArrow = Btn("LogArrowBtn", panel.transform, "▼ 战斗日志", new Vector2(0, 380), font, 140, 28);

            // ── Battle log overlay panel ──
            var logOverlay = NewPanel("LogOverlay", panel.transform, false);
            logOverlay.SetActive(false);
            Txt("LogOverlayText", logOverlay.transform, "", 14, new Vector2(0, 80), font, 600, 500);
            Btn("LogCloseBtn", logOverlay.transform, "关闭", new Vector2(0, -320), font, 180, 40);

            // ── Bottom bar: controls only ──
            var botBar = new GameObject("BottomBar", typeof(RectTransform));
            botBar.transform.SetParent(panel.transform, false);
            var bbRt = botBar.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0.5f, 0); bbRt.anchorMax = new Vector2(0.5f, 0);
            bbRt.pivot = new Vector2(0.5f, 0.5f);
            bbRt.sizeDelta = new Vector2(500, 40);
            bbRt.anchoredPosition = new Vector2(0, 20);

            Txt("StepsRemainingText", botBar.transform, "步数: 0", 14, new Vector2(-180, 0), font, 100, 22);
            Btn("TaskButton", botBar.transform, "任务", new Vector2(-80, 0), font, 60, 26);
            Btn("DeckButton", botBar.transform, "卡组", new Vector2(0, 0), font, 60, 26);
            Btn("RefreshButton", botBar.transform, "刷新(3)", new Vector2(80, 0), font, 100, 26);
            Btn("EndTurnButton", botBar.transform, "结束", new Vector2(180, 0), font, 100, 26);

            // Wire up BattleUI
            var battleUI = panel.AddComponent<BattleUI>();
            battleUI.playerAvatar = pAvatar;
            battleUI.turnAnnounceText = Txt("TurnAnnounceText", panel.transform, "", 28, new Vector2(0, 100), font, 400, 50);
            battleUI.turnAnnounceText.gameObject.SetActive(false);
            battleUI.playerHPText = GetTxt(pi.transform, "PlayerHPText");
            battleUI.playerShieldText = GetTxt(pi.transform, "PlayerShieldText");
            battleUI.playerATKText = GetTxt(pi.transform, "PlayerATKText");
            battleUI.stepMultiplierText = GetTxt(pi.transform, "StepMultiplierText");
            battleUI.enemyNameText = GetTxt(ei.transform, "EnemyNameText");
            battleUI.enemyPortrait = ei.transform.Find("EnemyPortrait")?.GetComponent<Image>();
            battleUI.enemyHPText = GetTxt(ei.transform, "EnemyHPText");
            battleUI.enemyShieldText = GetTxt(ei.transform, "EnemyShieldText");
            battleUI.enemyIntentText = GetTxt(ei.transform, "EnemyIntentText");
            battleUI.enemyPoisonText = GetTxt(ei.transform, "EnemyPoisonText");
            var logArrowBtn = panel.transform.Find("LogArrowBtn");
            battleUI.logArrowBtn = logArrowBtn?.GetComponent<Button>();
            battleUI.battleLogText = logArrowBtn?.GetComponentInChildren<Text>();
            battleUI.logOverlay = panel.transform.Find("LogOverlay")?.gameObject;
            battleUI.stepsRemainingText = GetTxt(botBar.transform, "StepsRemainingText");
            var refreshBtn = botBar.transform.Find("RefreshButton");
            battleUI.refreshButton = refreshBtn?.GetComponent<Button>();
            battleUI.refreshText = refreshBtn?.GetComponentInChildren<Text>();
            var endBtn = botBar.transform.Find("EndTurnButton");
            battleUI.endTurnButton = endBtn?.GetComponent<Button>();
            battleUI.deckButton = botBar.transform.Find("DeckButton")?.GetComponent<Button>();
            battleUI.deckViewerPanel = deckPanel;
            battleUI.statsPanel = canvasParent.Find("StatsPanel")?.gameObject;

            // HP bar images
            battleUI.playerHPBar = NewImage(pi.transform, "PlayerHPBar", Color.green, new Vector2(150, 10), new Vector2(0, -14));
            battleUI.playerShieldBar = NewImage(pi.transform, "PlayerShieldBar", Color.cyan, new Vector2(150, 6), new Vector2(0, -33));
            battleUI.enemyHPBar = NewImage(ei.transform, "EnemyHPBar", Color.red, new Vector2(150, 8), new Vector2(-130, 2));
            battleUI.enemyShieldBar = NewImage(ei.transform, "EnemyShieldBar", Color.cyan, new Vector2(150, 6), new Vector2(-130, -18));

            battleUI.hanoiUIs = new HanoiUI[] {
                hanoiArea.transform.Find("HanoiPanel0")?.GetComponent<HanoiUI>(),
                hanoiArea.transform.Find("HanoiPanel1")?.GetComponent<HanoiUI>(),
                hanoiArea.transform.Find("HanoiPanel2")?.GetComponent<HanoiUI>()
            };

            foreach (var hui in battleUI.hanoiUIs)
                if (hui != null) hui.uiFont = font;

            // Task overlay wiring
            battleUI.taskOverlay = panel.transform.Find("TaskOverlay")?.gameObject;
            battleUI.taskHanoiUI = panel.transform.Find("TaskOverlay/TaskHanoiArea")?.GetComponent<HanoiUI>();
            battleUI.taskStepText = panel.transform.Find("TaskOverlay/TaskStepText")?.GetComponent<Text>();
            var taskBtn = botBar.transform.Find("TaskButton");
            battleUI.taskButton = taskBtn?.GetComponent<Button>();
            var taskCloseBtn = panel.transform.Find("TaskOverlay/TaskCloseBtn");
            if (taskCloseBtn != null)
                battleUI.taskCloseButton = taskCloseBtn.GetComponent<Button>();

            if (battleUI.taskHanoiUI != null) battleUI.taskHanoiUI.uiFont = font;
        }

        // ========== HELPERS ==========

        private static GameObject NewPanel(string name, Transform parent, bool isSubPanel, float w = -1, float h = -1, Vector2? pos = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = isSubPanel
                ? new Color(0.2f, 0.18f, 0.15f, 0.92f)  // warm dark — Hanoi panel
                : new Color(0.1f, 0.08f, 0.06f, 0.95f);  // deep brown — full screen panel
            var rt = go.GetComponent<RectTransform>();
            if (w < 0) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
            else { rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = pos ?? Vector2.zero; }
            return go;
        }

        private static Text Txt(string name, Transform parent, string content, int fontSize, Vector2 pos, Font font, float w = 300, float h = 30)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = content; t.fontSize = fontSize; t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white; t.font = font; t.raycastTarget = false;
            t.rectTransform.sizeDelta = new Vector2(w, h);
            t.rectTransform.anchoredPosition = pos;
            return t;
        }

        private static Button Btn(string name, Transform parent, string label, Vector2 pos, Font font, float w = 200, float h = 40)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
            go.GetComponent<RectTransform>().anchoredPosition = pos;
            var btnImg = go.GetComponent<Image>();
            var spr = LoadUISprite(w < 120 ? "btn_small" : "btn_normal");
            if (spr != null) { btnImg.sprite = spr; btnImg.type = Image.Type.Sliced; btnImg.color = Color.white; }
            else btnImg.color = new Color(0.4f, 0.28f, 0.1f, 0.95f);

            var txtGo = new GameObject("Text", typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var t = txtGo.GetComponent<Text>();
            t.text = label; t.fontSize = 14; t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.88f, 0.65f); t.font = font; t.raycastTarget = false;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static Image NewImage(Transform parent, string name, Color color, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color; img.raycastTarget = false;
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = pos;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            return img;
        }

        private static Text GetTxt(Transform parent, string name)
        {
            var child = parent.Find(name);
            return child?.GetComponent<Text>();
        }

        private static Sprite LoadBgSprite(string name)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/{name}.png");
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return null;
        }

        private static Sprite LoadUISprite(string name)
        {
            return LoadBgSprite(name);
        }
    }
}
