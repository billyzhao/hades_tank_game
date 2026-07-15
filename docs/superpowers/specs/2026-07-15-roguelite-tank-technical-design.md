# 废土中继：Godot 技术设计

> 对应策划：[《废土中继：肉鸽坦克战 — 游戏策划》](./2026-07-15-roguelite-tank-design.md)

> 对应开发计划：[《Roguelite Tank MVP Implementation Plan》](../plans/2026-07-15-roguelite-tank-mvp.md)

## 1. 文档目的

本文定义《废土中继》最小可玩原型（MVP）的 Godot 实现边界、运行时结构、数据流和验收方式。开发计划应以本文为技术依据，按可运行迭代落地。

当前 `game1` 是 Godot 4.7 .NET / C# 的单场景移动与碰撞演示。它可用于确认本机工具链，但 `Main.cs` 和 `GameRules.cs` 不作为正式架构约束。正式开发在同一项目中逐步替换该演示，不建立第二套引擎工程。

### 技术目标

- 维持 60 FPS 的俯视角 2D 战斗；
- 移动、独立瞄准、射击和反弹在不同帧率下保持一致；
- 新敌人、武器、协议和房间主要通过资源资产扩展；
- 单局状态与静态配置严格分离，避免共享 `Resource` 被运行时修改；
- 每次迭代都能构建、启动并完成明确的可玩验证。

### MVP 非目标

- 不做在线多人、完全程序生成地图、完整局外叙事和多存档槽；
- 不提前实现后续三个区域及其 Boss；
- 不在没有性能证据前建立通用对象池、ECS 或复杂依赖注入框架；
- 不为尚未进入 MVP 的内容设计通用脚本语言。

## 2. 基础技术决策

| 项目 | 决策 |
|---|---|
| 引擎 | Godot 4.7 .NET，C#，目标框架 `net8.0`。 |
| 渲染 | 2D、OpenGL Compatibility；兼容当前较老显卡环境。 |
| 逻辑画布 | 480×270，16:9；默认窗口 960×540，以整数倍显示。 |
| 时间步 | 移动、弹道和碰撞放在 `_PhysicsProcess`；UI 与纯视觉插值放在 `_Process`。 |
| 坐标 | 连续世界坐标；房间导航另用逻辑网格，不限制玩家只能四方向移动。 |
| 内容配置 | Godot C# `Resource` + 文本 `.tres`。静态资源在运行时视为只读。 |
| 运行时状态 | 普通 C# 对象保存单局状态，节点只保存自身瞬时状态。 |
| 通信 | 父节点直接调用子节点命令；子场景用类型化 C# 事件/ Godot 信号向上报告事实。 |
| 随机 | 每局一个显式种子；敌群、路线和奖励都从该种子的派生随机流生成。 |

480×270 用于形成现代高精度像素画面，同时在 960×540 下得到无插值的 2 倍显示。所有像素纹理关闭过滤和 mipmap；摄像机最终渲染位置吸附到整数像素，但游戏物理位置不取整。

## 3. 总体运行结构

```text
Main.tscn / AppRoot
├── RunController
│   ├── RunState（普通 C# 对象，不是节点）
│   ├── RewardGenerator
│   └── SceneTransition
├── RoomHost
│   └── CombatRoom（动态实例）
│       ├── RoomController
│       ├── Terrain
│       │   ├── GroundLayer
│       │   ├── BrickLayer
│       │   └── SteelLayer
│       ├── NavigationGrid
│       ├── RelayStation
│       ├── PlayerSpawn
│       ├── EnemySpawns
│       ├── Actors
│       │   ├── PlayerTank
│       │   └── EnemyTank...
│       ├── Projectiles
│       └── Effects
└── UI
    ├── CombatHud
    ├── RewardScreen
    ├── PauseScreen
    └── DebugOverlay（仅调试构建）
```

仅把真正跨场景且无房间所有权的服务设为 Autoload：

- `SaveService`：设置和局外解锁的加载/保存；
- `SceneRouter`：启动、返回标题和崩溃恢复入口。

`RunController`、战斗事件和构筑状态不做全局单例。它们属于当前一局，退出本局时随 `RunRoot` 一起释放，避免下一局残留信号订阅和状态。

## 4. 目录规划

