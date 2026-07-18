# 迭代 06B：房间数据、TileMap 与动态导航 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将第二场战斗升级为不同布局的工业区房间，并让可破坏地形改变后敌军仍能可靠寻路至中继站。

**Architecture:** `RoomDefinition` 持有场景、波次与导航网格元数据，`RunController` 根据房间索引加载定义。地形视觉与碰撞迁移到三层 `TileMapLayer`；`NavigationGrid` 从可走格生成/更新 `AStarGrid2D`，敌军路径只向它查询下一格。

**Tech Stack:** Godot 4.7 .NET、C#、Godot TileSet/TileMapLayer、AStarGrid2D、NUnit。

> 2026-07-18 用户确认：所有真实 Godot 原生对象（包括 `AStarGrid2D`、TileMapLayer 与 TileSet）的行为测试由 `godot --headless` 测试宿主执行；NUnit 仅保留纯 C# 测试。原因是普通 NUnit 进程不初始化 Godot 运行时，调用原生 AStarGrid2D 会阻塞。

## Global Constraints

- 先完成并验收 06A；06B 不得重复实现协议、奖励或第二战状态机。
- 必须遵守 `AGENTS.md`、协同治理规格、06A 计划及 `2026-07-17-iteration-06-protocol-room-loop-design.md`。
- 必须有第二张明显不同的房间布局、三层 TileMapLayer、`RoomDefinition` 与破坏后导航更新。
- 砖块破坏后 0.25 秒内重建受影响导航；无路径时敌军不得卡死或抛异常，须安全等待并重试。
- 不扩大普通敌人池、Boss、协议内容；不改本局生命与重启规则。
- 未获用户可见验收前不得提交或推送。

---

## 文件结构与边界

| 路径 | 职责 |
| --- | --- |
| `game1/scripts/content/RoomDefinition.cs` | 房间资源：场景、波次、导航尺寸/格宽。 |
| `game1/scripts/navigation/NavigationGrid.cs` | 可走格、A* 重建、路径查询。 |
| `game1/scripts/terrain/TileTerrainAdapter.cs` | TileMap 砖块耐久和破坏事件。 |
| `game1/scenes/rooms/industrial_flank_room.tscn` | 第二张不同布局工业区房间。 |
| `game1/resources/rooms/*.tres` | 两个房间定义资源。 |

### Task 1: 定义房间资源与可更新导航网格

**Files:**
- Create: `game1/scripts/content/RoomDefinition.cs`
- Create: `game1/scripts/content/RoomWaveDefinition.cs`
- Create: `game1/scripts/navigation/NavigationGrid.cs`
- Create: `game1/tests/Game1.Tests/NavigationGridTests.cs`

**Interfaces:**

```csharp
public sealed partial class RoomDefinition : Resource {
    [Export] public PackedScene? Scene { get; set; }
    [Export] public Vector2I GridSize { get; set; }
    [Export] public int CellSize { get; set; }
    [Export] public RoomWaveDefinition[] Waves { get; set; } = Array.Empty<RoomWaveDefinition>();
}
public sealed class NavigationGrid {
    public void Rebuild(IReadOnlySet<Vector2I> blockedCells);
    public IReadOnlyList<Vector2I> FindPath(Vector2I from, Vector2I to);
}
```

- [ ] **Step 1: 写失败测试。**

```csharp
[Test]
public void Rebuild_RemovedBrick_OpensAPathOnNextQuery() {
    var grid = new NavigationGrid(new Vector2I(5, 3));
    grid.Rebuild(new HashSet<Vector2I> { new(2, 0), new(2, 1), new(2, 2) });
    Assert.That(grid.FindPath(new(0, 1), new(4, 1)), Is.Empty);
    grid.Rebuild(new HashSet<Vector2I> { new(2, 0), new(2, 2) });
    Assert.That(grid.FindPath(new(0, 1), new(4, 1)), Is.Not.Empty);
}
```

- [ ] **Step 2: 运行失败测试。**

Run: `dotnet test game1/tests/Game1.Tests/Game1.Tests.csproj --filter FullyQualifiedName~NavigationGridTests`

Expected: FAIL，因为房间和导航类型尚不存在。

- [ ] **Step 3: 实现 `NavigationGrid`。** 用 `AStarGrid2D` 建立 `Region = new Rect2I(Vector2I.Zero, GridSize)`、`DiagonalMode = Never`；每次 `Rebuild` 先 `Update()`，再对 blocked 格 `SetPointSolid(cell, true)`。`FindPath` 返回 `GetIdPath(from,to)`；起点、终点越界或无路径返回空数组而不抛异常。

- [ ] **Step 4: 完成资源校验。** `RoomDefinition` 验证 Scene 非空、格宽大于 0、GridSize 两轴大于 0、Waves 非空；无效定义抛出带房间资源路径的 `InvalidOperationException`。

- [ ] **Step 5: 运行测试。**

Run: `dotnet test game1/tests/Game1.Tests/Game1.Tests.csproj --filter FullyQualifiedName~NavigationGridTests`

Expected: PASS。

### Task 2: 迁移地形到 TileMapLayer 并提供破坏事件

**Files:**
- Create: `game1/scripts/terrain/TileTerrainAdapter.cs`
- Modify: `game1/scenes/rooms/mvp_combat_room.tscn`
- Create: `game1/scenes/rooms/industrial_flank_room.tscn`
- Create: `game1/resources/tiles/industrial_tileset.tres`
- Create: `game1/tests/Game1.Tests/TileTerrainAdapterTests.cs`

**Interfaces:**

```csharp
public sealed partial class TileTerrainAdapter : Node2D {
    public event Action<Vector2I>? BrickDestroyed;
    public bool DamageBrick(Vector2I cell, int damage);
    public IReadOnlySet<Vector2I> BlockedNavigationCells { get; }
}
```

