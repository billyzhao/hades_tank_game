# 迭代 06A：协议构筑与奖励循环 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不改变地图与导航架构的前提下，交付可重复游玩的协议三选一、奖励结算与两场战斗循环。

**Architecture:** 协议由 `ProtocolDefinition` 与 `ProtocolEffectDefinition` Godot Resource 数据定义，`ContentCatalog` 负责加载前校验；`BuildController` 独占本局构筑与事件订阅，向战斗提供快照/效果。`RewardGenerator` 以局种子、房间索引、已选集合和内容目录版本产生确定性候选，`RunController` 独占房间生命周期与选择落库。

**Tech Stack:** Godot 4.7 .NET、C#、NUnit、Godot Headless 测试宿主、Godot `.tres` Resource、现有 480×270 逻辑画布。

## Global Constraints

- 必须遵守 `AGENTS.md`、`docs/superpowers/specs/2026-07-17-agent-collaboration-governance-design.md` 和 `docs/superpowers/specs/2026-07-17-iteration-06-protocol-room-loop-design.md`。
- 06A 只使用现有 `mvp_combat_room.tscn` 和 `DefenseRoutePlanner`；不得引入 `TileMapLayer`、`AStarGrid2D`、`RoomDefinition` 或第二张不同布局地图。
- 候选池固定为 10 项：前线兵工局 5、侦察电子组 2、后勤维修署 2、工程兵团 1；每次恰好三张、不重复、可复现，并保证至少一个不依赖当前流派的通用候选。
- 候选必须以局种子、房间索引、已选协议集合与 ContentCatalog 版本为输入，先按稳定 Id 排序，再排除冲突、未满足前置和满层项。
- `StatPipeline` 固定采用“基础值 → 固定值 → 加法百分比 → 乘法百分比 → 最终钳制”；`BuildController` 负责本局作用域和 `ShotFired`、`ProjectileHit`、`DashStarted`、`RelayDamaged`、`RoomCleared` 钩子。
- `RoomController` 生命周期必须为 `Loading → Intro (0.6 秒) → Combat → Cleared → Reward → Exiting`，且仅 `Combat` 可进入 `Failed`；Cleared 必须先冻结生成、清除危险炮弹、结算清场修复，再进入 Reward。
- 不能硬编码“本次奖励是哪三个协议”；所有运行时数值变化必须经过统一修正器管线。
- 单元测试只调用真实生产类型和真实 Resource/ContentCatalog 实例；禁止反射、动态代理、mock/stub/fake、测试专用生产接口或替换生产行为。
- 不依赖 Godot 原生运行时的纯 C# 管线/状态测试由 NUnit 执行；构造真实 Resource 的目录、奖励和构筑测试必须由 `godot --headless` 启动的测试宿主执行，宿主以退出码 0/非 0 报告结果。
- 未获用户可见验收前不得提交或推送。

---

## 文件结构与边界

| 路径 | 职责 |
| --- | --- |
| `game1/scripts/content/ProtocolDefinition.cs`、`ProtocolEffectDefinition.cs`、`ContentCatalog.cs` | 协议、效果和内容目录 Resource。 |
| `game1/scripts/run/RewardGenerator.cs` | 纯确定性三选一候选生成。 |
| `game1/scripts/run/StatPipeline.cs`、`BuildController.cs` | 以固定顺序计算快照，管理本局效果订阅。 |
| `game1/scripts/run/RunState.cs` | 本局持久状态、已选协议和房间索引。 |
| `game1/scripts/run/RunController.cs` | 房间状态机、奖励确认、下一场派生种子。 |
| `game1/scripts/ui/RewardPanel.cs` | 三张可点击协议卡与键盘选择。 |
| `game1/resources/protocols/*.tres` | 十项已确认协议数据。 |
| `tests/Game1.Tests/**/*Tests.cs` | 不依赖渲染的确定性与状态机测试。 |
| `game1/tests/headless/ProtocolRuntimeTestHost.cs` | 初始化 Godot 运行时，执行真实 Resource 测试并用退出码报告。 |

### Task 1: 建立可编译 API 骨架并写入真正的红灯测试

**Files:**
- Create: `game1/scripts/content/ProtocolDepartment.cs`
- Create: `game1/scripts/content/ProtocolDefinition.cs`
- Create: `game1/scripts/content/ProtocolEffectDefinition.cs`
- Create: `game1/scripts/content/ContentCatalog.cs`
- Create: `game1/scripts/run/ProtocolOffer.cs`
- Create: `game1/scripts/run/RewardGenerator.cs`
- Create: `tests/Game1.Tests/Run/RewardGeneratorTests.cs`
- Create: `tests/Game1.Tests/Run/ContentCatalogTests.cs`
- Create: `game1/tests/headless/ProtocolRuntimeTestHost.cs`

