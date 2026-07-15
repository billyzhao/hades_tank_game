# Roguelite Tank MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在现有 `game1` Godot 4.7 C# 工程中，按可运行迭代完成一个包含双摇杆坦克战斗、可破坏地形、中继站双生命线、三类敌人、三选一构筑与路障指挥车 Boss 的最小可玩版本。

**Architecture:** 使用场景组合构建玩家、敌人、房间与 Boss；静态内容使用只读 C# `Resource`，单局状态使用普通 C# 对象。弹道使用物理帧扫掠查询，敌人导航使用 `AStarGrid2D`，房间和构筑状态限定在当前单局作用域。

**Tech Stack:** Godot 4.7 .NET、C# / .NET 8、Godot 2D Compatibility renderer、NUnit 4.6.1、NUnit3TestAdapter 6.2.0、Microsoft.NET.Test.Sdk 18.7.0。

## Global Constraints

- 逻辑画布固定 480×270，默认窗口 960×540，像素纹理关闭过滤与 mipmap。
- 移动、碰撞和弹道只在 `_PhysicsProcess` 更新，目标稳定 60 FPS。
- `Resource` 运行时只读；中继站耐久、装甲、重启次数与协议栈存入 `RunState`。
- MVP 固定一个城区战斗房、巡逻坦克/突击车/攻城炮车、8–12 个协议和一个路障指挥车 Boss。
- 不实现在线多人、完整程序生成、后续区域、复杂局外成长或战斗中途存档。
- 未经用户确认不购买或下载素材；付费素材不可用时使用程序占位图或已批准的 CC0 资源。
- AI 只补现成资源缺失的 Boss、关键特效、UI 或宣传素材，采用前必须完成人工像素清理和授权登记。
- 每个任务结束必须通过单元测试、构建、Godot headless 启动和一次人工 OpenGL 游玩。
- 当前根目录不是 Git 仓库；执行 Task 1 时初始化本地 Git，但不配置远程仓库。

## Approved References

- 策划：`docs/superpowers/specs/2026-07-15-roguelite-tank-design.md`
- 技术设计：`docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md`
- 素材流程：`asset_sources/README.md`
- 素材清单：`asset_sources/THIRD_PARTY_ASSETS.md`

## Iteration Overview

| 任务 | 可运行成果 | 进入下一阶段的门槛 |
|---|---|---|
| 1 | 新应用入口、测试基础和单局状态 | 构建/测试/启动全部通过 |
| 2 | 玩家移动、独立瞄准和冲刺 | 键鼠与手柄操作稳定，车体不能穿墙 |
| 3 | 主炮、扫掠命中和钢墙反弹 | 高速炮弹不穿墙，反弹可预测 |
| 4 | 砖墙、中继站、装甲和战场重启 | 双生命线边界条件正确 |
| 5 | 三类敌人、导航、波次和房间状态 | 一场守点战可以稳定完成 |
| 6 | 三选一协议和第二场战斗 | 至少两组联动明显改变操作 |
| 7 | 路障指挥车 Boss | 两阶段战斗和胜利结算完整 |
| 8 | 素材、音频、存档、性能和 MVP 验收 | 完整短局可反复游玩并保持 60 FPS |

---

### Task 1: 工程基线、测试项目与应用入口

**Iteration outcome:** 启动进入新的 `AppRoot` 空战斗壳；纯 C# 测试可运行；旧演示不再是入口。

**Files:**
- Create: `.gitignore`
- Create: `GodotTank.sln`
- Create: `tests/Game1.Tests/Game1.Tests.csproj`
- Create: `tests/Game1.Tests/Run/RunStateTests.cs`
- Create: `game1/scripts/run/RunState.cs`
- Create: `game1/scripts/app/AppRoot.cs`
- Create: `game1/scenes/app/main.tscn`
- Modify: `game1/project.godot`
- Preserve until the new entry passes smoke tests: `game1/scenes/main.tscn`, `game1/scripts/Main.cs`, `game1/scripts/GameRules.cs`

**Interfaces:**
- Produces: `RunState.CreateNew(int seed, int relayIntegrity = 100, int armor = 100, int reboots = 1)`
- Produces: `AppRoot.CurrentRun : RunState`

- [ ] **Step 1: Initialize version control and solution**

