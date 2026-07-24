# BC-02 Combat, Enemy, and Build Depth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. 本项目执行单主智能体模式，不启用子智能体。

**Goal:** 让封锁城区的三核心各自支持三条可识别构筑路线，让四类敌军、第五波精英和路障指挥车通过职责组合形成压力，而不是只增加生命。

**Architecture:** 保留现有 `BuildController`、`RewardGenerator`、`WaveDirector`、`EnemyTank`、`ArenaController` 和 Boss 组合架构。构筑路线只通过标签、权重和统一属性管线形成软引导；敌军与精英数值迁移到 Godot `Resource`，AI 将“职责决策”和“路径移动”分开；Boss 阶段参数继续由 `BossDefinition` 提供。

**Tech Stack:** Godot 4.7 .NET、C#、`.tres` Resource、现有 Godot headless 测试宿主、NUnit。

## Global Constraints

- 当前只开发“封锁城区”，不得新增第二竞技场、第二套战斗框架或中继站语义。
- 核心只固定操作节奏，同一核心必须有三条软引导路线，不得锁部门或锁候选池。
- 五波时长保持 `45/50/55/60/70` 秒；第 5 波只有一个精英规则。
- 难度来自侦察、追击、突击、远程和精英节奏的组合，不通过普通敌军生命统一膨胀。
- 所有生产数值通过内容目录或既有定义读取；运行时不得改写共享 Resource。
- 同一功能块连续开发，全部内容完成后才执行集中全量自检。
- 用户验收通过前不得提交或推送；`.superpowers/` 和未确认素材目录不得进入仓库。

---

### Task 1: 三核心九路线与受控随机

**Files:**
- Create: `game1/scripts/run/BuildRouteDefinition.cs`
- Create: `game1/scripts/run/BuildRouteCatalog.cs`
- Create: `game1/scripts/run/BuildRouteAnalyzer.cs`
- Modify: `game1/scripts/run/CoreCatalog.cs`
- Modify: `game1/scripts/run/RewardGenerator.cs`
- Modify: `game1/scripts/content/AuxiliaryDefinition.cs`
- Modify: `game1/resources/protocols/*.tres`
- Modify: `game1/resources/auxiliaries/*.tres`
- Modify: `game1/scripts/app/AppRoot.cs`
- Modify: `game1/scenes/app/main.tscn`
- Test: `game1/tests/headless/CoreBuildTestHost.cs`
- Test: `game1/tests/headless/ProtocolRuntimeTestHost.cs`
- Test: `game1/tests/headless/HudLayoutTestHost.cs`

**Interfaces:**
- Produces: `BuildRouteCatalog.GetRoutes(CoreId)`、`BuildRouteAnalyzer.Analyze(...)`。
- Consumes: `CoreDefinition.BuildTags`、`ProtocolDefinition.Tags`、`AuxiliaryDefinition.BuildTags`。

- [ ] **Step 1: 写入路线合同红灯**

在 `CoreBuildTestHost` 验证每个核心恰有三个唯一标签和三条路线；每条路线至少有两个普通协议、一个稀有协议或辅助支撑。在 `ProtocolRuntimeTestHost` 验证同种子确定性不变，核心匹配标签提高权重但不排除非匹配候选。

- [ ] **Step 2: 运行红灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/core_build_test_host.tscn
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite reward_catalog
```

Expected: 路线类型、辅助标签和标签权重尚不存在，编译或断言失败。

- [ ] **Step 3: 实现路线目录和分析器**

九条固定路线：

```text
突破重炮：ricochet 跳弹火网 / penetration 破甲直击 / impact 分裂重击
过载速射：rapid_fire 高频弹幕 / on_hit 命中触发 / auxiliary 自动武装
电驱游骑：dash 冲刺电轨 / mobility 环场机动 / area 近身压制
```

`Analyze` 按已选协议和辅助的标签计分，只返回当前核心的路线；零分返回“待成型”，并列按目录稳定顺序选择。

- [ ] **Step 4: 接入受控随机和可见反馈**

`RewardGenerator.GetWeight` 使用核心三标签 `×1.4`，已选路线同标签 `×1.25`；两个倍率只软加权，不过滤候选。`BuildLabel` 在战斗中显示“构筑路线：<路线> | <协议/辅助摘要>”，保持高度不超过 11px。

- [ ] **Step 5: 运行定向绿灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/core_build_test_host.tscn
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite reward_catalog
godot --headless --path game1 --scene res://tests/headless/hud_layout_test_host.tscn
```

Expected: 三套路线合同、确定性和 HUD 可见性全部 PASS。

---

### Task 2: 四敌军数据资源与职责移动

