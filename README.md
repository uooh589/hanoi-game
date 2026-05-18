# 汉诺塔：轮回 — Tower of Hanoi: Reincarnation

Roguelike 回合制卡牌战斗游戏，以汉诺塔操作为核心玩法。Unity 2021.3 LTS+ / 2D 模板 / 1200×800 分辨率。

---

## 一、Unity 项目创建

1. 打开 Unity Hub，点击 **New Project**。
2. 模板选择 **2D Core**，项目名自定（如 `HanoiGame`）。
3. 创建后将本仓库 `Assets/Scripts/` 下所有 `.cs` 文件拖入 Unity 项目的 `Assets/Scripts/` 中，保持子目录结构：
   ```
   Assets/
   └── Scripts/
       ├── Core/
       │   ├── CardData.cs
       │   ├── HanoiPuzzle.cs
       │   ├── EffectManager.cs
       │   ├── DeckManager.cs
       │   ├── BattleManager.cs
       │   ├── Enemy.cs
       │   ├── GameManager.cs
       │   └── SaveManager.cs
       ├── UI/
       │   ├── HanoiUI.cs
       │   ├── BattleUI.cs
       │   ├── CardRewardUI.cs
       │   ├── MainMenuUI.cs
       │   └── GameOverUI.cs
       └── Utilities/
           └── SimpleAudio.cs
       ```

---

## 二、场景搭建

所有 UI 在一个场景中完成，通过面板显隐切换状态。

### 2.1 创建 Canvas

1. 在 Hierarchy 中右键 → **UI → Canvas**，自动生成 Canvas + EventSystem。
2. 选中 Canvas，Inspector 中设置：
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler** → **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1200 × 800
   - **Screen Match Mode**: Match Width Or Height, Match = 0.5
3. Canvas 下创建 4 个空面板（右键 Canvas → **UI → Panel**），分别命名为：
   - `MainMenuPanel`
   - `BattlePanel`
   - `CardRewardPanel`
   - `GameOverPanel`
4. 每个 Panel 的 RectTransform 全屏拉伸：
   - Anchor Preset: stretch-stretch (右下角 Alt+点击 stretch)
   - Left/Right/Top/Bottom 均为 0

### 2.2 持久化 GameObject

在场景根创建空 GameObject，命名为 `GameController`，挂载以下脚本：
- `GameManager`
- `SimpleAudio`（会自动添加 AudioSource 组件）

### 2.3 主菜单 (MainMenuPanel)

在 `MainMenuPanel` 下创建：
```
MainMenuPanel/
├── TitleText (UI → Text)
│   - 内容："汉诺塔：轮回"，字号 48，居中，Y 位置 ~150
├── NewGameButton (UI → Button)
│   - Text: "新游戏"，Y 位置 ~0
├── ContinueButton (UI → Button)
│   - Text: "继续"，Y 位置 ~-60
└── QuitButton (UI → Button)
    - Text: "退出"，Y 位置 ~-120
```

挂载 `MainMenuUI` 脚本到 `MainMenuPanel`，将三个按钮拖入对应字段。

### 2.4 战斗界面 (BattlePanel)

这是最复杂的界面。`BattlePanel` 挂载 `BattleUI` 脚本。

#### 2.4.1 上半部 — 战斗信息区（顶部 60%）

创建布局如下（建议使用空 GameObject 分组）：

```
BattlePanel/
├── PlayerInfo (空节点，锚定左上)
│   ├── PlayerHPBar (UI → Slider, 只读)
│   ├── PlayerHPText (UI → Text)
│   ├── PlayerShieldBar (UI → Slider, 只读)
│   ├── PlayerShieldText (UI → Text)
│   ├── PlayerATKText (UI → Text)
│   └── StepMultiplierText (UI → Text)
├── EnemyInfo (空节点，锚定右上)
│   ├── EnemyNameText (UI → Text)
│   ├── EnemyHPBar (UI → Slider, 只读)
│   ├── EnemyHPText (UI → Text)
│   ├── EnemyShieldBar (UI → Slider, 只读)
│   ├── EnemyShieldText (UI → Text)
│   ├── EnemyIntentText (UI → Text)
│   └── EnemyPoisonText (UI → Text)
├── BattleLogArea (锚定中上)
│   └── BattleLogText (UI → Text, 配合 ScrollRect 实现滚动)
├── ControlBar (锚定中上，在日志下方)
│   ├── StepsRemainingText (UI → Text)
│   ├── EndTurnButton (UI → Button, Text: "结束回合")
│   └── RefreshButton (UI → Button, Text: "刷新")
```

#### 2.4.2 下半部 — 汉诺塔区域（底部 40%）

```
BattlePanel/
└── HanoiArea (空节点，锚定底部，高度 320)
    ├── HanoiPanel0 (UI → Panel, 宽 350 高 320)
    ├── HanoiPanel1 (UI → Panel, 宽 350 高 320)
    └── HanoiPanel2 (UI → Panel, 宽 350 高 320)
```