```text
game1/
├── assets/
│   ├── audio/
│   ├── sprites/
│   ├── tilesets/
│   └── ui/
├── data/
│   ├── enemies/
│   ├── modules/
│   ├── protocols/
│   ├── rooms/
│   └── weapons/
├── scenes/
│   ├── app/
│   ├── actors/
│   ├── combat/
│   ├── rooms/
│   └── ui/
├── scripts/
│   ├── app/
│   ├── combat/
│   ├── content/
│   ├── enemies/
│   ├── player/
│   ├── run/
│   └── ui/
└── tests/
    ├── unit/
    └── integration/
```

场景脚本与场景同名。可复用节点优先组合，不建立超过两层的玩法继承树。敌人共享 `EnemyTank` 场景，差异由定义资源和行为策略决定；Boss 使用独立场景组合专属部件。

## 5. 核心场景与组件

### 5.1 玩家坦克

```text
PlayerTank (CharacterBody2D)
├── BodySprite (Sprite2D)
├── BodyCollision (CollisionShape2D)
├── Turret (Node2D)
│   ├── TurretSprite
│   └── Muzzle (Marker2D)
├── WeaponController (Node)
├── HealthComponent (Node)
├── DashComponent (Node)
├── ActiveAbilityComponent (Node)
└── Hurtbox (Area2D)
```

- `PlayerTank` 只协调输入、车体移动与炮塔朝向；
- `WeaponController` 根据武器快照创建炮弹，不直接读取协议列表；
- `HealthComponent` 处理装甲、无敌帧和报废事件；
- `DashComponent` 处理冷却、位移、碰撞和冲刺事件；
- `BuildController` 在房间外持有构筑，并向玩家生成本场有效属性快照。

坦克采用 `CharacterBody2D`，用 `MoveAndSlide()` 驱动，不使用 `RigidBody2D` 模拟。这样车体响应可控，不会因质量、摩擦或帧率改变操作手感。

### 5.2 中继站

`RelayStation` 使用 `StaticBody2D` 作为实体碰撞。房间节点的 `HealthComponent` 从 `RunState` 初始化，只负责显示、接收伤害和报告变化；真实剩余耐久存于 `RunState`，发生变化后立即回写，绝不修改静态配置资源。

中继站防护墙、护盾和自动机炮是独立子场景。它们通过中继站的插槽 Marker 实例化，不把所有防御逻辑堆进 `RelayStation.cs`。

### 5.3 敌人

普通敌人根节点统一为 `CharacterBody2D`：

```text
EnemyTank
├── Body/Turret/Collision
├── HealthComponent
├── WeaponController
├── TelegraphController
└── EnemyBrain
```

`EnemyDefinition` 用 `BehaviorId` 选择行为工厂。MVP 行为包括：

- `PatrolShooterBehavior`：保持距离、转炮、直线点射；
- `AssaultBehavior`：快速接近玩家并短距离连射；
- `SiegeBehavior`：寻路至攻击位，优先瞄准中继站。

行为统一经过 `AcquireTarget → Move → Telegraph → Attack → Recover` 状态，不允许跳过可读的攻击预警。Boss 不继承一棵复杂 AI 类树，而用独立的阶段控制器组合移动、召唤、架墙和冲锋动作。

### 5.4 房间控制器

房间状态机固定为：

```text
Loading → Intro → Combat → Cleared → Reward → Exiting
                    └──────────────→ Failed
```

`RoomController` 负责连接中继站、玩家和敌军导演的事件。它判断房间是否清空，但不直接修改 HUD、播放玩家动画或生成协议效果。房间清空后冻结敌人生成，清理危险炮弹，再把 `RoomResult` 交给 `RunController`。

## 6. 输入、移动与瞄准

输入动作统一在 `project.godot` 中定义：

- `move_left/right/up/down`；
- `aim_left/right/up/down`（手柄右摇杆）；
- `fire_primary`、`dash`、`active_ability`、`interact`、`pause`。

键鼠瞄准取鼠标世界坐标；手柄瞄准使用右摇杆向量并应用死区。最后一次有效设备决定提示图标，但不影响玩法状态。

移动速度以像素/秒配置，使用物理帧 `delta`。冲刺是有限持续时间的受控高速移动，不瞬移穿墙。冲刺期间是否无敌、能否伤敌由 `DashDefinition` 配置，MVP 默认短暂无敌但不能穿越钢墙。

## 7. 弹道、伤害与碰撞