**Interfaces:**

```csharp
public enum ProtocolDepartment { Arsenal, Recon, Logistics, Engineering }
public sealed partial class ProtocolDefinition : Resource {
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public ProtocolDepartment Department { get; set; }
    [Export] public int Rarity { get; set; }
    [Export] public float BaseWeight { get; set; } = 1f;
    [Export] public string[] Tags { get; set; } = Array.Empty<string>();
    [Export] public string[] RequiredTags { get; set; } = Array.Empty<string>();
    [Export] public string[] ConflictTags { get; set; } = Array.Empty<string>();
    [Export] public int MaxStacks { get; set; } = 1;
    [Export] public ProtocolEffectDefinition[] Effects { get; set; } = Array.Empty<ProtocolEffectDefinition>();
}
public readonly record struct RewardGenerationInput(int RunSeed, int RoomIndex, IReadOnlyList<string> SelectedIds, string CatalogVersion);
public readonly record struct ProtocolOffer(int RoomIndex, IReadOnlyList<string> ProtocolIds);
public static ProtocolOffer Generate(RewardGenerationInput input, ContentCatalog catalog);
```

- [ ] **Step 1: 建立无行为 API 骨架。** 此步骤仅创建上述公开类型、构造函数与方法签名；方法体统一 `throw new NotImplementedException()`，不包含候选、校验或效果逻辑。该编译支撑步骤已由用户确认，目的仅为让下一步真实行为测试可编译执行。

- [ ] **Step 2: 写失败测试。**

```csharp
[Test]
public void Generate_SameInput_ReturnsSameThreeUniqueIdsWithGeneralChoice() {
    var input = new RewardGenerationInput(731, 0, Array.Empty<string>(), "v1");
    var first = RewardGenerator.Generate(input, TestCatalog.ValidTen);
    var again = RewardGenerator.Generate(input, TestCatalog.ValidTen);
    Assert.That(first.ProtocolIds, Is.EqualTo(again.ProtocolIds));
    Assert.That(first.ProtocolIds.Distinct().Count(), Is.EqualTo(3));
    Assert.That(first.ProtocolIds.Any(id => TestCatalog.IsGeneralChoice(id)), Is.True);
}
```

