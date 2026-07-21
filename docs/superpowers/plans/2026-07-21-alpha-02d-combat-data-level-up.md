# Alpha 02D 战斗数据与完全暂停升级实施计划

> 执行约束：单主智能体执行；用户验收前不得提交或推送。开发阶段连续完成可玩闭环，迭代完成后集中自检。

**目标：** 实现敌军战斗数据掉落、靠近收集、完全暂停的三选一属性升级、连续升级队列与波末自动回收，形成战斗内成长的第一层闭环。

**架构：** `RunState` 保存等级、经验与待处理升级次数；纯 C# `ExperienceCurve`、`ControlledStatOfferGenerator` 和 `LevelUpController` 计算升级事实；Godot `CombatDataPickup` / `CombatDataCollector` 只处理场上数据与收集动画；`AppRoot` 通过既有 `PauseCoordinator` 装配升级暂停与波末顺序。`BuildController` 仍是属性应用唯一入口，02D 只开放基础属性，三核心/协议/维护留给 02E。

**技术栈：** Godot 4.7 .NET、C#、`Resource/.tres`、NUnit、480×270 逻辑画布；不新增依赖。

## 范围与非范围

- 范围：数据掉落、近距离吸收、经验 HUD、完全暂停升级卡、基础属性三选一、连升、波末回收、0.4 秒升级保护、验收入口与 Batch 5 单张 HUD/升级样图。
- 非范围：核心选择、协议、维护、辅助系统、正式精英词条、新敌军、Boss 实战、存档 schema 扩展和 Batch 5 批量素材。
- 固定顺序：`清场 → 回收全部战斗数据 → 依次完成待升级 → 波间奖励`。
- 暂停期间战斗世界、波次计时和炮弹冻结；升级 UI 使用 `WhenPaused`，最后一项选择后才释放 `PauseReason.LevelUp`。

## 文件结构

| 文件 | 职责 |
|---|---|
| `game1/scripts/run/ExperienceCurve.cs` | 数据驱动经验阈值计算 |
| `game1/scripts/run/LevelUpController.cs` | 经验、待升级队列与选择状态唯一真值 |
| `game1/scripts/run/ControlledStatOfferGenerator.cs` | 只产出有效且不重复的基础属性三选一 |
| `game1/scripts/combat/CombatDataPickup.cs` | 单个正整数战斗数据掉落与靠近收集事实 |
| `game1/scripts/combat/CombatDataCollector.cs` | 场上掉落注册、回收与收集转发 |
| `game1/scripts/ui/LevelUpPanel.cs` | 暂停时可输入的三选一卡片面板 |
| `game1/scripts/app/AppRoot.cs` | 连接掉落、收集、升级、暂停、波末回收与 HUD |
| `game1/scenes/app/main.tscn` | 经验 HUD 与升级 UI 容器 |
| `tests/Game1.Tests/Run/*` | 曲线、候选、队列、无死选项与顺序测试 |
| `game1/tests/headless/*` | 暂停冻结、波末回收、连续升级和 UI 命令合同 |

## 实施任务

### 任务 1：经验状态与纯 C# 升级队列

- [ ] 先写 `ExperienceCurveTests`：等级 1 起始阈值、递增阈值、非法等级拒绝。
- [ ] 先写 `LevelUpControllerTests`：一次大额经验可入队多个升级；选择完成前不弹出下一项；选择后按 FIFO 进入下一项。
- [ ] 实现 `ExperienceCurve.GetRequiredExperience(int level)`、`RunState.AddExperience(int, ExperienceCurve)` 和 `RunState.TryConsumePendingLevel(out int)`；负经验必须拒绝。
- [ ] 实现 `LevelUpController.AddExperience(int)`、`Choose(string statId)`、`PendingLevelUps`、`OfferRequested` 和 `QueueDrained`。
- [ ] 运行定向 NUnit 测试转绿。

### 任务 2：受控基础属性三选一与 BuildController 应用

- [ ] 先写候选测试：每次恰有三个唯一 Id、不会给已满属性、不会给无效数值、相同种子可复现。
- [ ] 定义仅限 02D 的基础属性：最大装甲、移动速度、主炮伤害、主炮冷却、冲刺冷却；每项都有安全上限和显示名称。
- [ ] 实现 `ControlledStatOfferGenerator`，由 `BuildController` 的公开方法应用；不得绕过 `BuildController` 改玩家节点。
- [ ] 更新 `RunState`/HUD 所需快照，保证装甲上限提高时当前装甲与 `HealthComponent` 同步。
- [ ] 运行候选与应用定向测试转绿。

### 任务 3：战斗数据掉落、吸收与波末回收

- [ ] 先写 Godot headless 测试：敌军击毁后只生成正整数掉落；玩家靠近收集一次；波末回收不会遗漏或重复。
- [ ] 实现 `CombatDataPickup`（预警结束后才可收集）和 `CombatDataCollector`（注册、近距离吸收、`CollectAllAtWaveEnd`）。
- [ ] 在 `WaveDirector` 的敌军销毁事实处由 `AppRoot` 生成掉落；不得让导演管理等级、暂停或奖励。
- [ ] 在 `ArenaController` 清场与奖励之间接入回收门，确保回收产生的所有升级先完成。
- [ ] 运行掉落与回收 headless 测试转绿。

### 任务 4：完全暂停升级面板与恢复保护

- [ ] 先写暂停合同测试：升级打开后 `PauseReason.LevelUp` 存在、炮弹坐标不变、UI 按钮仍可触发；最后一项选择后才恢复；恢复时给予 0.4 秒保护。
- [ ] 新建 `LevelUpPanel`，三个卡片只显示当前 `LevelUpController` 候选，`ProcessModeEnum.WhenPaused`。
- [ ] `AppRoot` 收到候选后获取 `PauseReason.LevelUp`；收到队列清空后关闭面板、释放原因并调用 `HealthComponent.GrantInvulnerability(0.4d)`。
- [ ] 保持 Manual/FocusLost 原因独立，升级结束不得解除其他暂停原因。
- [ ] 运行暂停与连续升级 headless 测试转绿。

### 任务 5：HUD、验收入口、Batch 5 小样与集中自检

- [ ] 在 HUD 边缘加入等级与经验进度，不重新扩大 02C 已验收的战场遮挡区域。
- [ ] Debug 验收菜单增加“授予经验”“触发连续升级”“回收本波数据”三项正式命令，仅通过 `LevelUpController` / `CombatDataCollector` 公开方法。
- [ ] 生成一张 Batch 5 HUD 与三选一升级小样；待用户确认，不直接复制进 `game1/assets/`。
- [ ] 集中运行构建、NUnit、相关 Godot headless、主场景启动、暂停可见路径与 `git diff --check`。
- [ ] 更新 `docs/iterations/`，提供策划可见验收步骤；用户验收前不提交或推送。

## 02D 用户验收标准

1. 击毁敌军后能看见数据掉落；靠近自动收集，经验条增长。
2. 经验满时战斗与波次倒计时完全冻结，三张属性卡可点击。
3. 一次获得多级经验时，选完本级立即显示下一组，直到队列清空才恢复战斗。
4. 恢复后敌弹继续原位置运动，玩家有约 0.4 秒受击保护。
5. 波末残敌清空后，散落数据先自动回收、升级先结算，之后才显示现有波间确认。
6. HUD 仍不遮挡主要战场；验收菜单可快速验证单级、连升、回收和暂停。