```powershell
git init
dotnet new sln -n GodotTank
dotnet sln GodotTank.sln add game1/Game1.csproj
```

Expected: local repository and solution exist; no remote is configured.

- [ ] **Step 2: Create the NUnit project**

Use `net8.0` and exact package versions from the plan header. Add a project reference to `../../game1/Game1.csproj`, then add the test project to `GodotTank.sln`.

- [ ] **Step 3: Write the failing run-state test**

```csharp
[Test]
public void CreateNew_UsesApprovedDefaults()
{
    RunState state = RunState.CreateNew(seed: 42);

    Assert.Multiple(() =>
    {
        Assert.That(state.Seed, Is.EqualTo(42));
        Assert.That(state.RelayIntegrity, Is.EqualTo(100));
        Assert.That(state.PlayerArmor, Is.EqualTo(100));
        Assert.That(state.RebootsRemaining, Is.EqualTo(1));
        Assert.That(state.RoomIndex, Is.Zero);
    });
}
```

Run: `dotnet test tests/Game1.Tests/Game1.Tests.csproj --filter CreateNew_UsesApprovedDefaults`

Expected: FAIL because `RunState` does not exist.

- [ ] **Step 4: Implement minimal `RunState`**

Use required seed plus mutable relay integrity, player armor, reboot count and room index. `CreateNew` must set the tested values exactly and must not reference Godot nodes.

- [ ] **Step 5: Create the app scene**

Exact scene tree:

```text
AppRoot (Node)
├── RoomHost (Node)
└── UI (CanvasLayer)
    └── StatusLabel (Label, text="MVP FOUNDATION")
```

Set `run/main_scene` to this scene and set 480×270 viewport, 960×540 override, `canvas_items` stretch and `keep` aspect.

- [ ] **Step 6: Verify and commit**

```powershell
dotnet test GodotTank.sln
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --editor --quit
godot --headless --path game1 --quit-after 5
```

Expected: all commands exit 0.

Commit: `chore: establish roguelite tank project baseline`.

---

### Task 2: 玩家移动、独立瞄准与冲刺

**Iteration outcome:** 玩家在灰盒房间内用 WASD/左摇杆移动、鼠标/右摇杆瞄准，并可冲刺；车体和炮塔朝向独立。

**Files:**
- Create: `game1/scripts/player/TankMotion.cs`
- Create: `game1/scripts/player/PlayerTank.cs`
- Create: `game1/scripts/player/DashComponent.cs`
- Create: `game1/scenes/actors/player_tank.tscn`
- Create: `game1/scenes/rooms/mvp_combat_room.tscn`
- Create: `tests/Game1.Tests/Player/TankMotionTests.cs`
- Modify: `game1/project.godot`
- Modify: `game1/scripts/app/AppRoot.cs`

**Interfaces:**
- Produces: `TankMotion.CalculateVelocity(Vector2 input, float speed) : Vector2`
- Produces: `PlayerTank.AimDirection : Vector2`
- Produces events: `DashComponent.DashStarted`, `DashComponent.DashEnded`

- [ ] **Step 1: Test diagonal normalization**

```csharp
[Test]
public void CalculateVelocity_NormalizesDiagonalInput()
{
    Vector2 velocity = TankMotion.CalculateVelocity(Vector2.One, 120f);
    Assert.That(velocity.Length(), Is.EqualTo(120f).Within(0.001f));
}
```

Expected before implementation: FAIL.

- [ ] **Step 2: Implement motion math**

```csharp
public static Vector2 CalculateVelocity(Vector2 input, float speed)
{
    return input.IsZeroApprox() ? Vector2.Zero : input.Normalized() * speed;
}
```

Expected after implementation: PASS.

- [ ] **Step 3: Build the player scene**

```text
PlayerTank (CharacterBody2D)
├── BodyVisual (Polygon2D, blue programmatic stand-in)
├── BodyCollision (CollisionShape2D, 18×18)
├── Turret (Node2D)
│   ├── TurretVisual (Polygon2D)
│   └── Muzzle (Marker2D, 14,0)
└── DashComponent (Node)
```

`PlayerTank._PhysicsProcess` reads movement input, assigns `Velocity`, calls `MoveAndSlide()`, rotates chassis toward non-zero movement and rotates turret toward mouse world position or right-stick direction. Physics position must not be rounded.

- [ ] **Step 4: Add input actions and dash**