- [ ] **Step 3: 运行测试，确认红灯。** `RewardGeneratorTests` 与 `ContentCatalogTests` 必须由 Headless 宿主加载真实 Resource；StatPipeline/RunController 继续由 NUnit 执行。

Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite reward_catalog`

Expected: 非零退出，报告 `NotImplementedException`，不是 Godot native 崩溃或编译错误。

- [ ] **Step 4: 实现数据校验与确定性选择。** `ContentCatalog` 验证唯一 Id、非空效果、稀有度/基础权重/数值范围、需求可满足；`ProtocolDefinition` 保留部门、稀有度、基础权重、标签、需求标签、冲突标签和叠加上限。`RewardGenerator` 先稳定排序 Id，再依据 `RewardGenerationInput` 过滤冲突/前置/满层项，以确定性随机流按基础权重抽取，保留至少一项不依赖当前标签的通用候选。

```csharp
// 只从已排序、可用候选中选择；选择过程不能依赖资源加载顺序或字典枚举顺序。
```

- [ ] **Step 5: 补充约束测试。** 覆盖重复 Id、空/缺失效果、非法范围、无满足前置的目录、冲突标签、满层、相同输入有序相同、不同房间派生种子可变与通用候选保底；相同 SelectedIds 的不同排列结果相同；CatalogVersion 参与随机流；固定两组预计算 RunSeed/RoomIndex 断言不同有序候选，不能使用“可能不同”的偶然断言。

- [ ] **Step 6: 运行测试，确认通过。**

Run: `dotnet test tests/Game1.Tests/Game1.Tests.csproj --filter FullyQualifiedName~RewardGeneratorTests`

Expected: PASS。

### Task 2: 建立统一属性管线与本局构筑控制器

**Files:**
- Create: `game1/scripts/run/StatId.cs`
- Create: `game1/scripts/run/StatModifier.cs`
- Create: `game1/scripts/run/StatPipeline.cs`
- Create: `game1/scripts/run/BuildController.cs`
- Modify: `game1/scripts/run/RunState.cs`
- Modify: `game1/scripts/player/WeaponController.cs`
- Modify: `game1/scripts/player/DashComponent.cs`
- Create: `tests/Game1.Tests/Run/StatPipelineTests.cs`
- Create: `tests/Game1.Tests/Run/BuildControllerTests.cs`

**Interfaces:**

```csharp
public enum StatId { Damage, FireCooldown, DashCooldown, ArmorMax, RelayRepair, RelayShield }
public readonly record struct StatModifier(StatId Stat, float FlatAdd, float AdditivePercent, float MultiplicativePercent, string SourceProtocolId);
public sealed class StatPipeline {
    public float Evaluate(StatId stat, float baseValue, IEnumerable<StatModifier> modifiers);
}
```

- [ ] **Step 1: 建立无行为 API 骨架。** 先建立 StatId、StatModifier、StatPipeline、BuildController 与公开签名；全部未实现行为抛 NotImplementedException，不得实现协议效果。

- [ ] **Step 2: 写失败测试。**

```csharp
[Test]
public void Evaluate_AppliesAddsBeforeMultipliers_InStableSourceOrder() {
    var value = new StatPipeline().Evaluate(StatId.Damage, 10f, new[] {
        new StatModifier(StatId.Damage, 2f, 0f, 0f, "a"),
        new StatModifier(StatId.Damage, 0f, .5f, 0f, "b"),
        new StatModifier(StatId.Damage, 0f, 0f, 1f, "c") });
    Assert.That(value, Is.EqualTo(36f));
}
```

- [ ] **Step 3: 运行失败测试。**

Run: `dotnet test tests/Game1.Tests/Game1.Tests.csproj --filter FullyQualifiedName~StatPipelineTests`

Expected: FAIL，原因为 NotImplementedException，不是编译错误。

- [ ] **Step 3: 实现管线、RunState 与 BuildController。** `Evaluate` 必须按基础→固定→加法百分比→乘法百分比→最终钳制执行，`10 + 2、+50%、×2` 必须得到 `36`。`BuildController` 仅持有当前 Run 的选择与订阅，在 Run 结束/应用壳时解除订阅；它为 ShotFired、ProjectileHit、DashStarted、RelayDamaged、RoomCleared 创建明确效果钩子。`RunState` 保存选择、候选及房间索引，拒绝无效/重复/满层选择。

- [ ] **Step 4: 将玩家与中继站接入快照/钩子。** `WeaponController` 读取武器快照；`DashComponent` 读取冲刺快照并触发 DashStarted；中继站受击触发 RelayDamaged。不得在玩家、武器、中继站或 HUD 以协议 Id 分支改数值。实现十项协议的已确认效果，并验证反弹联动、电能履带联动以及两项清场修复在 Cleared→Reward 前可见结算。

- [ ] **Step 5: 运行全部单元测试。**

Run: `dotnet test tests/Game1.Tests/Game1.Tests.csproj --filter FullyQualifiedName~BuildControllerTests`

Expected: RED before BuildController behavior exists; after implementation, a selected real protocol affects the current Run only, and ending the Run clears selected Ids, snapshot caches, and every event subscription.

- [ ] **Step 5a: Run BuildControllerTests green.** Use real BuildController, ProtocolDefinition, ProtocolEffectDefinition and ContentCatalog instances; do not use reflection, mocks, stubs, fakes, or test-only production APIs.

Run: `dotnet test tests/Game1.Tests/Game1.Tests.csproj`

Expected: 全部通过，且原有 29 项不能回退。

### Task 3: 实现房间生命周期、奖励 UI 与第二场同布局战斗

**Files:**
- Create: `game1/scripts/run/RunController.cs`
- Create: `game1/scripts/ui/RewardPanel.cs`
- Modify: `game1/scripts/room/RoomController.cs`
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/scenes/app_root.tscn`
- Modify: `game1/scenes/ui/hud.tscn`
- Create: `tests/Game1.Tests/Run/RunControllerTests.cs`

**Interfaces:**

```csharp
public enum RoomPhase { Loading, Intro, Combat, Cleared, Reward, Exiting, Failed }
public sealed class RunController {
    public RoomPhase Phase { get; }
    public event Action<RoomPhase>? PhaseChanged;
    public void BeginRoom(); public void OnCombatCleared();
    public void Advance(double deltaSeconds); public void ChooseProtocol(string protocolId); public void OnTankDefeated();
}
```

- [ ] **Step 1: 建立无行为 API 骨架。** 先建立 RoomPhase、RunController 与 Advance(double) 的公开签名，所有行为抛 NotImplementedException；不得迁移状态或启动房间。