- [ ] **Step 1: 写失败测试。**

```csharp
[Test]
public void DamageBrick_AtZeroHitPoints_RemovesBlockedCellAndRaisesOnce() {
    var terrain = TileTerrainFactory.Create(new Vector2I(3, 3), new Vector2I(1, 1), hitPoints: 2);
    var events = 0; terrain.BrickDestroyed += _ => events++;
    terrain.DamageBrick(new(1, 1), 2);
    terrain.DamageBrick(new(1, 1), 2);
    Assert.Multiple(() => {
        Assert.That(terrain.BlockedNavigationCells.Contains(new(1, 1)), Is.False);
        Assert.That(events, Is.EqualTo(1));
    });
}
```

- [ ] **Step 2: 实现并运行测试。** 每个砖格由字典保存耐久；耐久归零时清除 `Destructible` TileMapLayer 的 tile、同步碰撞层对应 tile、从 blocked 集移除并仅发一次 `BrickDestroyed`。运行 Task 1/2 测试，Expected: PASS。

- [ ] **Step 3: 迁移两张场景。** 每张房间必须包含命名为 `Ground`、`Structure`、`Destructible` 的三层 `TileMapLayer`；钢墙留在 Structure，砖墙留在 Destructible。删除原先逐个砖块 `StaticBody2D` 节点。第二张 `industrial_flank_room` 采用侧翼绕行、非对称掩体和不同出生边，不能只是镜像/换色。

- [ ] **Step 4: 编辑器验证。**

Run: `godot --headless --path game1 --editor --quit`

Expected: 两场景、TileSet 和脚本均无解析错误。

### Task 3: 让敌军按 A* 路径移动并随破坏更新

**Files:**
- Modify: `game1/scripts/enemies/EnemyTank.cs`
- Modify: `game1/scripts/enemies/EnemyDirector.cs`
- Modify: `game1/scripts/room/RoomController.cs`
- Modify: `game1/scripts/run/RunController.cs`
- Create: `game1/tests/Game1.Tests/EnemyNavigationTests.cs`

**Interfaces:**

```csharp
public interface IEnemyPathProvider {
    IReadOnlyList<Vector2> GetWorldPath(Vector2 fromWorld, Vector2 toWorld);
}
public void EnemyTank.SetPathProvider(IEnemyPathProvider pathProvider);
```

- [ ] **Step 1: 写失败测试。**

```csharp
[Test]
public void Enemy_WithNoPath_StopsAndRetriesInsteadOfThrowing() {
    var enemy = EnemyTankTestFactory.Create();
    enemy.SetPathProvider(new EmptyPathProvider());
    Assert.DoesNotThrow(() => enemy.TickNavigation(0.25f));
    Assert.That(enemy.Velocity, Is.EqualTo(Vector2.Zero));
}
```

- [ ] **Step 2: 实现路径消费。** 敌人每 0.25 秒或接到 `BrickDestroyed` 后请求路径；按路径点移动，距离终点允许误差为半格。空路径时速度为零、保留 0.25 秒重试计时，不调用旧 `DefenseRoutePlanner`。到中继站射程内仍沿用既有攻击逻辑。

- [ ] **Step 3: 绑定房间生命周期。** `RoomController` 在 Loading 建立 `NavigationGrid`，订阅 `TileTerrainAdapter.BrickDestroyed` 后调用 `Rebuild`；`EnemyDirector` 从当前房间取出生点、波次、路径提供器。`RunController` 以 `RoomIndex` 选择两个 RoomDefinition，第二战加载 `industrial_flank_room`。

- [ ] **Step 4: 运行相关测试。**

Run: `dotnet test game1/tests/Game1.Tests/Game1.Tests.csproj --filter "FullyQualifiedName~NavigationGridTests|FullyQualifiedName~TileTerrainAdapterTests|FullyQualifiedName~EnemyNavigationTests"`

Expected: PASS。

### Task 4: 完成不同房间可见验收与后置门禁

**Files:**
- Create: `game1/resources/rooms/mvp_combat_room.tres`
- Create: `game1/resources/rooms/industrial_flank_room.tres`
- Modify: `docs/decisions/0001-prototype-architecture-deferrals.md`
- Modify: `docs/iterations/iteration-06b-room-tilemap-navigation.md`
- Modify: `README.md`

- [ ] **Step 1: 写资源目录测试。** 断言有两个 RoomDefinition、两张不同场景、每个房间有非空波次、GridSize 与 CellSize 合法；断言房间二场景节点路径同时含三层 TileMapLayer。

- [ ] **Step 2: 完成可见验收。** 以固定种子清完第一房，在奖励页选择协议后确认：加载到明显不同布局；敌军能绕障向中继站推进；打掉关键砖块后 0.25 秒内改走新路径；人为封死路径时敌军停下重试、不报错、不穿墙。

- [ ] **Step 3: 完整验证。**

Run: `dotnet test game1/tests/Game1.Tests/Game1.Tests.csproj; dotnet build game1/game1.csproj -c Release; godot --headless --path game1 --editor --quit`

Expected: 全绿、0 warning/0 error、Godot 无解析错误。

- [ ] **Step 4: 更新决策记录。** 仅在上述验证及架构审查通过后，把决策 0001 中 TileMap、A*、RoomDefinition 三项从“延后”改为“已采用”，保留其余延后项不变。

## 06B 自审映射

- RoomDefinition 与不同房间选择：Task 1、3、4。
- 三层 TileMapLayer 与运行时砖块破坏：Task 2。
- A* 重建、敌军寻路与安全无路径回退：Task 1、3。
- 玩法和敌人内容不扩张：全局约束与 Task 4 审查。