Add `aim_left/right/up/down`, `fire_primary`, `dash`, `active_ability`, `interact` and `pause`. Dash defaults: 3× speed, 0.14 s duration, 0.8 s cooldown; reject zero-direction dash and never teleport through collision.

- [ ] **Step 5: Verify**

Acceptance: cardinal/diagonal speed equal; mouse aim does not rotate chassis; right stick holds last valid aim; dash cannot cross room boundary; 30 seconds continuous movement has no errors.

Commit: `feat: add independent tank movement and aiming`.

---

### Task 3: 主炮、扫掠碰撞与钢墙反弹

**Iteration outcome:** 玩家可射击，高速炮弹稳定命中墙体，钢墙产生可预测反弹。

**Files:**
- Create: `game1/scripts/content/WeaponDefinition.cs`
- Create: `game1/scripts/combat/Team.cs`
- Create: `game1/scripts/combat/ProjectileSpec.cs`
- Create: `game1/scripts/combat/ProjectileMath.cs`
- Create: `game1/scripts/combat/Projectile.cs`
- Create: `game1/scripts/combat/WeaponController.cs`
- Create: `game1/scenes/combat/projectile.tscn`
- Create: `tests/Game1.Tests/Combat/ProjectileMathTests.cs`
- Modify: player and room scenes

**Interfaces:**
- Produces: `ProjectileMath.Reflect(Vector2 direction, Vector2 normal) : Vector2`
- Produces: `WeaponController.TryFire(Vector2 origin, Vector2 direction, Team team) : bool`
- Produces: `Projectile.Initialize(ProjectileSpec spec, Team team, Vector2 direction)`

- [ ] **Step 1: Test reflection**

Test a horizontal wall and a 45-degree hit. Expected before implementation: FAIL.

- [ ] **Step 2: Implement reflection**

```csharp
public static Vector2 Reflect(Vector2 direction, Vector2 normal)
{
    return (direction - 2f * direction.Dot(normal) * normal).Normalized();
}
```

- [ ] **Step 3: Implement swept projectile movement**

Each physics frame casts from current to proposed position, handles the closest hit, consumes remaining frame distance after reflection, offsets 0.05 pixel along the normal, and stops after four impacts in one frame. Steel consumes one bounce; an exhausted projectile is queued for deletion.

- [ ] **Step 4: Add greybox steel and weapon data**

Use collision layer `world_steel`. Default weapon: damage 10, speed 360 px/s, cooldown 0.22 s, lifetime 2.5 s, one bounce.

- [ ] **Step 5: Verify**

Acceptance: no thin-wall tunneling; symmetric 45-degree reflection; projectile disappears after final bounce; fire cooldown enforced; 160 debug projectiles do not crash.

Commit: `feat: add swept projectiles and steel ricochet`.

---

### Task 4: 伤害、砖墙、中继站与战场重启

**Iteration outcome:** 砖墙可破坏；玩家和中继站拥有跨房间生命资源；坦克报废可消耗一次重启。

**Files:**
- Create: `game1/scripts/combat/IDamageable.cs`
- Create: `game1/scripts/combat/DamageContext.cs`
- Create: `game1/scripts/combat/DamageResult.cs`
- Create: `game1/scripts/combat/HealthComponent.cs`
- Create: `game1/scripts/combat/DestructibleTerrain.cs`
- Create: `game1/scripts/combat/RelayStation.cs`
- Create: `game1/scripts/combat/RebootController.cs`
- Create: `game1/scenes/combat/relay_station.tscn`
- Create: `tests/Game1.Tests/Run/RunFailureTests.cs`
- Create: `tests/Game1.Tests/Combat/DamageTests.cs`
- Modify: `RunState` and MVP room

**Interfaces:**
- Produces: `IDamageable.ApplyDamage(DamageContext context) : DamageResult`
- Produces: `RunState.ApplyRelayDamage(int amount) : bool`
- Produces: `RunState.TryConsumeReboot() : bool`
- Produces events: `HealthComponent.ValueChanged`, `HealthComponent.Depleted`

- [ ] **Step 1: Test both failure paths**

Test relay integrity reaches exactly zero, one reboot succeeds, a second reboot fails, and counts never become negative.

- [ ] **Step 2: Implement `RunState` transitions**

