# Alpha 02C 五波稀疏竞技场实施计划

> **执行约束：** 单主智能体按本文内联执行。用户验收前不得提交或推送；同一功能块连续实现，迭代全部完成后再集中全量自检。

**目标：** 在封锁城区灰盒中建立 45/50/55/60/70 秒五波竞技场闭环，第 5 波包含一个可追踪精英槽位，刷新结束后必须清空残敌才能进入奖励。

**架构：** `RunController` 只保留单局级竞技场/失败职责；纯 C# `ArenaController` 是五波状态机唯一真值；Godot `WaveDirector` 只负责计时生成、存活计数和第 5 波精英槽位。主场景通过事件组合三者，不在 `_Process()` 复制波次判断。

**技术栈：** Godot 4.7 .NET、C#、游戏程序集 `net8.0`、NUnit `net10.0`、`Resource/.tres`、`TileMapLayer`、480×270 逻辑画布。

## 全局约束

- 正常配置固定五波：45/50/55/60/70 秒。
- 奖励顺序固定为普通协议、维护、普通协议、维护、稀有协议；02C 只实现确认门禁，不提前实现奖励效果。
- 第 5 波必须生成一个精英槽位，精英仍存活时不得结算。
- 地图为 3～5 组、每组 1～3 格的小型障碍岛；中心无长墙，主要通路至少 72 像素宽。
- 不新增敌军种类、经验、即时升级、核心选择、正式精英词条或 Boss 战。
- 不新增依赖，不改变 Godot/.NET 版本，不使用未确认素材替换玩法规则。

---

### 任务 1：五波领域状态机

**文件：**

- 新建：`game1/scripts/run/ArenaState.cs`
- 新建：`game1/scripts/run/RewardKind.cs`
- 新建：`game1/scripts/run/ArenaController.cs`
- 新建：`tests/Game1.Tests/Run/ArenaControllerTests.cs`
- 修改：`game1/scripts/run/RunState.cs`
- 修改：`game1/scripts/run/RunController.cs`

**接口：**

- `ArenaController.BeginArena(ArenaDefinition definition)`
- `ArenaController.OnIntroFinished()`
- `ArenaController.OnWaveSpawnWindowEnded()`
- `ArenaController.OnAllEnemiesCleared()`
- `ArenaController.ConfirmReward(string rewardId)`
- `ArenaController.OnPlayerRunFailed()`
- `event Action<WaveDefinition> WaveRequested`
- `event Action<RewardKind> RewardRequested`

- [x] 先写失败测试，覆盖五波顺序、清场门禁、第 5 波奖励后进入 `BossIntro`、非法迁移拒绝。
- [x] 运行单个测试程序集并观察类型缺失导致的预期红灯。
- [x] 实现最小状态机；`RunState` 增加只读竞技场/波次索引和受控写入口。
- [x] 把 `RunController` 收缩为单局级竞技场与失败裁决，不再生成房间奖励。
- [x] 运行状态机定向测试转绿。

### 任务 2：数据驱动竞技场与波次资源

**文件：**

- 新建：`game1/scripts/content/WaveDefinition.cs`
- 新建：`game1/scripts/content/ArenaDefinition.cs`
- 新建：`game1/resources/arenas/blockade_city_arena.tres`
- 新建：`tests/Game1.Tests/Content/ArenaDefinitionTests.cs`

**接口：**

- `WaveDefinition.Validate()`
- `ArenaDefinition.Validate()`
- `ArenaDefinition.GetWave(int waveNumber)`

- [x] 测试恰有五波、波号唯一、时长顺序、奖励顺序以及仅第 5 波包含精英。
- [x] 资源包含现有三种 `BehaviorId` 的循环池、生成间隔、场上数量上限和四周入口安全距离。
- [x] 资源校验拒绝空场景、非法持续时间、空敌军池和错误精英位置。

### 任务 3：限时生成与残敌清场

**文件：**

- 新建：`game1/scripts/combat/SpawnEntrance.cs`
- 新建：`game1/scripts/combat/WaveDirector.cs`
- 新建：`game1/tests/headless/ArenaWaveTestHost.cs`
- 新建：`game1/tests/headless/arena_wave_test_host.tscn`

**接口：**

- `WaveDirector.Configure(WaveDefinition, IReadOnlyList<SpawnEntrance>, int, int, int, IEnemyPathProvider)`
- `WaveDirector.StartWave()`
- `WaveDirector.StopSpawning()`
- `event Action<double> TimeChanged`
- `event Action<int> EnemyCountChanged`
- `event Action SpawnWindowEnded`
- `event Action AllEnemiesCleared`

- [x] Headless 测试先引用缺失导演并观察预期红灯。
- [x] 物理帧递减刷新窗口；时间归零只停止生成，不删除敌人。
- [x] 存活数为零且刷新已停止时只发一次清场事件。
- [x] 第 5 波首先生成一个使用现有敌军行为的金色精英槽位，占用独立追踪标记；不提前实现正式精英词条。
- [x] 入口按玩家安全距离过滤；全部不安全时选择最远入口。

### 任务 4：主场景、HUD、验收入口与稀疏灰盒

**文件：**

- 修改：`game1/scripts/app/AppRoot.cs`
- 修改：`game1/scripts/ui/AcceptanceMenu.cs`
- 新建：`game1/scripts/ui/WaveRewardPanel.cs`
- 修改：`game1/scripts/ui/DebugOverlay.cs`
- 修改：`game1/scenes/app/main.tscn`
- 修改：`game1/scenes/rooms/mvp_combat_room.tscn`
- 修改：`game1/resources/rooms/mvp_combat_room.tres`

**流程：**

```text
ArenaController 请求波次
→ WaveDirector 限时生成
→ 刷新结束进入 Cleanup
→ 玩家击毁全部残敌/精英
→ 可见奖励确认
→ 下一波
→ 第 5 波奖励后停在 BossIntro 占位
```

- [x] 主场景只订阅控制器事实，不在 `_Process()` 复制波次规则。
- [x] HUD 同时显示竞技场、波次、刷新/清场状态、剩余秒数、敌军数和精英状态。
- [x] Debug 验收菜单提供结束刷新（不删除残敌）、敌军全灭、结束本轮并结算、确认并到下一波和结束本局；只有验收命令可调用导演的受控清场接口。
- [x] 奖励面板提供可见确认按钮；02C 的确认不应用协议或维修效果。
- [x] 地图改为四个三格以内障碍岛，四周入口可达，中心与横纵主通路至少三台坦克宽。

### 任务 5：Batch 2 小样与集中自检

**文件：**

- 新建：`asset_sources/ai_generated/batch-02-mobile-core/sparse-arena-sample/`
- 修改：`asset_sources/AI_PROTOTYPE_ASSETS.md`
- 新建：`docs/iterations/iteration-alpha-02c-five-wave-arena.md`

- [x] 以已确认 Gate 0 和 Batch 1 第二版为参考，只生成一张稀疏城区竞技场小样，不批量扩展地形。
- [x] 检查小样无中继站、无底部基地，开放区和四个入口清晰。
- [x] 迭代开发完成后集中运行构建、全部纯 C# 测试、相关 Godot headless、主场景启动和可见流程。
- [x] 记录架构矩阵、测试结果、已知限制和策划验收步骤。
- [ ] 用户验收前不暂存、不提交、不推送。