### 7.1 炮弹方案

炮弹使用 `Node2D` + 每物理帧线段扫掠查询，而不是依赖 `RigidBody2D` 自由模拟。每帧从旧位置向预计新位置做 `IntersectRay`：

1. 找到最近命中；
2. 根据碰撞层和材质决定伤害、销毁、穿透或反弹；
3. 反弹时用表面法线计算新方向，并消耗剩余帧距离；
4. 单帧反弹次数设硬上限，防止卡在墙角形成死循环。

这套方案可避免高速炮弹穿过薄墙，并能稳定复现反弹构筑。视觉拖尾与命中特效不参与碰撞判定。

### 7.2 碰撞层

| 层 | 名称 | 用途 |
|---:|---|---|
| 1 | `world_steel` | 不可破坏、可反弹的钢墙和场地边界。 |
| 2 | `world_brick` | 可破坏砖墙。 |
| 3 | `player` | 玩家实体。 |
| 4 | `enemy` | 普通敌人与 Boss 可受击实体。 |
| 5 | `relay` | 中继站和可受击防御设施。 |
| 6 | `trigger` | 奖励、出口和非实体交互区。 |
| 7 | `hazard` | 爆炸、地雷、持续伤害区。 |

炮弹不依靠自身物理层寻找目标，而根据阵营构建射线查询 mask：玩家炮弹查询世界与敌人；敌人炮弹查询世界、玩家与中继站。爆炸和地雷使用 `Area2D` 形状查询。

### 7.3 伤害契约

所有可受伤对象实现 `IDamageable.ApplyDamage(DamageContext)`。`DamageContext` 至少包含来源阵营、基础伤害、命中点、方向、伤害标签和来源实例 ID。

即时结果用直接调用返回 `DamageResult`；“受伤完成”“单位销毁”等事实再由事件上报。这样同一次命中不会因多个监听器重复结算伤害。

伤害顺序固定为：基础伤害 → 攻击方加成 → 防御方减伤/护盾 → 最小值钳制 → 扣除装甲 → 发出事件。协议只能在公开钩子改变数据，不直接改写其他节点私有字段。

## 8. 可破坏地形与导航

房间使用 Godot 4.7 的 `TileMapLayer`：地面、砖墙和钢墙分层。TileSet 自定义数据记录 `terrain_kind`、`max_hp`、`reflects_projectile` 和 `blocks_navigation`。

`DestructibleTerrain` 维护本房间砖块坐标到当前耐久的运行时字典。炮弹命中时用碰撞点转换为格子坐标；砖块耐久归零后擦除单元格、生成碎片并通知导航网格更新。静态 `.tres`/TileSet 资产绝不保存当前耐久。

敌人导航使用 `AStarGrid2D`：

- 房间加载时从阻挡格构建一次；
- 砖墙摧毁或工程单位架墙时只更新相关格；
- AI 低频重算路径，移动仍在连续坐标中完成；
- 玩家与其他坦克不写入静态导航网格，局部避让通过短射线和分离力处理。

该方案比为每次墙体变化重烘焙导航多边形更可控，也符合 FC 式格状场地。

## 9. 单局状态与战场重启

`RunState` 是普通 C# 数据对象，至少包含：

- 运行种子和当前区域/房间索引；
- 中继站当前/最大耐久；
- 玩家当前/最大装甲；
- 剩余战场重启次数；
- 已选择核心模块和协议栈；
- 本局货币与临时资源。

坦克装甲归零时：

1. `RoomController` 进入 `Rebooting` 子状态，暂停新一轮攻击生成；
2. 消耗一次重启，玩家失去控制并播放报废反馈；
3. 清除玩家周围小范围敌方炮弹；
4. 在中继站附近的安全点重建玩家，默认恢复 50% 最大装甲并获得短暂无敌；
5. 若无重启次数，则房间和本局进入失败结算。

恢复比例、无敌时间和清弹半径均为可调数据，不写死在流程代码中。

## 10. 构筑与奖励系统

### 10.1 静态定义

`ProtocolDefinition : Resource` 包含：

- 稳定唯一 `Id`、显示名称、描述和图标；
- 部门、稀有度、基础权重；
- 提供标签、需求标签和冲突标签；
- 一个或多个 `ProtocolEffectDefinition`；
- 最大叠加次数和升级规则。