**Files:**
- Create: `game1/scripts/content/EnemyDefinition.cs`
- Create: `game1/scripts/enemies/EnemyMovementMode.cs`
- Create: `game1/scripts/enemies/EnemyMovementPolicy.cs`
- Create: `game1/resources/enemies/scout_drone.tres`
- Create: `game1/resources/enemies/patrol_tank.tres`
- Create: `game1/resources/enemies/assault_vehicle.tres`
- Create: `game1/resources/enemies/mortar_carrier.tres`
- Modify: `game1/scripts/content/ContentCatalog.cs`
- Modify: `game1/resources/content_catalog.tres`
- Modify: `game1/scripts/enemies/EnemyTank.cs`
- Modify: `game1/scripts/combat/WaveDirector.cs`
- Modify: `game1/scripts/bosses/BossSummonController.cs`
- Modify: `game1/scripts/app/AppRoot.cs`
- Test: `game1/tests/headless/BlockadeCityEnemyTestHost.cs`
- Test: `game1/tests/headless/ArenaWaveTestHost.cs`
- Test: catalog helpers in `CoreBuildTestHost.cs`、`RewardControllerTestHost.cs`、`ProtocolRuntimeTestHost.cs`

**Interfaces:**
- Produces: `ContentCatalog.GetEnemy(BehaviorId)`、`EnemyTank.Configure(EnemyDefinition)`、`EnemyMovementPolicy.Calculate(...)`。
- Consumes: `IEnemyPathProvider` 只负责从当前位置到职责目标点的路径。

- [ ] **Step 1: 写入四职责红灯**

验证：

```text
Scout：高速侧绕，低装甲、低伤害、短预警
Patrol：稳定追击，中等射程与耐久
Assault：近距离快速压迫，短冷却但不靠高血
Mortar：保持远距，玩家贴近时撤退，长预警高伤害
```

并验证四资源 Id、行为、移动模式和数值区间唯一有效。

- [ ] **Step 2: 运行红灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/blockade_city_enemy_test_host.tscn
```

Expected: `EnemyDefinition` 和职责策略不存在。

- [ ] **Step 3: 实现资源与目录校验**

生产参数：

```text
Scout  armor 12 / speed 72 / range 78 / cooldown 1.00 / telegraph .16 / damage 5
Patrol armor 20 / speed 42 / range 100 / cooldown 1.35 / telegraph .35 / damage 9
Assault armor 24 / speed 58 / range 68 / cooldown .80 / telegraph .20 / damage 8
Mortar armor 18 / speed 32 / range 175 / retreat 105 / cooldown 2.10 / telegraph .85 / damage 16
```

目录必须恰有四个 `BehaviorId`，资源不得共享运行状态。

- [ ] **Step 4: 让 EnemyTank 消费职责意图**

`EnemyMovementPolicy` 返回职责目标点和是否停驻；Scout 取切向侧绕点，Assault/Patrol 追击，Mortar 在 105px 内向外撤退、105～175px 保持、超距重新靠近。路径仍由共享 A* 生成，不每帧重算。

- [ ] **Step 5: 运行定向绿灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/blockade_city_enemy_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/arena_wave_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/enemy_projectile_test_host.tscn
```

Expected: 资源、移动意图、生成和敌弹全部 PASS。

---

### Task 3: 第五波组合压力与单一过载精英

**Files:**
- Create: `game1/scripts/content/EliteModifierDefinition.cs`
- Create: `game1/resources/elites/overdrive_elite.tres`
- Modify: `game1/scripts/content/ContentCatalog.cs`
- Modify: `game1/resources/content_catalog.tres`
- Modify: `game1/resources/arenas/blockade_city_arena.tres`
- Modify: `game1/scripts/combat/WaveDirector.cs`
- Modify: `game1/scripts/enemies/EnemyTank.cs`
- Test: `game1/tests/headless/ArenaWaveTestHost.cs`
- Test: `game1/tests/headless/BlockadeCityEnemyTestHost.cs`

**Interfaces:**
- Produces: `ContentCatalog.GetEliteModifier("elite_overdrive")`、`EnemyTank.ConfigureElite(...)`。

- [ ] **Step 1: 写入精英红灯**

验证第 5 波只生成一个 `elite_overdrive`；其循环为 `1.25s ×1.55` 过载和 `.75s ×.55` 冷却，装甲倍率为 `1.0`，证明压力不靠生命膨胀。

- [ ] **Step 2: 运行红灯**

Run:

```powershell
godot --headless --path game1 --scene res://tests/headless/arena_wave_test_host.tscn
```

Expected: 精英资源和运行时配置不存在。

