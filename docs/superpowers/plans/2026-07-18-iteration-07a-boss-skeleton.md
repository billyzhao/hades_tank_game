# 迭代 07A：路障指挥车 Boss 骨架 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立可运行、可验收的路障指挥车 Boss 房间、独立血条和不可逆两阶段状态机，不提前实现 07B 的攻击与结算内容。

**Architecture:** `BossDefinition` 是只读静态资源；`BossPhaseController` 是无 Godot 依赖的纯 C# 阶段裁决器；`RoadblockCommander` 以已有 `IDamageable` 接收伤害并向阶段控制器报告生命变化；`BossHudController` 订阅 Boss 信号显示独立血条。`AppRoot` 用一个可见的验收入口加载独立 BossRoom，不将不含普通波次的 Boss 房强行纳入 `RoomDefinition`，且不改变协议奖励、双生命线或普通房间循环。

**Tech Stack:** Godot 4.7 .NET、C#、Godot `Resource`/`PackedScene`/`Control`、现有 headless 测试宿主。

## Global Constraints

- 保持 Godot 4.7 .NET/C#、480×270 逻辑画布与 1440×810 默认窗口。
- 静态 Boss 配置 Resource 在运行时只读；运行时生命值不得写回资源。
- Boss 使用独立场景组合，不建立复杂继承树；阶段状态不直接操作 HUD。
- 07A 不得实现路障、召唤、冲锋、普通敌人扩充、胜利结算、新素材或新音频。
- 用户验收通过前，不得提交或推送；不得暂存 `.superpowers/`。

---

## File Structure

| 路径 | 职责 |
|---|---|
| `game1/scripts/bosses/BossPhase.cs` | 阶段枚举。 |
| `game1/scripts/bosses/BossPhaseController.cs` | 纯 C# 阶段判定与一次性事件。 |
| `game1/scripts/content/BossDefinition.cs` | Boss 静态资源与资源校验。 |
| `game1/scripts/bosses/RoadblockCommander.cs` | Boss Godot 实体、伤害契约和视觉预警。 |
| `game1/scripts/ui/BossHudController.cs` | 独立 Boss 血条显示与解绑。 |
| `game1/scenes/actors/roadblock_commander.tscn` | Boss 组合场景。 |
| `game1/scenes/rooms/mvp_boss_room.tscn` | Boss 验收房，沿用三层 TileMap。 |
| `game1/resources/bosses/roadblock_commander.tres` | BossDefinition 资源。 |
| `game1/tests/headless/ProtocolRuntimeTestHost.cs` | 阶段、资源和 Godot 运行时验证。 |
| `game1/scripts/app/AppRoot.cs` | 可见 Boss 验收入口与 HUD 生命周期。 |
| `docs/iterations/iteration-07a-boss-skeleton.md` | 07A 门禁、证据、验收脚本与偏离记录。 |

### Task 1: 纯阶段状态机与资源约束