Clamp relay damage to non-negative and relay integrity to zero. `TryConsumeReboot` returns false without mutation when no reboot remains.

- [ ] **Step 3: Implement damage contract**

Use immutable record structs. Shield applies before armor; negative damage cannot heal; depletion emits exactly once even if more hits arrive after zero.

- [ ] **Step 4: Implement terrain and relay**

Use `TileMapLayer` for ground, brick and steel. `DestructibleTerrain` owns `Dictionary<Vector2I,int>` current HP, erases the cell at zero and never modifies TileSet resources. Relay initializes from and writes back to `RunState`.

- [ ] **Step 5: Implement reboot flow**

On player depletion: disable input/collision, consume reboot, wait 1.2 s, clear hostile projectiles in 48 pixels, move to relay safe marker, restore 50% armor and grant 1.0 s invulnerability. With no reboot, fail the run.

- [ ] **Step 6: Verify**

Acceptance: brick breaks, steel does not; relay zero fails immediately; first player death reboots; second death fails; no duplicate failure event.

Commit: `feat: add destructible defense and reboot lifecycle`.

---

### Task 5: 三类敌人、导航、波次与房间状态

**Iteration outcome:** 巡逻坦克、突击车和攻城炮车组成一场稳定可完成的守点战斗。

**Files:**
- Create: `game1/scripts/content/EnemyDefinition.cs`
- Create: `game1/scripts/content/RoomDefinition.cs`
- Create: `game1/scripts/enemies/EnemyTank.cs`
- Create: `game1/scripts/enemies/EnemyBrain.cs`
- Create: `game1/scripts/enemies/TargetPolicy.cs`
- Create: `game1/scripts/enemies/IEnemyBehavior.cs`
- Create: `game1/scripts/enemies/PatrolShooterBehavior.cs`
- Create: `game1/scripts/enemies/AssaultBehavior.cs`
- Create: `game1/scripts/enemies/SiegeBehavior.cs`
- Create: `game1/scripts/combat/NavigationGrid.cs`
- Create: `game1/scripts/combat/EnemyDirector.cs`
- Create: `game1/scripts/combat/RoomController.cs`
- Create: `game1/scenes/actors/enemy_tank.tscn`
- Create: `game1/data/enemies/*.tres`
- Create: target-policy and wave-budget tests

**Interfaces:**
- Produces: `TargetPolicy.SelectTarget(BehaviorId behavior, TargetSnapshot targets) : TargetId`
- Produces: `EnemyDirector.StartEncounter(RoomDefinition definition)`
- Produces events: `EnemyTank.Destroyed`, `EnemyDirector.AllWavesFinished`, `RoomController.RoomCleared`, `RoomController.RoomFailed`

- [ ] **Step 1: Test target policy**

Patrol and assault prefer player; siege prefers relay; all fall back safely when their preferred target is unavailable.

- [ ] **Step 2: Implement common AI state flow**

Every behavior uses `AcquireTarget → Move → Telegraph → Attack → Recover`. Telegraph durations: patrol 0.35 s, assault 0.2 s, siege 0.75 s. Spawn warning lasts 0.6 s before collision and attacks enable.

- [ ] **Step 3: Implement `AStarGrid2D` navigation**

Build from blocking tiles, update only changed cells, recalculate each enemy no more than four times per second with staggered offsets, and use direct steering fallback if no path exists.

- [ ] **Step 4: Implement threat waves**

Threat costs: patrol 1, assault 2, siege 3. Room budgets: 4, 6, 8. No more than one siege unit alive; reject unsafe spawn points around player and relay.

- [ ] **Step 5: Implement room lifecycle**

`Loading → Intro → Combat → Cleared → Reward → Exiting` with `Failed` from Combat. Clear hostile projectiles before reward. Clear only after last wave and all enemies are gone.

- [ ] **Step 6: Verify**

Acceptance: roles are readable with programmatic stand-ins; every attack is telegraphed; siege creates a priority decision; waves finish without stuck enemies.

Commit: `feat: add enemy roles and room encounter lifecycle`.

---

### Task 6: 协议构筑、三选一与第二场战斗

**Iteration outcome:** 战斗结束进入三选一，选择改变下一场战斗，并可连续完成两场。

