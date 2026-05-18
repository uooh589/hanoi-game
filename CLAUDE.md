# CLAUDE.md — Hanoi Game

Unity 2022.3.62f3c1 / C# / Roguelike 卡牌战斗游戏。

## Build

```bash
# 1. Rebuild scene (REQUIRED before every build)
/home/liuhan/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity -quit -batchmode -nographics \
  -projectPath /home/liuhan/hanoi-game \
  -executeMethod HanoiGame.SceneBuilder.Build

# 2. Build Windows
/home/liuhan/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity -quit -batchmode -nographics \
  -projectPath /home/liuhan/hanoi-game \
  -buildTarget Win64 -buildWindows64Player /home/liuhan/hanoi-game/Builds/Windows/HanoiGame.exe

# 3. Build Linux
/home/liuhan/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity -quit -batchmode -nographics \
  -projectPath /home/liuhan/hanoi-game \
  -buildTarget Linux64 -buildLinux64Player /home/liuhan/hanoi-game/Builds/Linux/HanoiGame
```

## Architecture

```
Assets/Scripts/
├── Core/          # Game logic
│   ├── BattleManager.cs   — 战斗回合/步数/手牌/任务卡
│   ├── CardData.cs        — 卡牌数据 + EffectPool 卡池
│   ├── DeckManager.cs     — 牌组管理
│   ├── EffectManager.cs   — 所有效果执行 (DoDamage/DoHeal)
│   ├── ElementReactions.cs— 20种元素反应
│   ├── Enemy.cs           — 敌人AI+行动
│   ├── EnemyDatabase.cs   — 45+敌人数据（含首回合模板）
│   ├── GameManager.cs     — 全局状态/存档/ESC
│   ├── HanoiPuzzle.cs     — 汉诺塔盘逻辑
│   ├── MapData.cs         — 地图生成
│   └── SaveManager.cs     — JSON持久化
├── UI/            # UI组件
│   ├── BattleUI.cs        — 战斗界面（立绘/日志/按钮/任务覆盖层）
│   ├── HanoiUI.cs         — 汉诺塔交互（拖拽/完成动画）
│   ├── MapUI.cs           — 地图（节点图标/点击/步数奖励）
│   ├── StatsPanelUI.cs    — 属性面板（点击头像）
│   ├── DeckViewerUI.cs    — 卡组查看器
│   ├── TutorialUI.cs      — 新手引导（4步+金色提示）
│   ├── EscMenuHandler.cs  — ESC菜单（检查更新/保存日志）
│   └── ...                — CardRewardUI/ChestUI/EventUI等
├── Editor/
│   └── SceneBuilder.cs    — 场景构建器（必须运行才能更新Main.unity）
└── Utilities/
    ├── BGMPlayer.cs       — BGM播放（真实MP3）
    ├── SimpleAudio.cs     — SFX合成
    ├── FontHelper.cs      — 字体加载（含CJK）
    ├── PortraitGen.cs     — 立绘加载
    ├── VersionManager.cs  — 版本管理+更新检查
    └── LogManager.cs      — 日志收集+上传
```

## 关键注意事项

- **SceneBuilder 必须跑**：任何修改 SceneBuilder.cs 或需要新 UI 元素的改动，必须先跑 `-executeMethod HanoiGame.SceneBuilder.Build` 重建场景，否则变更不生效
- **Lambda 不能序列化**：SceneBuilder 中按钮回调不要用 `() => {}`，打包后失效。用 `EscMenuHandler` 模式在 Start() 绑定
- **GameObject.Find 只找激活对象**：用 SceneBuilder 赋值的引用字段代替
- **Transform.Find 只找直接子级**：嵌套对象注意查找路径
- **UnityEngine.Random vs System.Random**：CardData.cs 有 `using System;`，必须用 `UnityEngine.Random.Range`
- **Resources.Load<Sprite> 对 PNG 无效**：用 `Resources.Load<Texture2D>` + `Sprite.Create`
- **StreamingAssets/config.txt**：版本检查URL配置在此

## 发布

```bash
./publish.sh v1.0.1 --github uooh589/hanoi-game
```
更新检查：`https://raw.githubusercontent.com/uooh589/hanoi-game/master/VERSION`