- [ ] **Step 2: 写失败状态机测试。** 除下例外，还必须覆盖完整 Loading→Intro→Combat→Cleared→Reward→Exiting、Advance(0.599) 仍 Intro、Advance(0.001) 才 Combat、每个非法迁移被拒绝、仅 Combat→Failed、ChooseProtocol 重复调用只开始一次下一战。

```csharp
[Test]
public void Cleared_EntersReward_ThenChoiceAdvancesToNextRoom() {
    var run = TestRunController.Create(seed: 42);
    run.BeginRoom(); run.OnCombatCleared();
    Assert.That(run.Phase, Is.EqualTo(RoomPhase.Reward));
    run.ChooseProtocol(run.CurrentOffer.ProtocolIds[0]);
    Assert.That(run.Phase, Is.EqualTo(RoomPhase.Exiting));
    Assert.That(run.State.RoomIndex, Is.EqualTo(1));
}
```

- [ ] **Step 3: 运行失败测试。**

Run: `dotnet test tests/Game1.Tests/Game1.Tests.csproj --filter FullyQualifiedName~RunControllerTests`

Expected: FAIL，原因为 NotImplementedException，不是编译错误。

- [ ] **Step 3: 实现状态机。** `RoomController` 不再在 `_Ready` 直接启动敌潮，而是由 `RunController` 依次进入 Intro（显示 0.6 秒“第 N 战区”）、Combat、Cleared、Reward、Exiting；Cleared 冻结生成、清理危险炮弹、调用 BuildController.RoomCleared 结算修复后才进入 Reward；`EnemyDirector.StartWaves(seed)` 仅在 Combat 调用。`OnTankDefeated` 只有在 Combat 且重启耗尽时进入 Failed。第二场使用 `HashCode.Combine(RunState.Seed, RunState.RoomIndex)` 派生敌潮种子，仍实例化当前同一场景。

- [ ] **Step 4: 实现可见奖励面板。** 面板展示三张协议卡的名称、部门、效果文案、已选协议列表及 `1/2/3` 快捷键；未选前暂停敌人生成与坦克输入。选择成功后由 RunController 写入 RunState/BuildController，隐藏面板、更新 HUD 的“构筑”区域并进入 Exiting；清场修复和中继拦截必须有事件栏/数值反馈。

- [ ] **Step 5: 运行测试与 Godot 验证。**

Run: `dotnet test tests/Game1.Tests/Game1.Tests.csproj; dotnet build game1/game1.csproj; godot --headless --path game1 --editor --quit`

Expected: 测试通过、编译 0 error、Godot 无场景或脚本解析错误。

### Task 4: 写入十项协议资源并完成可见验收包

**Files:**
- Create: `game1/resources/protocols/arsenal_*.tres`
- Create: `game1/resources/protocols/chassis_*.tres`
- Create: `game1/resources/protocols/relay_*.tres`
- Create: `game1/resources/protocols/recon_*.tres`
- Create: `game1/resources/protocols/universal_*.tres`
- Modify: `README.md`
- Modify: `docs/iterations/iteration-06a-protocol-reward-loop.md`

- [ ] **Step 1: 按已确认设计创建十项资源。** 精确包含前线兵工局 5（额外反弹、反弹伤害+30%、首次命中分裂、射速提升但伤害降低、重炮弹但射速降低）、侦察电子组 2（电能冲刺履带、冲刺冷却-20%）、后勤维修署 2（清场修复装甲、清场修复中继站）、工程兵团 1（中继站拦截护盾）。每项 Id、名称、标签、效果、显示文案均唯一；不得以代码数组代替 `.tres` 内容。

- [ ] **Step 2: 写内容目录测试并运行。** 测试加载全部资源后断言总数 10、部门计数 `5/2/2/1`、每个 Effects 非空、所有 Id 唯一、所有前置可满足且数值范围合法。

- [ ] **Step 3: 完成三轮可见验收。** 使用固定种子开局三次，记录：同房间候选相同；第一场清怪后可三选一；选择后 HUD 即时变化且进入第 2 场；坦克在 Combat 阵亡且无重启时进入失败页。窗口使用 1440×810。

- [ ] **Step 4: 完整验证。**

Run: `dotnet test tests/Game1.Tests/Game1.Tests.csproj; dotnet build game1/game1.csproj -c Release; godot --headless --path game1 --editor --quit`

Expected: 全部通过，编译 0 warning/0 error，编辑器无解析错误。

## 06A 自审映射

- 确定性三选一与通用保底：Task 1。
- 构筑状态及统一数值管线：Task 2。
- 完整生命周期和奖励后第二战：Task 3。
- 十项内容与策划可见验收：Task 4。
- TileMap、A*、不同布局房间：明确不在本计划，交由 06B。
