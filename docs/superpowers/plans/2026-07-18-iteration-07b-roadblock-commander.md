# 迭代 07B：路障指挥车完整战斗与结算 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将已验收的路障指挥车骨架扩展为有可读预警的两阶段 Boss 战，并在击败后提供本局结果、重试与返回入口。

**Architecture:** 保持 `BossDefinition + BossRoom` 独立于普通 `RoomDefinition`；`BossEncounterController` 编排阶段循环，`RoadblockCommander` 只维护实体、移动、受伤害资格和冲锋。运行时路障与指定砖墙通过共享的 `RoomNavigationFactory` 刷新同一份 A* 路径提供器；结果 UI 只发出请求，`AppRoot` 是重试/返回和房间清理的唯一入口。

**Tech Stack:** Godot 4.7 .NET、C#、`TileMapLayer`、`AStarGrid2D`、现有 Godot headless 测试宿主。

## Global Constraints

- 保持 Godot 4.7 .NET/C#、480×270 逻辑画布和 1440×810 默认窗口；不增加外部依赖、素材包或音频包。
- 继续使用已确认的 `BossDefinition + BossRoom`，不将 Boss 房强行纳入普通 `RoomDefinition` 波次体系。
- 不新增普通敌军、协议、房间、存档、对象池或通用行为树；Boss 召唤物仅复用现有巡逻坦克与 A*。
- 预警必须可见；运行时仅改房间实例的 TileMap 单元，不写回 TileSet/`.tres` 资源。
- 用户可见验收通过前不提交、不推送，也不暂存 `.superpowers/`。

## 2026-07-18 已确认的执行节奏调整

保持 07B 已确认的玩法范围、技术架构和最终验收标准不变，但执行顺序改为：

1. **可玩闭环优先：** 先完成第一阶段核心攻防、第二阶段冲锋/脆弱和最简胜利结果，使玩家可完整体验 Boss 战。
2. **补强机制：** 再加入机枪哨、巡逻召唤、敌对实体清理和边界覆盖。
3. **交付门禁：** 最后执行全量构建、headless 套件、Godot 可见自检和策划验收脚本。

迭代开发阶段连续完成上述功能块；不再对每个私有接口、方法或小型数据字段变动执行构建、测试或进度门禁。完整构建、全部 headless 套件、Godot 启动、可见自检和回归统一在 07B 开发内容完成后执行；开发中仅对无法继续的编译错误、明确失败修复和高风险规则使用单个定向检查。该调整由用户于 2026-07-18 确认，属于交付节奏调整，不缩减任何 07B 验收项。

---

## File Structure

| 路径 | 职责 |
|---|---|
| `game1/scripts/navigation/RoomNavigationFactory.cs` | 从 Structure/砖墙生成并刷新共享 A* provider。 |
| `game1/scripts/terrain/TileTerrainAdapter.cs` | 公开一次性销毁指定砖墙单元的安全接口。 |
| `game1/scripts/bosses/BarrierDeployment.cs` | 校验候选格、预警、写入/移除运行时钢制路障。 |
| `game1/scripts/bosses/BossGunEmplacement.cs` | Boss 专属固定机枪哨：预警后短点射。 |
| `game1/scripts/bosses/BossSummonController.cs` | 复用巡逻坦克、威胁上限和共享路径。 |
| `game1/scripts/bosses/RoadblockCommander.cs` | 锚点移动、扇形炮、冲锋、无敌/脆弱窗口。 |
| `game1/scripts/bosses/BossEncounterController.cs` | 订阅阶段，编排 Phase 1/2，并保证击败清理只发生一次。 |
| `game1/scripts/ui/RunResultScreen.cs` | 读取结果快照，向外发 Retry/Return 请求。 |
| `game1/scripts/run/RunResultSnapshot.cs` | 只读结果数据与耗时格式化。 |
| `game1/scripts/app/AppRoot.cs` | 绑定 Boss 战、创建快照，处理 Retry/Return。 |
| `game1/scenes/rooms/mvp_boss_room.tscn` | 加入砖墙、适配器、BossEncounterController 和可冲锋结构布局。 |
| `game1/tests/headless/ProtocolRuntimeTestHost.cs` | 07B 的纯逻辑和 Godot 集成红绿测试。 |
| `docs/iterations/iteration-07b-roadblock-commander.md` | 门禁、证据和策划验收脚本。 |

### Task 1: 房间导航工厂与可控砖墙

**Files:**
- Create: `game1/scripts/navigation/RoomNavigationFactory.cs`
- Modify: `game1/scripts/terrain/TileTerrainAdapter.cs`
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`

**Interfaces:**

```csharp
public sealed class RoomNavigationFactory : IDisposable {
    public IEnemyPathProvider Provider { get; }
    public RoomNavigationFactory(Node2D room, Vector2I gridSize, int cellSize);
    public void Rebuild();
    public void Dispose();
}
public bool TileTerrainAdapter.DestroyBrick(Vector2I cell);
```

- [x] **Step 1: 写失败测试。** 在 `navigation_grid` 套件增加：通过 `RoomNavigationFactory` 构建的 provider 在完整砖墙时无路径，调用 `DestroyBrick(new Vector2I(2, 1))` 后同一个 provider 立即给出路径；重复销毁返回 `false` 且只产生一次 `BrickDestroyed`。
- [x] **Step 2: 运行红灯测试。**

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite navigation_grid`

  Expected: FAIL，因为工厂和 `DestroyBrick` 尚不存在。