- 三个 HanoiPanel 使用 **Horizontal Layout Group**（可选）居中对齐，间距 25。
- 或者手动设置位置：
  - Panel0: 锚定中下，Pos X = -380
  - Panel1: 锚定中下，Pos X = 0
  - Panel2: 锚定中下，Pos X = 380

每个 `HanoiPanel` 挂载 `HanoiUI` 脚本，**Hand Index** 分别设为 0、1、2。

### 2.5 选牌界面 (CardRewardPanel)

```
CardRewardPanel/
├── TitleText (UI → Text, "选择一张卡牌")
├── CardOption0 (UI → Button, 宽 300 高 200)
│   ├── CardNameText (UI → Text)
│   └── CardDescText (UI → Text)
├── CardOption1 (同 CardOption0)
├── CardOption2 (同 CardOption0)
└── ConfirmButton (UI → Button, "确认")
```

三张卡牌按钮水平排列，间距 20。
挂载 `CardRewardUI` 脚本到 `CardRewardPanel`，拖入：
- Card Buttons (3个), Card Name Texts (3个), Card Desc Texts (3个), Card Borders (3个 Image)
- Title Text, Confirm Button, Confirm Text

### 2.6 游戏结束界面 (GameOverPanel)

```
GameOverPanel/
├── ResultText (UI → Text, "败北", 字号 48)
├── StatsText (UI → Text, 显示统计)
└── ReturnButton (UI → Button, "返回主菜单")
```

挂载 `GameOverUI` 脚本到 `GameOverPanel`，拖入对应引用。

---

## 三、GameController 引用赋值

选中 `GameController` 节点，在 Inspector 的 **GameManager** 组件中：

| 字段 | 赋值 |
|------|------|
| Main Menu Panel | 拖入 `MainMenuPanel` |
| Battle Panel | 拖入 `BattlePanel` |
| Card Reward Panel | 拖入 `CardRewardPanel` |
| Game Over Panel | 拖入 `GameOverPanel` |

**BattleUI** 组件（在 `BattlePanel` 上）中，将对应的 UI 元素拖入字段（HP bars、Texts、Buttons）。

**Hanoi UIs** 数组：将三个 `HanoiPanel` 拖入数组的 3 个槽位。

---

## 四、HanoiPuzzle 预制体

HanoiUI 脚本会在运行时**动态创建**所有盘子、柱子的 UI 元素，无需预制体。

但是如果你希望预制化柱子外观，可以：

1. 在 HanoiPanel 下创建 `PegPrefab`（Image，宽 8、高 210，灰色）
2. 在 HanoiPanel 下创建 `PlatformPrefab`（Image，宽 100、高 6，深灰色）
3. 在 HanoiUI.cs 中修改 `CreateBaseVisuals()` 方法，用 Instantiate 替换现有的 CreateRect 调用。

默认实现完全由代码生成，**无需外部资源文件**。

---

## 五、卡牌效果扩展指南

### 5.1 添加新效果类型

1. 在 `CardData.cs` 的 `EffectType` 枚举末尾添加新值：
   ```csharp
   public enum EffectType
   {
       // ... existing ...
       MyNewEffect,  // 新增
   }
   ```

2. 在 `EffectManager.cs` 的 `Execute()` 方法 switch 中添加 case：
   ```csharp
   case EffectType.MyNewEffect:
   {
       // 实现效果逻辑
       // 使用 card.effectValue1, card.effectValue2, card.effectValueF
       // 通过 battle 对象操作战斗状态
       return "效果描述文本";
   }
   ```

3. 在 `CardData.cs` 的 `EffectPool.Pool` 中将新效果注册到对应层数：
   ```csharp
   [5] = new List<EffectDef>
   {
       // ... existing ...
       new EffectDef
       {
           type = EffectType.MyNewEffect,
           v1 = 10, v2 = 3, vf = 0.2f,
           descTemplate = "造成{0}点伤害，附加{1}回合效果({2}%)"
       },
   },
   ```

### 5.2 BattleManager 可用接口

| 方法 | 说明 |
|------|------|
| `DealDamageToEnemy(int, bool pierce)` | 对敌人造成伤害，pierce 无视护盾 |
| `AddShield(int)` | 给玩家加护盾 |
| `HealPlayer(int)` | 回复玩家生命（不超上限） |
| `DrawCards(int)` | 抽牌到空手牌位 |
| `AddBattleLog(string)` | 添加战斗日志 |

公开字段直接修改：`playerHP`、`playerShield`、`stepsRemaining`、`refreshCharges`、`enemyStunTurns`、`comboMultiplier` 等。

---

## 六、存档文件格式

存档路径：`Application.persistentDataPath/hanoi_save.json`