**Files:**
- Create: `game1/scripts/content/ProtocolDefinition.cs`
- Create: `game1/scripts/content/ProtocolEffectDefinition.cs`
- Create: `game1/scripts/content/ContentCatalog.cs`
- Create: `game1/scripts/run/StatPipeline.cs`
- Create: `game1/scripts/run/StatId.cs`
- Create: `game1/scripts/run/BuildController.cs`
- Create: `game1/scripts/run/RewardGenerator.cs`
- Create: `game1/scripts/run/RunController.cs`
- Create: `game1/scripts/ui/RewardScreen.cs`
- Create: `game1/scenes/ui/reward_screen.tscn`
- Create: `game1/data/content_catalog.tres` and protocol resources
- Create: stat and deterministic reward tests

**Interfaces:**
- Produces: `StatPipeline.Evaluate(StatId id, float baseValue) : float`
- Produces: `RewardGenerator.GenerateThree(RunState state, IReadOnlyList<ProtocolDefinition> pool) : IReadOnlyList<ProtocolDefinition>`
- Produces: `BuildController.AddProtocol(StringName id)`
- Produces events: `RewardScreen.ProtocolChosen`, `RunController.RoomChanged`

- [ ] **Step 1: Test stat ordering**

Base 10, fixed +2, additive +50%, multiplicative ×2 must equal 36 before final clamp.

- [ ] **Step 2: Test deterministic rewards**

Same seed and history produce the same three unique IDs. Exclude conflicts and maxed items. Preserve one universal candidate so synergy weighting does not decide for the player.

- [ ] **Step 3: Implement runtime build scope**

Definitions remain read-only. Runtime effects subscribe only to shot, hit, kill, dash, player damage, relay damage and room-clear hooks, then unsubscribe when the run ends.

`ContentCatalog` must validate unique IDs, non-null scene/resource references, legal numeric ranges and satisfiable protocol requirements. Invalid debug content stops entry into combat with a precise error; release mode returns to the app shell.

- [ ] **Step 4: Author the first protocol set**

Required: ricochet +1; ricochet damage +30%; split after first hit; electric dash trail; dash cooldown -20%; room-clear player repair; room-clear relay repair; relay projectile shield; fire-rate tradeoff; heavy-shell tradeoff. Ricochet pair and dash pair are mandatory synergies.

- [ ] **Step 5: Connect battle → reward → second battle**

Pause combat for reward, show exactly three choices, record one, safely free old room, instance next room and restore persistent run state. Reuse the same room with a derived seed.

- [ ] **Step 6: Verify**

Acceptance: deterministic choices, no duplicates, two synergies visibly alter play, repairs persist, two battles complete without stale signals.

Commit: `feat: add deterministic protocol reward loop`.

---

### Task 7: 路障指挥车 Boss 与短局胜利

**Iteration outcome:** 完成两阶段 Boss 战并进入本局胜利结算。

**Files:**
- Create: `game1/scripts/bosses/RoadblockCommander.cs`
- Create: `game1/scripts/bosses/BossPhaseController.cs`
- Create: `game1/scripts/bosses/BarrierDeployment.cs`
- Create: `game1/scenes/actors/roadblock_commander.tscn`
- Create: `game1/scenes/rooms/mvp_boss_room.tscn`
- Create: `game1/scripts/ui/RunResultScreen.cs`
- Create: `game1/scenes/ui/run_result_screen.tscn`
- Create: `tests/Game1.Tests/Bosses/BossPhaseTests.cs`

**Interfaces:**
- Produces: `BossPhaseController.ReportHealth(float current, float maximum) : BossPhase`
- Produces events: `RoadblockCommander.PhaseChanged`, `RoadblockCommander.Defeated`
- Consumes: terrain placement, enemy summon and run-result APIs

- [ ] **Step 1: Test phases**

Above 50% is PhaseOne; at/below 50% enters PhaseTwo exactly once; zero enters Defeated and cannot return.

- [ ] **Step 2: Implement Phase One**

Move between authored anchors, preview barriers for 0.8 s, deploy only to validated empty cells, summon patrols under threat cap and fire a readable three-shot fan.

- [ ] **Step 3: Implement Phase Two**

Stop deployment, destroy selected brick cells to open a corridor, telegraph a line to relay, then charge. Steel collision interrupts and opens 1.5 s vulnerability; charge has an authored cooldown.

- [ ] **Step 4: Connect victory**