- [x] **Step 3: 实现最小代码。** `RoomNavigationFactory` 保存 `NavigationGrid`、房间 `Structure` 与可选 `TileTerrainAdapter`；`Rebuild()` 合并两者的已用/阻塞格并调用 `grid.Rebuild`，构造时订阅 `BrickDestroyed`，`Dispose()` 解订阅。`TileTerrainAdapter.DestroyBrick` 仅在该格仍存活时移除 Tile、字典和阻塞标记、发出一次事件。`AppRoot` 的普通房间路径创建替换为该工厂，工厂随房间清理而释放。
- [x] **Step 4: 运行绿灯和回归。**

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite navigation_grid`

  Expected: `navigation_grid: 4 / 4 passed` 或更高，所有路径测试通过。

### Task 2: 路障合法性、预警与第一阶段编排

**Files:**
- Create: `game1/scripts/bosses/BarrierDeployment.cs`
- Create: `game1/scripts/bosses/BossGunEmplacement.cs`
- Create: `game1/scripts/bosses/BossSummonController.cs`
- Create: `game1/scripts/bosses/BossEncounterController.cs`
- Modify: `game1/scripts/bosses/RoadblockCommander.cs`
- Modify: `game1/scenes/rooms/mvp_boss_room.tscn`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`

**Interfaces:**

```csharp
public partial class BarrierDeployment : Node {
    public bool IsLegalCell(Vector2I cell, Vector2 playerPosition, Vector2 relayPosition);
    public void PreviewAndDeploy(Vector2I cell, double previewSeconds = .8d);
    public void ClearAll();
}
public partial class BossEncounterController : Node {
    public void Initialize(RoadblockCommander boss, IEnemyPathProvider pathProvider);
    public void StopEncounter();
}
```

- [ ] **Step 1: 写失败测试。** 在 `boss_encounter` 套件断言候选格若为 Structure、砖墙、玩家/中继站所在格或部署后令任一预设锚点到中继站无路径，则 `IsLegalCell` 返回 `false`；合法格在 `.8` 秒预警前不写 Tile，预警结束后写入 Structure 并使共享路径重新计算。
- [ ] **Step 2: 运行红灯测试。**

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter`

  Expected: FAIL，因为路障编排组件不存在。
- [ ] **Step 3: 实现第一阶段。** `BarrierDeployment` 以 TileMap 的格中心做距离校验，部署前用 `NavigationGrid.FindPath` 校验玩家格和 Boss 锚点仍可到达中继站；预警用半透明 `Polygon2D`，确认时仅在运行时 `Structure.SetCell`，然后调用导航工厂重建。`RoadblockCommander` 在三个导出的锚点间匀速移动，每 2.6 秒向玩家发 3 发 -14°/0°/+14° 扇形炮。`BossGunEmplacement` 先闪烁 `.8` 秒后对当前方向发 3 发短点射；`BossSummonController` 最多保有 2 辆巡逻坦克，实例化后注入同一 provider。`BossEncounterController` 仅在 PhaseOne 启动这些循环，PhaseTwo/Defeated 立即停止新循环。
- [ ] **Step 4: 运行绿灯和可见冒烟。**

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter`

  Expected: 路障合法性、延迟部署、威胁上限和阶段停止断言全部 PASS。

  Run: `godot --headless --path game1 --quit-after 180`

  Expected: 进程以 0 退出且无 C# 解析错误。

### Task 3: 第二阶段通道、冲锋和脆弱窗口

**Files:**
- Modify: `game1/scripts/bosses/RoadblockCommander.cs`
- Modify: `game1/scripts/bosses/BossEncounterController.cs`
- Modify: `game1/scenes/rooms/mvp_boss_room.tscn`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`

**Interfaces:**

```csharp
public bool RoadblockCommander.IsDamageable { get; }
public void RoadblockCommander.BeginCharge(Vector2 relayPosition);
public void RoadblockCommander.InterruptCharge();
```

- [ ] **Step 1: 写失败测试。** 断言 Boss 首次进入 PhaseTwo 时只销毁一次指定砖墙列表，销毁后同一导航 provider 有通路；`BeginCharge` 先维持 `.8` 秒线性预警，冲锋期间 `ApplyDamage` 返回零伤害；模拟撞击钢墙后速度归零、`IsDamageable` 在 1.5 秒内为真，窗口结束后为假，且可再次准备冲锋。
- [ ] **Step 2: 运行红灯测试。**

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter`

  Expected: FAIL，因为第二阶段行为尚未实现。