核心模块使用 `ModuleDefinition`，结构类似但在单局开始或稀有事件中选择。

### 10.2 运行时效果

`BuildController` 把已选定义转换为运行时效果实例。效果只订阅明确的战斗钩子：

- `ShotFired`、`ProjectileHit`、`EnemyDestroyed`；
- `DashStarted`、`PlayerDamaged`；
- `RelayDamaged`、`RoomCleared`。

属性计算统一走 `StatPipeline`：基础值 → 固定加法 → 百分比加法 → 乘法修正 → 上下限。协议效果通过修正器参与计算，不能直接永久修改 `WeaponDefinition`。

### 10.3 三选一生成

`RewardGenerator` 使用本局随机流，按以下顺序生成三个不重复候选：

1. 过滤未解锁、已满层、冲突或需求不满足的协议；
2. 按稀有度和基础权重抽取；
3. 对已有标签形成的有效联动给予有限加权；
4. 保留至少一个不依赖当前流派的通用候选，避免系统替玩家做决定。

同一种子和相同选择序列必须得到相同奖励结果，便于复现平衡问题。

## 11. 敌军导演与威胁预算

`RoomDefinition` 保存多个波次，每个波次由出生组、时间/击杀触发条件和威胁预算组成。`EnemyDirector` 只决定“何时、在哪里、生成什么”，不控制单个敌人的行为。

每个敌人定义有威胁值。房间难度通过预算、编队约束和出生方向增长，不直接对所有敌人乘血量。MVP 的编队规则包括：

- 同时存在的攻城炮车数量有上限；
- 出生点不能在玩家和中继站的安全半径内；
- 新敌人在可见预警后才获得碰撞和攻击能力；
- 最后一波生成后且所有敌人销毁，房间才进入清空状态。

## 12. 数据资源与内容校验

| 资源 | 关键字段 |
|---|---|
| `EnemyDefinition` | Id、场景、装甲、速度、行为、武器、威胁值、标签。 |
| `WeaponDefinition` | 射速、伤害、速度、寿命、反弹、穿透、爆炸、标签。 |
| `ProtocolDefinition` | 部门、稀有度、权重、标签、效果、叠加规则。 |
| `ModuleDefinition` | 底盘属性、初始武器、技能和规则修正。 |
| `RoomDefinition` | 房间场景、波次、出生规则、奖励类别。 |
| `RunBalanceDefinition` | 初始耐久、重启次数、奖励权重和区域曲线。 |

项目维护一个显式的 `ContentCatalog.tres`，以类型化数组引用全部敌人、武器、协议、模块和房间定义。启动调试版本时，`ContentCatalog` 加载该资产并校验：Id 唯一、引用不为空、数值范围合法、协议依赖可满足、房间包含中继站和出生点。这样导出包不依赖运行时遍历 `res://` 文件夹。关键内容错误在调试构建中立即终止进入战斗；发布构建记录错误并跳过无效内容，返回安全的标题界面。

## 13. 通信规则

为避免“全局事件总线”难以追踪，采用以下规则：

- 同一场景内明确的一对一命令使用直接方法调用；
- 可复用子场景向父级报告完成事实时使用类型化事件/信号；
- 多目标广播只使用少量命名组，如 `enemies`、`player_owned_defenses`；
- HUD 只监听 `RunController`/`RoomController` 提供的只读状态事件，不直接寻找玩家子节点；
- 切换房间前，长生命周期对象显式解除对房间对象的订阅。

主要事件包括 `ArmorChanged`、`RelayIntegrityChanged`、`TankDisabled`、`EnemyDestroyed`、`RoomCleared`、`RoomFailed`、`RewardChosen`。

## 14. 保存与版本兼容

MVP 仅持久化设置和局外解锁，不保存战斗中途状态。保存文件写入 `user://save.json`，使用带 `schema_version` 的普通 DTO，不直接序列化 Godot 节点或 `Resource`。

保存流程为：写入临时文件 → 校验可重新读取 → 替换正式文件。加载失败时保留损坏文件副本、记录错误并回退到默认解锁，不让游戏卡在启动阶段。

单局随机种子和结束摘要写入最近一局日志，便于复现奖励和敌群问题，但不作为续局存档。

## 15. UI 与暂停