Boss defeat clears hostile actors/projectiles and opens result UI showing seed, protocols, relay integrity and elapsed time. Retry creates new `RunState`; return exits to app shell.

- [ ] **Step 5: Verify**

Acceptance: phase changes once; barriers never overlap player/relay; corridor remains navigable; defensive and offensive builds can both win; result can be reached repeatedly.

Commit: `feat: add roadblock commander boss encounter`.

---

### Task 8: 素材、音频、存档、性能与 MVP 验收

**Iteration outcome:** 灰盒替换为统一原型表现，设置/解锁可保存，性能满足预算，完整短局通过验收。

**Files:**
- Modify: `asset_sources/THIRD_PARTY_ASSETS.md`
- Populate selectively: `game1/assets/**`
- Create: `game1/scripts/app/SaveData.cs`
- Create: `game1/scripts/app/SaveService.cs`
- Create: `game1/scripts/app/AudioService.cs`
- Create: `game1/scripts/ui/CombatHud.cs`
- Create: `game1/scenes/ui/combat_hud.tscn`
- Create: `game1/scripts/ui/DebugOverlay.cs`
- Create: `game1/scenes/ui/debug_overlay.tscn`
- Create: `game1/tests/integration/mvp_test_runner.tscn`
- Create: `game1/tests/integration/MvpTestRunner.cs`
- Create: `tests/Game1.Tests/App/SaveDataTests.cs`
- Modify: `game1/project.godot`

**Interfaces:**
- Produces: `SaveService.LoadOrDefault() : SaveData`
- Produces: `SaveService.SaveAtomic(SaveData data)`
- Produces: `AudioService.PlaySfx(StringName id, Vector2? worldPosition = null)`

- [ ] **Step 1: Apply the asset approval gate**

Present exact P0/P1 links before download. If paid packs are not approved, use Kenney CC0 or programmatic stand-ins. Save license/receipt, update version/hash, and copy only processed selected files into `game1/assets`.

- [ ] **Step 2: Normalize visuals**

Apply one palette, pixel density and shadow direction. Replace player, three enemies, relay, brick/steel, projectile, explosions, protocol icons and Boss in that order. Verify silhouettes in grayscale at logical resolution.

- [ ] **Step 3: Add audio**

Create `Master`, `Music`, `SFX` and `UI` buses. Minimum content: three cannon variants, three impacts, two explosions, relay warning, reboot, selection, clear, Boss transition, one ambience and one combat loop.

- [ ] **Step 4: Test and implement atomic save**

Test schema version, defaults, JSON round-trip and corrupted-file fallback. Write `user://save.tmp`, validate, replace `save.json`, preserve `.broken` on failure. Save settings, unlock IDs and last-run summary only.

- [ ] **Step 5: Add HUD and debug overlay**

HUD shows player armor, relay integrity, reboot count, dash cooldown and protocol icons. Debug overlay shows FPS, enemies, projectiles, seed, room state and navigation toggle; release builds disable debug inputs.

Add pause handling: focus loss pauses automatically; pause UI continues processing while gameplay stops; resuming requires an explicit player input.

- [ ] **Step 6: Add headless integration runner**

It exits non-zero unless all checks pass: steel stops dash; projectile reflects; brick breaks; siege targets relay; room reaches Reward; reboot continues once; second death fails; relay zero fails; Boss victory reaches result.

Run:

```powershell
godot --headless --path game1 --scene res://tests/integration/mvp_test_runner.tscn
```

Expected: exit 0 with every integration check PASS.

- [ ] **Step 7: Stress and final acceptance**

Stress with 30 enemies, 160 projectiles and 40 hazard areas. Profile before pooling. Complete battle → reward → battle → Boss → result; separately test relay failure and reboot-exhausted failure; restart five times without stale events or save corruption.

- [ ] **Step 8: Verify MVP candidate**

```powershell
dotnet test GodotTank.sln
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --editor --quit
godot --headless --path game1 --scene res://tests/integration/mvp_test_runner.tscn
```

Expected: all commands exit 0, no new warnings, complete short run remains playable at target frame rate.

Commit: `feat: complete roguelite tank MVP vertical slice`.

## Review Checkpoint After Every Task

Stop and report automated results, manual acceptance, files changed, observed gameplay/balance risk and whether the next task's assumptions remain valid. Do not begin the next task while the current playable gate fails; fix it or revise this plan with user approval.
