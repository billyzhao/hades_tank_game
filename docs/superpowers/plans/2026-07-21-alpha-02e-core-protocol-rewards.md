# Alpha 02E 三核心与协议奖励实施计划

> 执行约束：单主智能体在当前工作区内执行；不引入依赖；每个迭代完成集中自检并交由用户验收后才提交推送。

**目标：** 将现有五波之间的占位确认替换为可见、可选择、确定性的核心/协议/维护奖励闭环。

**架构：** `RunState` 记录已选核心、协议的 Mk.I–III 阶级与构筑标签；`BuildController` 仍是唯一应用属性的入口。纯 C# 的 `CoreCatalog`、`RewardController` 和 `RewardGenerator` 依赖种子、竞技场、波次、核心及已拥有构筑生成三选一；Godot UI 只显示当前候选并提交选择，`AppRoot` 保持奖励时序和暂停协调。

**技术栈：** Godot 4.7 .NET / C#，NUnit (`net10.0`)，Godot headless 验收场景。

## 固定范围

- 三核心：突破重炮、过载速射、电驱游骑；核心只提供基础节奏和软权重，不锁部门或路线。
- 第一批内容数据：16 个普通协议、4 个稀有协议；每个协议只能按 Mk.I → Mk.II → Mk.III 提升，Mk.III 退出奖励池。
- 波 1/3 发普通协议、波 2/4 发维护、波 5 发稀有协议；装甲低于最大值 30% 时，维护三选一必含修复 25% 最大装甲。
- Boss 1 进化只建立数据与三选一 UI 合同，不接入 Boss 战斗效果；自动辅助系统留给 Alpha 02F。
- 不恢复中继站、基地、商店、装备栏或第二生命线。

## 文件职责

| 文件 | 职责 |
|---|---|
| `game1/scripts/run/CoreId.cs`、`CoreDefinition.cs`、`CoreCatalog.cs` | 三核心静态定义与初始基础修正 |
| `game1/scripts/run/ProtocolRank.cs`、`OwnedProtocol.cs` | 协议分阶运行时事实 |
| `game1/scripts/run/RewardChoice.cs`、`RewardOffer.cs`、`RewardController.cs` | 奖励三选一、确定性生成和确认状态 |
| `game1/scripts/run/RunState.cs` | 核心、已拥有协议阶级及当前奖励唯一真值 |
| `game1/scripts/run/BuildController.cs` | 应用核心初始修正、协议各阶效果及维护效果 |
| `game1/scripts/ui/CoreSelectionPanel.cs`、`WaveRewardPanel.cs` | 开局核心选择和波间三张完整奖励卡 |
| `game1/scripts/app/AppRoot.cs` | 正常局奖励时序、暂停、HUD/验收入口与运行时装配 |
| `tests/Game1.Tests/Run/*`、`game1/tests/headless/*` | 随机性、阶级、维护阈值、暂停/波间可见闭环 |

## 任务 1：核心与协议阶级纯状态

**文件：** 新建 `CoreId.cs`、`CoreDefinition.cs`、`CoreCatalog.cs`、`ProtocolRank.cs`、`OwnedProtocol.cs`；修改 `RunState.cs`、`BuildController.cs`；测试 `tests/Game1.Tests/Run/CoreCatalogTests.cs`、`ProtocolRankTests.cs`。

- [ ] 写失败测试：三核心 Id 唯一；各核心只影响其定义的基础属性与构筑标签；未选核心不能重复选择；新协议首次为 Mk.I，重复时依次至 Mk.III，满阶拒绝。
- [ ] 运行 `dotnet test GodotTank.sln --no-restore --filter "FullyQualifiedName~CoreCatalogTests|FullyQualifiedName~ProtocolRankTests"`，确认测试因缺少类型/行为失败。
- [ ] 实现 `CoreCatalog.Get(CoreId)`、`RunState.SelectCore(CoreId)`、`RunState.GetProtocolRank(string)` 和 `RunState.UpgradeProtocol(string)`；禁止直接写入协议列表。
- [ ] `BuildController` 通过 `ApplyCore(CoreDefinition)` 与 `ApplyProtocolRank(OwnedProtocol)` 唯一写入属性修正，并触发 `SnapshotChanged`。
- [ ] 再次运行上述定向测试，预期全绿。