**Files:**
- Create: `game1/scripts/bosses/BossPhase.cs`
- Create: `game1/scripts/bosses/BossPhaseController.cs`
- Create: `game1/scripts/content/BossDefinition.cs`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`

**Interfaces:**

```csharp
public enum BossPhase { PhaseOne, PhaseTwo, Defeated }
public sealed class BossPhaseController {
    public BossPhase CurrentPhase { get; }
    public event Action<BossPhase>? PhaseChanged;
    public event Action? Defeated;
    public BossPhase ReportHealth(int currentHealth, int maximumHealth);
}
public partial class BossDefinition : Resource {
    [Export] public PackedScene Scene { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public int MaximumHealth { get; set; }
    public void Validate();
}
```

- [ ] **Step 1: 写入失败测试**

在 `ProtocolRuntimeTestHost` 增加 `boss_phase` 套件，先断言：

```csharp
BossPhaseController controller = new();
int phaseEvents = 0;
int defeatedEvents = 0;
controller.PhaseChanged += _ => phaseEvents++;
controller.Defeated += () => defeatedEvents++;
Assert(controller.ReportHealth(100, 100) == BossPhase.PhaseOne, "满血必须为第一阶段。");
Assert(controller.ReportHealth(50, 100) == BossPhase.PhaseTwo, "生命值首次到 50% 必须进入第二阶段。");
controller.ReportHealth(40, 100);
controller.ReportHealth(0, 100);
controller.ReportHealth(0, 100);
Assert(phaseEvents == 1 && defeatedEvents == 1, "阶段与击败事件均只能触发一次。");
Assert(controller.ReportHealth(100, 100) == BossPhase.Defeated, "击败后状态不可逆。");
```

- [ ] **Step 2: 运行失败测试**

Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase`

Expected: FAIL，因为阶段类型不存在。

- [ ] **Step 3: 实现最小状态机与资源校验**

`ReportHealth` 对 `maximumHealth <= 0` 抛 `ArgumentOutOfRangeException`；`currentHealth <= 0` 只首次进入 `Defeated`；其余输入以 `currentHealth * 2 <= maximumHealth` 判定第二阶段。`BossDefinition.Validate()` 拒绝空场景、空名称或非正最大生命。

- [ ] **Step 4: 运行通过测试**

Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase`

Expected: 所有 boss_phase 断言 PASS。

### Task 2: Boss 实体、房间和独立 HUD

**Files:**
- Create: `game1/scripts/bosses/RoadblockCommander.cs`
- Create: `game1/scripts/ui/BossHudController.cs`
- Create: `game1/scenes/actors/roadblock_commander.tscn`
- Create: `game1/scenes/rooms/mvp_boss_room.tscn`
- Create: `game1/resources/bosses/roadblock_commander.tres`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`

**Interfaces:**

```csharp
public partial class RoadblockCommander : CharacterBody2D, IDamageable {
    [Signal] public delegate void HealthChangedEventHandler(int current, int maximum);
    [Signal] public delegate void PhaseChangedEventHandler(int phase);
    [Signal] public delegate void DefeatedEventHandler();
    public void Initialize(BossDefinition definition);
    public DamageResult ApplyDamage(DamageContext context);
}
public partial class BossHudController : Control {
    public void Bind(RoadblockCommander boss, BossDefinition definition);
    public void Unbind();
}
```

- [ ] **Step 1: 写入资源/场景失败测试**

在 `boss_phase` 套件加载 `res://resources/bosses/roadblock_commander.tres`，调用 `Validate()`，实例化场景，并断言 `MvpBossRoom` 包含 `Ground`、`Structure`、`Destructible` 三个 `TileMapLayer` 与 `RoadblockCommander`；断言 Boss 资源名为“路障指挥车”、生命值为 300。

- [ ] **Step 2: 运行失败测试**

Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase`

Expected: FAIL，因为 Boss 资源与场景尚不存在。

- [ ] **Step 3: 实现场景与行为**

Boss 使用敌人碰撞层、矩形碰撞体、当前像素原型 Sprite/Polygon 组合。`Initialize` 创建运行时 `currentHealth` 和 `BossPhaseController`，并订阅阶段事件。受击时忽略负伤害，最大只扣至 0；阶段二切换时播放一次 0.25 秒 Modulate 闪烁；击败时停止移动和碰撞、只发送一次 `Defeated`。

Boss 房保留既有中继站、玩家、RebootController、三层 TileMap 与相机；不创建 EnemyDirector、路障或 Boss 攻击节点。HUD 使用 `ProgressBar`、名称 Label、阶段 Label；第一阶段为黄橙色，第二阶段为红色。

- [ ] **Step 4: 运行通过测试与编辑器解析**

Run:

```powershell
dotnet build game1/Game1.csproj -c Release --no-restore
godot --headless --path game1 --editor --quit
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase
```

Expected: Release 0 warning/0 error，编辑器无解析错误，boss_phase 全绿。

### Task 3: 可见验收入口、生命周期和回归

**Files:**
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/tests/headless/ProtocolRuntimeTestHost.cs`
- Create: `docs/iterations/iteration-07a-boss-skeleton.md`
- Modify: `README.md`
- Modify: `game1/README.md`

**Interfaces:**

```csharp
private void EnterBossValidationRoom();
private void BindBossHud(Node2D room);
private void ClearBossHud();
```

- [ ] **Step 1: 写入生命周期失败测试**

在 headless 宿主实例化 Boss，订阅 `HealthChanged`、`PhaseChanged` 与 `Defeated`，依次给予 150、1、149 点伤害，断言生命序列为 150、149、0，阶段二只出现一次，击败只出现一次；再额外伤害一次，断言事件计数不变。

- [ ] **Step 2: 运行失败测试**

Run: `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase`

Expected: FAIL，因为运行时 Boss 尚未接入。

- [ ] **Step 3: 实现可见入口与解绑**

在现有 HUD 添加可点击“Boss 验收”按钮；点击后清理当前 RoomHost 子节点、隐藏奖励面板、实例化 Boss 房间，并调用 Boss `Initialize`。进入时显示独立 Boss HUD；重载普通战斗房或 Boss 击败后均调用 `Unbind`，防止旧 Boss 事件写入新 HUD。Boss 击败仅显示“路障指挥车已击败（07B 将接入结算）”，不改变 `RunState` 或跳转结算。

- [ ] **Step 4: 完整验证**

Run:

```powershell
dotnet build game1/Game1.csproj -c Release --no-restore
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite reward_catalog
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite navigation_grid
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase
godot --headless --path game1 --editor --quit
godot --headless --path game1 --quit-after 180
```

Expected: 既有 06A/06B 测试与 boss_phase 均通过；构建、编辑器和启动无错误。

- [ ] **Step 5: 更新执行记录并交付验收**

记录架构合规矩阵、测试命令、无实质偏离结论与以下用户验收脚本：点击 Boss 验收 → 观察独立血条 → 打至 50% → 观察一次阶段二提示 → 打至 0 → 观察一次击败提示。用户验收前不执行 `git add`、`git commit` 或 `git push`。

## Self-Review

- 规格覆盖：Task 1 覆盖不可逆两阶段与资源只读校验；Task 2 覆盖 Boss 场景、独立 HUD 和三层 TileMap；Task 3 覆盖可见入口、解绑、回归与验收。
- 范围检查：所有 07B 攻击、路障、召唤、冲锋与结算功能均被明确排除。
- 类型一致性：阶段裁决只通过 `BossPhaseController.ReportHealth`；实体和 HUD 通过信号通信，未引入对 `RunState` 的 Boss 特例写入。