- [ ] **Step 3: 接入精英资源与第五波编队**

五波敌军池：

```text
1 Scout/Patrol
2 Scout/Assault/Patrol
3 Patrol/Assault/Mortar
4 Scout/Assault/Patrol/Mortar
5 Mortar/Patrol/Scout/Assault（Assault 为精英槽）
```

生成间隔依次 `4.0/3.8/3.6/3.4/3.2`，存活上限 `4/5/5/6/7`；不改变五波总时长。

- [ ] **Step 4: 运行定向绿灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/arena_wave_test_host.tscn
```

Expected: 精英数量、周期、清场门禁和波次组合全部 PASS。

---

### Task 4: 路障指挥车两阶段节奏数据化

**Files:**
- Modify: `game1/scripts/content/BossDefinition.cs`
- Modify: `game1/resources/bosses/roadblock_commander.tres`
- Modify: `game1/scripts/bosses/RoadblockCommander.cs`
- Modify: `game1/scripts/bosses/BossEncounterController.cs`
- Modify: `game1/scripts/app/AppRoot.cs`
- Test: `game1/tests/headless/BossChargeRecoveryTestHost.cs`
- Test: `game1/tests/headless/ProtocolRuntimeTestHost.cs`

**Interfaces:**
- `BossDefinition` 增加 `BarrierIntervalSeconds`、`ThreatIntervalSeconds`、`ChargeIntervalSeconds`、`ChargeTelegraphSeconds`、`VulnerableSeconds`。

- [ ] **Step 1: 写入 Boss 节奏红灯**

验证阶段一哨位/召唤与路障间隔为有限正值；阶段二冲锋间隔大于预警时间；弱点窗口不少于 `1.8s`；配置直接注入 Boss 和遭遇控制器。

- [ ] **Step 2: 运行红灯**

Run:

```powershell
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase
godot --headless --path game1 --scene res://tests/headless/boss_charge_recovery_test_host.tscn
```

Expected: 新定义字段不存在。

- [ ] **Step 3: 实现数据注入与节奏**

使用：

```text
barrier 3.4s / phase-one threat 4.6s /
phase-two charge interval 1.8s / telegraph .85s / vulnerable 2.0s
```

不改变二阶段“冲锋结束才开放弱点”的规则，不恢复基地目标。

- [ ] **Step 4: 运行定向绿灯**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase
godot --headless --path game1 --scene res://tests/headless/boss_charge_recovery_test_host.tscn
```

Expected: Boss 配置和弱点恢复测试 PASS。

---

### Task 5: 多核心多种子审计、集中自检与验收

**Files:**
- Create: `game1/tests/headless/Bc02BalanceAuditTestHost.cs`
- Create: `game1/tests/headless/bc02_balance_audit_test_host.tscn`
- Create: `docs/iterations/iteration-bc02-combat-build-depth.md`
- Modify: `README.md`
- Modify: `game1/README.md`

- [ ] **Step 1: 写入平衡审计**

对三个核心各运行 24 个固定种子，验证普通/稀有奖励始终三选一、确定性重复、三路线均能在样本中出现、非核心标签仍能出现；同时验证四敌军职责和五波组合不是单调生命增长。

- [ ] **Step 2: 运行 BC-02 定向审计**

Run:

```powershell
dotnet build game1/Game1.csproj --nologo
godot --headless --path game1 --scene res://tests/headless/bc02_balance_audit_test_host.tscn
```

Expected: `[PASS] bc02_balance_audit`。

- [ ] **Step 3: 执行集中全量自检**

Run:

```powershell
dotnet test GodotTank.sln --nologo
dotnet build game1/Game1.csproj --nologo
dotnet build game1/Game1.csproj -c Release --nologo
godot --headless --path game1 --scene res://tests/headless/bc02_balance_audit_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/core_build_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/blockade_city_enemy_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/arena_wave_test_host.tscn
godot --headless --path game1 --scene res://tests/headless/boss_charge_recovery_test_host.tscn
godot --headless --path game1 --scene res://tests/integration/mvp_test_runner.tscn
godot --headless --path game1 --editor --quit
godot --headless --path game1 --scene res://scenes/app/main.tscn --quit-after 180
git diff --check
```

- [ ] **Step 4: 多核心可见试玩**

至少分别使用突破重炮、过载速射和电驱游骑开始一局；检查构筑路线标签、敌军职责、第五波精英过载/冷却和 Boss 两阶段节奏。记录实际观察，不用自动化替代。

- [ ] **Step 5: 交付一次用户验收**

只在 BC-02 全部开发和集中自检完成后提供策划验收步骤；用户确认前不得提交或推送。