- [ ] **Step 3: 实现最小代码。** `BossEncounterController` 用一次性 PhaseChanged 订阅调用 `DestroyBrick` 打开场景中明确配置的三格冲锋道。`RoadblockCommander` 的冲锋状态枚举为 `Idle/Telegraph/Charging/Vulnerable/Cooldown`；Telegraph 绘制中继站方向线 `.8` 秒，Charging 期间以碰撞检测移动且免疫，命中 `world_steel` 后进入 `Vulnerable` 1.5 秒。非脆弱且处于第二阶段的 `ApplyDamage` 返回 `DamageResult.None`；PhaseOne 保持原有可受伤害语义。不得改写 `BossPhaseController`。
- [ ] **Step 4: 运行绿灯。**

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter`

  Expected: 通道、预警、免疫、钢墙打断和 1.5 秒窗口断言全部 PASS。

### Task 4: 击败清理与结果快照

**Files:**
- Create: `game1/scripts/run/RunResultSnapshot.cs`
- Create: `game1/scripts/ui/RunResultScreen.cs`
- Modify: `game1/scripts/bosses/BossEncounterController.cs`
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`

**Interfaces:**

```csharp
public readonly record struct RunResultSnapshot(int Seed, IReadOnlyList<string> ProtocolIds, int RelayIntegrity, TimeSpan Elapsed);
public partial class RunResultScreen : Control {
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void ReturnRequestedEventHandler();
    public void ShowResult(RunResultSnapshot snapshot);
    public void HideResult();
}
```

- [ ] **Step 1: 写失败测试。** 断言 `RunResultSnapshot` 保存种子、协议顺序、中继站完整度和耗时；Boss `Defeated` 连续触发两次只清除敌军组与敌方弹体组一次，并只显示一张结果页；结果页面标签含 seed、协议列表、`中继站 X/100` 和 `mm:ss`。
- [ ] **Step 2: 运行红灯测试。**

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter`

  Expected: FAIL，因为快照和结果 UI 尚不存在。
- [ ] **Step 3: 实现最小代码。** `AppRoot` 在新局开始记录 `Time.GetTicksMsec()`；Boss 进入时由 encounter 追踪，击败时停止所有计时器、`QueueFree` 组 `enemies` 以及敌方弹体组，创建只读快照并显示 `RunResultScreen`。结果屏的按钮只 emit 请求；`AppRoot` 的 Retry 创建新的 `RunState` 和新的 `BuildController/RunController`，回到首战房；Return 清空房间、解绑所有 Boss/导航对象，隐藏战斗 HUD，显示明确的“返回基地 / 点击开始新局”壳层。不得让 UI 直接修改 `RunState`。
- [ ] **Step 4: 运行绿灯。**

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter`

  Expected: 快照、击败一次性清理和 UI 请求断言全部 PASS。

### Task 5: 可见验收入口、文档和全量自检

**Files:**
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`
- Create: `docs/iterations/iteration-07b-roadblock-commander.md`

- [ ] **Step 1: 补可见验收入口。** Boss 验收按钮进入完整 Boss 战；在该入口的事件标签明确显示“第一阶段：预警路障/扇形炮/哨位/巡逻”与“第二阶段：打断冲锋后输出”。保留既有 debug 伤害键，不引入与用户既有键位冲突的新快捷键。
- [ ] **Step 2: 编写验收文档。** 使用“操作 → 预期画面/数值/状态变化”，至少覆盖四项：Phase 1 路障不封死、50% 后砖墙通道与冲锋、钢墙打断/脆弱窗口、击败结果/Retry/Return；记录每一条自动化与可见验证证据。
- [ ] **Step 3: 执行完整自动化自检。**

  Run: `dotnet build GodotTank.sln -c Release`

  Expected: 0 errors，0 new warnings。

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite reward_catalog`

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite navigation_grid`

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase`

  Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_encounter`

  Expected: 每个套件全部 PASS。
- [ ] **Step 4: 执行 Godot 验证。**

  Run: `godot --headless --path game1 --editor --quit`

  Run: `godot --headless --path game1 --quit-after 180`

  Expected: 均以 0 退出，无场景解析或运行时异常。
- [ ] **Step 5: 策划可见自检并交付验收。** 主智能体按 `iteration-07b-roadblock-commander.md` 的四段路径手动运行游戏；仅在画面、数值与状态均可见且与规格一致时，向用户交付验收说明。用户明确验收前不得执行 commit/push。

## Plan Self-Review

- **规格覆盖：** Task 1 覆盖共享 A* 与砖墙；Task 2 覆盖路障、扇形炮、哨位、巡逻与威胁上限；Task 3 覆盖一次性 Phase 2 通道、预警冲锋、钢墙打断和脆弱；Task 4 覆盖击败清理、快照、Retry/Return；Task 5 覆盖回归和可见验收。
- **范围控制：** 全部任务复用现有敌军、炮弹、伤害和导航契约；没有扩展普通敌军池、协议或存档。
- **接口一致性：** 导航工厂对外只提供 `IEnemyPathProvider`，结果页只请求动作，Boss 阶段裁决仍只由 `BossPhaseController` 所有。