`CombatHud` 显示坦克装甲、中继站耐久、重启次数、冲刺/技能冷却和当前协议图标。关键警告优先使用图形、颜色和音效三重提示，不能只靠文字或颜色。

奖励界面打开时，`RoomController` 已处于 `Reward` 状态，战斗逻辑暂停。暂停菜单使用 SceneTree pause；需要继续运行的 UI 节点设置为 `WhenPaused`。游戏焦点丢失时自动暂停，恢复后等待玩家输入再继续。

## 16. 错误处理与调试能力

- 固定子节点在 `_Ready()` 中用类型化 `GetNode<T>` 获取并在缺失时尽早失败；可选节点用 `GetNodeOrNull<T>`；
- 动态实例释放统一使用 `QueueFree()`，跨帧引用先检查 `GodotObject.IsInstanceValid()`；
- 内容错误使用 `GD.PushError`，可恢复的异常显示用户可理解的回退界面；
- `DebugOverlay` 可显示 FPS、物理帧、活动敌人/炮弹数、随机种子、当前房间状态和导航路径；
- 调试快捷键可重开当前房间、切换无敌和生成指定敌人，但发布构建不注册这些输入。

## 17. 性能预算

目标硬件以当前 OpenGL 3.3 兼容环境为下限参考，默认输出 960×540，稳定 60 FPS。

| 指标 | MVP 预算 |
|---|---:|
| 同屏普通敌人 | 30 |
| 活动炮弹 | 160 |
| 持续伤害/触发区 | 40 |
| 单帧每枚炮弹反弹处理 | 最多 4 次 |
| AI 完整寻路频率 | 每个单位每秒不超过 4 次，并错峰执行 |

先用 Godot Profiler、Monitors 和调试计数定位瓶颈。只有实例化或 GC 被确认造成帧尖峰时，才为炮弹和特效引入对象池；不预先池化所有节点。

## 18. 测试与验收

### 18.1 纯 C# 单元测试

建立独立测试项目，测试不依赖场景树的逻辑：

- 伤害结算顺序和钳制；
- 属性修正器叠加顺序；
- 相同种子的三选一可复现；
- 冲突标签、最大层数和联动加权；
- 中继站耐久、战场重启和失败条件；
- 存档 schema 迁移与损坏回退。

### 18.2 Godot 集成测试

用小型测试场景在 headless 模式运行：

- 玩家不能穿过钢墙，冲刺也不能越墙；
- 炮弹高速移动不穿墙，按法线正确反弹；
- 砖墙受伤后销毁并更新导航；
- 三种敌人选择正确目标并遵守攻击预警；
- 房间清空后停止生成并进入奖励状态；
- 坦克报废有重启时继续，无重启时结束；
- 中继站归零立即结束本局。

### 18.3 每次迭代的最低验证

1. `dotnet build` 无错误、无新警告；
2. Godot headless 启动和集成场景通过；
3. OpenGL 启动脚本人工游玩一轮；
4. 对本迭代验收项录制结果或保存调试日志。

## 19. 关键风险与控制

| 风险 | 控制方式 |
|---|---|
| 守点变成被迫蹲守 | 攻城单位数量受限；给玩家预警、自动防御和主动截击空间。 |
| 反弹弹道出现角落死循环 | 扫掠查询、位置微偏移和单帧反弹上限。 |
| 可破坏墙导致 AI 卡死 | 网格局部更新、寻路失败回退和调试路径显示。 |
| 协议效果互相直接改字段 | 统一战斗钩子与属性管线，静态资源只读。 |
| C# 节点释放后仍被事件持有 | 一局一作用域，房间退出显式解绑，释放前停止事件源。 |
| 内容量过早膨胀 | MVP 固定一个房间、三种敌人、8–12 个协议和一个 Boss。 |

## 20. 技术完成定义

技术基础达到可进入内容扩充阶段，必须同时满足：

- 一次短局可从战斗进入三选一，再进入下一场战斗；
- 双生命线和战场重启在所有边界条件下正确；
- 反弹、砖墙破坏与敌人寻路连续运行无明显错误；
- 至少两套协议联动能通过数据资产配置，而非修改玩家核心脚本；
- 固定种子可复现房间敌群与奖励；
- 性能预算内维持 60 FPS；
- 构建、核心单元测试和 headless 集成测试全部通过。

满足上述条件后，开发计划才进入更多房间、完整城区敌人池和 Boss 内容迭代。