```json
{
  "deckEntries": [
    {
      "towerLevel": 3,
      "effectTypeName": "PureDamage",
      "effectValue1": 5,
      "effectValue2": 0,
      "effectValueF": 0.0,
      "isTaskCard": false,
      "effectDescription": "造成5点伤害",
      "id": "a1b2c3d4"
    }
  ],
  "stepMultiplier": 1.1,
  "permanentATKBonus": 2,
  "permanentThorns": 0,
  "maxHPBonus": 0,
  "currentStage": 3,
  "hasTaskCard": true,
  "taskPeg0Items": [8, 5, 2],
  "taskPeg1Items": [7, 6, 4],
  "taskPeg2Items": [3, 1],
  "taskStepsUsed": 42
}
```

字段说明：
- `deckEntries[]`: 卡组所有卡牌
- `stepMultiplier`: 步数倍率（初始 1.0，每完成一次任务牌 ×1.1）
- `permanentATKBonus`: 永久攻击力加成
- `currentStage`: 当前关卡数
- `taskPeg{0,1,2}Items[]`: 8 层任务牌的三个柱子状态（数字代表盘子大小，1最小、8最大）
- `taskStepsUsed`: 任务牌已使用步数

---

## 七、游戏平衡调参指南

### 关键参数位置

| 参数 | 文件 | 位置 |
|------|------|------|
| 初始步数倍率 | `GameManager.cs:31` | `stepMultiplier = 1.0f` |
| 任务牌倍率增量 | `CardData.cs` Pool[8] | `vf = 0.1f` (即 ×1.1) |
| 玩家初始生命 | `BattleManager.cs:18` | `playerMaxHP = 50` |
| 基础攻击力 | `BattleManager.cs:20` | `baseATK = 5` |
| 刷新次数 | `BattleManager.cs:28` | `maxRefreshCharges = 3` |
| 敌人生命公式 | `Enemy.cs:75` | `(30 + stage * 20) * scaling` |
| 敌人攻击公式 | `Enemy.cs:76` | `(6 + stage * 3) * scaling` |
| 敌人 scaling | `Enemy.cs:72` | `1f + (stage - 1) * 0.4f` |
| 各层伤害值 | `CardData.cs` Pool 字典 | 每组 `EffectDef` 的 v1/v2/vf |
| 各层护盾值 | `CardData.cs` Pool 字典 | 同上 |

### 难度曲线

| 关卡 | 敌人名 | HP | 攻击 | 可解锁技能 |
|------|--------|-----|------|------------|
| 1 | 史莱姆 | ~50 | ~8 | 仅攻击 |
| 2 | 骷髅兵 | ~98 | ~13 | 攻击、减步、护盾 |
| 3 | 暗影法师 | ~155 | ~18 | +弃牌、回复、封锁 |
| 4+ | 全部解锁 | — | — | 重击、中毒、削弱 |

---

## 八、运行测试

1. 打开 `File → Build Settings`，确认场景已添加。
2. 点击 Play 进入游戏。
3. 点击主菜单"新游戏"开始。
4. 鼠标点击汉诺塔柱子选中顶部盘子，再点击目标柱子移动。
5. 完成任意汉诺塔后效果立即触发，面板刷新。
6. 步数用尽或点击"结束回合"后，敌人行动。
7. 胜利后从三张牌中选一张加入卡组。

---

## 九、无资源文件运行

游戏完全自包含，无需外部图片、音频或字体：
- **UI 元素**：由 `HanoiUI.cs` 使用 Unity 原生 Image 组件动态生成
- **音效**：由 `SimpleAudio.cs` 使用 `AudioClip.Create()` 合成正弦波
- **字体**：使用 `Resources.GetBuiltinResource<Font>("Arial.ttf")` 内置字体

---

## 十、文件清单

```
Assets/Scripts/
├── Core/
│   ├── CardData.cs          — 卡牌数据结构、效果枚举、效果池、初始卡组
│   ├── HanoiPuzzle.cs       — 汉诺塔核心逻辑（移动、验证、随机生成、完成检测）
│   ├── EffectManager.cs     — 效果执行器（28 种效果的 switch）
│   ├── DeckManager.cs       — 卡组管理（抽牌、洗牌、奖励生成）
│   ├── BattleManager.cs     — 战斗流程（回合、步数、刷新、效果链）
│   ├── Enemy.cs             — 敌人属性、AI 意图系统、行动执行
│   ├── GameManager.cs       — 全局状态机、场景管理、永久属性
│   └── SaveManager.cs       — JSON 存档/读档
├── UI/
│   ├── HanoiUI.cs           — 汉诺塔面板渲染与交互（动态 UI + 动画）
│   ├── BattleUI.cs          — 战斗界面更新（HP、按钮、日志）
│   ├── CardRewardUI.cs      — 战后三选一界面
│   ├── MainMenuUI.cs        — 主菜单
│   └── GameOverUI.cs        — 游戏结束画面
└── Utilities/
    └── SimpleAudio.cs       — 合成音效（单例，无需音频文件）
```