## 任务 2：确定性奖励与低装甲维护保障

**文件：** 新建 `RewardChoice.cs`、`RewardOffer.cs`、`RewardController.cs`；修改 `RewardGenerator.cs`、`RewardGenerationInput.cs`、`ContentCatalog` 的协议内容；测试 `RewardControllerTests.cs`。

- [ ] 写失败测试：相同种子/竞技场/波次/核心/构筑输入产生完全相同三项；三项 Id 唯一；已 Mk.III 协议不会出现；核心标签只提高相关协议出现权重；装甲 `< 30%` 时维护候选含“修复 25% 最大装甲”，`>= 30%` 时不强制。
- [ ] 运行 `dotnet test GodotTank.sln --no-restore --filter "FullyQualifiedName~RewardControllerTests"`，确认失败点为奖励控制器缺失。
- [ ] 实现 `RewardController.Generate(RewardGenerationInput)` 与 `Choose(string choiceId)`；普通、稀有和维护候选均只由该控制器生成，不让 UI 或 `ArenaController` 判定资格。
- [ ] 建立 16 普通/4 稀有协议的 Resource 数据；每个定义提供三阶效果、部门、标签及安全上限，禁止中继站相关描述或效果。
- [ ] 运行定向测试，预期全绿。

## 任务 3：可验收的核心选择与波间奖励卡

**文件：** 新建 `CoreSelectionPanel.cs`、`core_selection_panel.tscn`；修改 `WaveRewardPanel.cs`、`main.tscn`、`AppRoot.cs`；测试 `game1/tests/headless/core_reward_flow_test_host.tscn`。

- [ ] 写 headless 失败场景：开局暂停并显示三核心；选择一项后恢复且 HUD 标识所选核心；第 1/3/5 波显示三张协议卡，第 2/4 波显示维护卡；卡片确认后只应用一次并进入下一波。
- [ ] 运行 `core_reward_flow_test_host.tscn`，确认失败原因是面板/奖励流未接入。
- [ ] 核心选择和波间奖励卡均设 `ProcessModeEnum.WhenPaused`；卡片文本必须显示名称、Mk 阶级、效果、部门/标签，按钮只调用 `RewardController.Choose`。
- [ ] `AppRoot` 固定执行 `清场 → 回收数据 → 消耗所有升级队列 → 波间奖励 → 下一波`，不允许验收命令绕开状态机。
- [ ] 运行 headless 场景，预期 `[PASS] core_reward_flow`。

## 任务 4：验收入口、完整自检与文档

**文件：** 修改 `AcceptanceMenu.cs`、`AppRoot.cs`、`docs/iterations/iteration-alpha-02e-*.md`、素材索引（只登记已确认样图）。

- [ ] 增加可见验收命令：选择指定核心、授予普通/维护/稀有奖励、将装甲设为 29%、结束当前波；命令必须走生产控制器。
- [ ] 验收文档按“操作 → 预期画面/数值/状态变化”写出核心节奏差异、Mk.I–III、满级过滤、30% 修理边界与暂停恢复。
- [ ] 集中运行 `dotnet build GodotTank.sln --no-restore`、`dotnet test GodotTank.sln --no-restore`、相关 Godot headless、`main.tscn` headless、实际 Godot 运行路径与 `git diff --check`。
- [ ] 用户验收通过前不提交或推送。

## 自检覆盖表

| 需求 | 覆盖任务 |
|---|---|
| 三核心不同操作节奏，且不锁路线 | 任务 1、3 |
| 受控随机、唯一三选一、核心软权重 | 任务 2 |
| Mk.I–III 与满阶退出 | 任务 1、2、3 |
| 30% 修理保障 | 任务 2、3、4 |
| 现有波末数据/升级顺序不回归 | 任务 3、4 |
| 策划可操作验收 | 任务 4 |
