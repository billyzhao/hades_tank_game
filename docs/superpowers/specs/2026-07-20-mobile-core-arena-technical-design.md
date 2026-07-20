# 《废土中继》移动核心竞技场技术设计

状态：Alpha 02A 技术权威基线，等待用户验收。

日期：2026-07-20

对应策划：[《移动核心竞技场肉鸽重构设计》](./2026-07-20-mobile-core-arena-roguelite-redesign.md)

对应路线图：[《移动核心竞技场重构总实施路线图》](../plans/2026-07-20-mobile-core-arena-roadmap.md)

## 1. 目的与适用范围

本文定义固定中继站防守原型迁移为移动核心竞技场肉鸽时的运行时职责、公共类型、状态流、数据资源、暂停、存档、错误处理和测试边界。Alpha 02B 及后续实现必须以本文为技术依据。

旧技术文档 `2026-07-15-roguelite-tank-technical-design.md` 只用于解释现有 MVP 代码。与本文冲突时，以本文为准；旧 `RelayStation`、`RelayIntegrity`、房间清场与基地目标不得继续扩展。

## 2. 不可变技术基线

| 项目 | 约束 |
|---|---|
| 引擎 | Godot 4.7 stable .NET / C# |
| 游戏程序集 | `net8.0` |
| 独立测试项目 | `net10.0`，引用游戏程序集 |
| 渲染 | 2D、OpenGL Compatibility |
| 逻辑画布 | 480×270 |
| 默认窗口 | 1440×810，整数 3 倍显示 |
| 坐标 | 连续世界坐标；24 像素地图格只用于地形、导航和内容尺度 |
| 时间步 | 移动、炮弹、碰撞与战斗计时使用物理帧；纯 UI 动画使用普通帧 |
| 内容配置 | Godot C# `Resource` + 文本 `.tres`；运行时视为只读 |
| 单局状态 | 普通 C# 对象持有，不写回共享 `Resource` |
| 通信 | 父级调用子级命令；子场景用类型化 C# 事件或 Godot 信号上报事实 |
| 随机 | 每局一个显式种子，通过稳定派生流生成波次、掉落和奖励 |
| 依赖 | 不新增完整肉鸽模板、行为树、测试或依赖注入框架 |
| 执行 | 单主智能体；每个迭代完成后集中自检，用户验收前不提交推送 |

## 3. 目标场景树与所有权

```text
Main.tscn / AppRoot
├── RunControllerHost
│   ├── RunController              # 普通 C# 控制器，由 AppRoot 持有
│   ├── BuildController            # 普通 C# 构筑状态与计算
│   ├── RewardController           # 普通 C# 奖励生成与应用
│   └── LevelUpController          # 普通 C# 经验与升级队列
├── ArenaHost
│   └── ArenaInstance
│       ├── ArenaController
│       ├── Terrain
│       │   ├── Ground
│       │   ├── Destructible
│       │   └── Structure
│       ├── NavigationGrid
│       ├── SpawnEntrances
│       │   └── SpawnEntrance...
│       ├── WaveDirector
│       ├── Actors
│       │   ├── PlayerTank
│       │   └── Enemy...
│       ├── Pickups
│       ├── Projectiles
│       └── Effects
└── UI
    ├── CombatHud
    ├── StatLevelUpPanel
    ├── RewardPanel
    ├── PausePanel
    ├── AcceptanceMenu
    └── RunResultScreen
```

所有权规则：

- `AppRoot` 只负责装配控制器、实例化竞技场、绑定顶层事件和切换 UI；
- `RunController` 只管理核心选择、跨竞技场进度、通关和失败；
- `ArenaController` 是五波、波间奖励和 Boss 衔接的唯一状态机；
- `WaveDirector` 只生成敌人并报告“刷新窗口结束”“敌军清空”等事实，不决定奖励或换区；
- `BuildController` 是属性、协议、核心进化和辅助槽的唯一写入口；
- `PlayerTank` 只读取已经计算的构筑快照，不识别具体协议 Id；
- HUD 只订阅只读快照和事实事件，不遍历场景树推断业务状态。

## 4. 领域类型

### 4.1 枚举

```csharp
namespace Game1;

public enum RunPhase
{
    CoreSelection,
    Arena,
    Completed,
    Failed
}

public enum ArenaState
{
    Loading,
    Intro,
    WaveCombat,
    Cleanup,
    Reward,
    BossIntro,
    BossCombat,
    Completed,
    Failed
}

public enum RewardKind
{
    Stat,
    NormalProtocol,
    Maintenance,
    RareProtocol,
    CoreEvolution,
    MajorEvolution
}

public enum ProtocolRank
{
    None = 0,
    MkI = 1,
    MkII = 2,
    MkIII = 3
}

public enum CoreId
{
    Unselected = 0,
    BreachHeavy,
    OverloadRapid,
    ElectricRanger
}

public enum AuxiliaryTargetMode
{
    Nearest,
    HighestThreat,
    OwnerAimDirection,
    MovementDistance,
    AreaDensity
}
```

不得在运行中通过任意字符串表示阶段。资源稳定 Id 使用小写蛇形命名，例如 `core_breach_heavy`、`protocol_arsenal_ricochet`；枚举只表示固定程序语义。

### 4.2 构筑值对象

```csharp
namespace Game1;

public sealed record OwnedProtocol(
    string ProtocolId,
    ProtocolRank Rank);

public sealed record AuxiliarySlotState(
    string AuxiliaryId,
    int Rank);

public sealed record StatUpgradeState(
    StatId Stat,
    int Rank);

public sealed record BuildSnapshot(
    CoreId CoreId,
    IReadOnlyList<StatUpgradeState> Stats,
    IReadOnlyList<OwnedProtocol> Protocols,
    IReadOnlyList<AuxiliarySlotState> AuxiliarySlots,
    IReadOnlySet<string> Tags,
    IReadOnlyList<StatModifier> Modifiers);
```

快照必须复制集合或暴露只读集合。运行组件不能取得 `RunState` 的可变集合引用。

## 5. RunState

`RunState` 是完整单局跨场景状态的唯一持有者，不是 Godot 节点，不引用场景节点或 `Resource`。

```csharp
namespace Game1;

public sealed class RunState
{
    public int Seed { get; }
    public int ArenaIndex { get; private set; }
    public int WaveIndex { get; private set; }
    public int PlayerArmor { get; private set; }
    public int MaximumArmor { get; private set; }
    public int RebootsRemaining { get; private set; }
    public int Level { get; private set; }
    public int Experience { get; private set; }
    public CoreId CoreId { get; private set; }
    public IReadOnlyList<StatUpgradeState> StatUpgrades { get; }
    public IReadOnlyList<OwnedProtocol> Protocols { get; }
    public IReadOnlyList<AuxiliarySlotState> AuxiliarySlots { get; }

    public static RunState CreateNew(
        int seed,
        int maximumArmor = 100,
        int reboots = 1);

    internal void SelectCore(CoreId coreId);
    public void SetWaveIndex(int waveIndex);
    public void SynchronizeArmor(int armor, int maximumArmor);
    public bool TryConsumeReboot();
    public void RestoreAfterReboot();
    public void RepairArmor(int amount);
    public void RestoreArmorForNextArena();
    public void AddExperience(int amount, ExperienceCurve curve);
    public bool TryConsumePendingLevel(out int newLevel);
    public void AdvanceArena();
}
```

约束：

- `ArenaIndex` 为 0–4；`WaveIndex` 为 0–4；
- 新局以 `CoreId.Unselected` 开始，进入竞技场前必须且只能选择一次三个有效核心之一；内容目录不得为 `Unselected` 建立核心资源；
- `PlayerArmor` 始终钳制到 0–`MaximumArmor`；
- `RebootsRemaining` 永不为负；首个纵切默认 1 次；
- `RestoreAfterReboot()` 把装甲设置为向上取整的 50% 最大装甲；
- `RestoreArmorForNextArena()` 只恢复装甲，不恢复重启次数；
- 负经验、负维修、无效最大装甲和非法索引必须抛出参数异常；
- `RunState` 不直接生成奖励，不直接操作 UI，也不保存当前场景节点。

## 6. 顶层状态机

### 6.1 RunController

```csharp
namespace Game1;

public sealed class RunController
{
    public RunPhase Phase { get; private set; }
    public RunState State { get; }
    public event Action<RunPhase> PhaseChanged;
    public event Action<int> ArenaRequested;
    public event Action<RunResultSnapshot> RunCompleted;
    public event Action<RunResultSnapshot> RunFailed;

    public RunController(
        RunState state,
        BuildController build);

    public void SelectCore(CoreId coreId);
    public void OnArenaCompleted();
    public void OnArenaFailed();
}
```

合法流程：

```text
CoreSelection → Arena(0) → Arena(1) → Arena(2) → Arena(3) → Arena(4) → Completed
                         ↘ 任意时点无重启且装甲归零 → Failed
```

`RunController.SelectCore()` 只允许在 `CoreSelection` 调用，由它命令 `BuildController.SelectCore()` 并进入 `Arena`。`RunController` 不知道普通敌人数、波次剩余秒数或奖励卡内容，只接受竞技场完成/失败事实。

### 6.2 ArenaController

```csharp
namespace Game1;

public sealed class ArenaController
{
    public ArenaState State { get; private set; }
    public int CurrentWave { get; private set; }
    public RewardOffer CurrentReward { get; private set; }
    public event Action<ArenaState> StateChanged;
    public event Action<WaveDefinition> WaveRequested;
    public event Action<RewardOffer> RewardRequested;
    public event Action BossRequested;
    public event Action ArenaCompleted;
    public event Action ArenaFailed;

    public ArenaController(
        RunState runState,
        RewardController rewards);

    public void BeginArena(ArenaDefinition definition);
    public void OnWaveSpawnWindowEnded();
    public void OnAllEnemiesCleared();
    public void ConfirmReward(string rewardId);
    public void OnBossIntroFinished();
    public void OnBossDefeated();
    public void OnPlayerRunFailed();
}
```

状态迁移固定为：

```text
Loading → Intro → WaveCombat
WaveCombat --刷新计时结束--> Cleanup
Cleanup --场上敌人和第5波精英清空--> Reward
Reward --确认奖励，波1至4--> WaveCombat(下一波)
Reward --确认第5波稀有协议--> BossIntro
BossIntro → BossCombat → Completed
任意战斗状态 --玩家无重启且装甲归零--> Failed
```

第 5 波精英是当前波敌人集合的一部分；精英未被击毁时不能触发 `OnAllEnemiesCleared()`。奖励阶段与 BossIntro 阶段不生成普通敌人。

## 7. WaveDirector

`EnemyDirector` 在 Alpha 02C 迁移为 `WaveDirector`。新导演按时间和威胁预算出兵，不再要求“固定列表全部生成后清空才进入下一波”。

```csharp
namespace Game1;

public partial class WaveDirector : Godot.Node
{
    public int AliveEnemyCount { get; private set; }
    public double RemainingSpawnSeconds { get; private set; }
    public bool IsSpawning { get; private set; }

    public event Action<double> TimeChanged;
    public event Action<int> EnemyCountChanged;
    public event Action SpawnWindowEnded;
    public event Action AllEnemiesCleared;

    public void Configure(
        WaveDefinition definition,
        IReadOnlyList<SpawnEntrance> entrances,
        int runSeed,
        int arenaIndex,
        int waveIndex,
        IEnemyPathProvider pathProvider);

    public void StartWave();
    public void StopSpawning();
}
```

规则：

- `StartWave()` 只允许调用一次；
- 默认五波持续时间为 45、50、55、60、70 秒；
- 刷新窗口结束时只停止新生成，不删除存活敌人；
- 存活敌人归零且刷新已停止时只发出一次 `AllEnemiesCleared`；
- 每次实例化前先播放入口预警；预警期间敌人尚不存在、不能碰撞或受伤；
- 出生入口与玩家距离不足定义的安全距离时退出候选；全部入口暂不安全时选择最远入口，不在场内随机生成；
- 第 5 波必须从精英池生成一个带单一 `EliteModifierDefinition` 的单位；
- 导演不读取中继站、基地或奖励状态。

## 8. 经验、暂停与奖励

### 8.1 ExperienceCurve

```csharp
namespace Game1;

public sealed class ExperienceCurve
{
    public int GetRequiredExperience(int level);
}
```

首个纵切使用数据表或确定公式，使前期平均每波约 1 次、中后期约 2 次，完整单局目标 30–40 次。曲线属于数据和平衡参数，不在掉落节点中硬编码。

### 8.2 LevelUpController

```csharp
namespace Game1;

public sealed class LevelUpController
{
    public int PendingLevelUps { get; private set; }
    public RewardOffer CurrentOffer { get; private set; }
    public event Action<RewardOffer> OfferReady;
    public event Action QueueCompleted;

    public LevelUpController(
        RunState state,
        BuildController build,
        ControlledStatOfferGenerator offers,
        ExperienceCurve curve);

    public void AddExperience(int amount);
    public void Choose(string rewardId);
}
```

`CombatDataPickup` 只携带正整数数据量并发出收集事实；`CombatDataCollector` 管理场上掉落集合和波末回收。固定顺序是：清场 → 自动回收 → 依次处理全部待升级 → 波间奖励。

### 8.3 PauseCoordinator

手动暂停、失焦暂停、升级暂停和波间奖励可能重叠，不能由各 UI 直接写 `SceneTree.Paused`。

```csharp
namespace Game1;

public enum PauseReason
{
    Manual,
    FocusLost,
    LevelUp,
    InterWaveReward
}

public sealed class PauseCoordinator
{
    public bool IsPaused { get; }
    public event Action<bool> PauseChanged;
    public PauseCoordinator(Godot.SceneTree sceneTree);
    public void Acquire(PauseReason reason);
    public void Release(PauseReason reason);
    public bool Contains(PauseReason reason);
}
```

`PauseCoordinator` 由 `AppRoot` 组合并拥有，内部用集合保存原因；只有集合从空变非空或从非空变空时才改变 `SceneTree.Paused`。升级和奖励 UI 使用 `ProcessModeEnum.WhenPaused`。

完全暂停时：

- 敌人、玩家、炮弹、拾取物、波次计时器、Boss 和普通战斗动画停止；
- UI 输入与卡片动画继续；
- 不清除暂停前的炮弹；
- 所有积压升级完成并释放 `LevelUp` 原因后，`HealthComponent.GrantInvulnerability(0.4)`；
- 如果仍有 `Manual` 或 `FocusLost` 原因，游戏保持暂停。

### 8.4 奖励值对象

```csharp
namespace Game1;

public sealed record RewardChoice(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<string> Tags);

public sealed record RewardOffer(
    RewardKind Kind,
    IReadOnlyList<RewardChoice> Choices);

public sealed record RewardContext(
    int RunSeed,
    int ArenaIndex,
    int WaveIndex,
    int OfferOrdinal,
    CoreId CoreId,
    int PlayerArmor,
    int MaximumArmor,
    BuildSnapshot Build);
```

`RewardController` 根据 `RewardContext` 生成确定性三选一并验证选择来自当前候选。维护在 `PlayerArmor < MaximumArmor * 0.30` 时必须包含修复 25% 最大装甲；等于 30% 时不强制，但仍可随机出现。

```csharp
namespace Game1;

public sealed class RewardController
{
    public RewardOffer CurrentOffer { get; private set; }

    public RewardController(
        RunState state,
        BuildController build,
        ContentCatalog catalog);

    public RewardOffer Generate(
        RewardKind kind,
        RewardContext context);

    public void Choose(string rewardId);
    public void ClearCurrentOffer();
}

public sealed class ControlledStatOfferGenerator
{
    public RewardOffer Generate(RewardContext context);
}
```

`Choose()` 必须拒绝空 Id、非当前候选、重复确认和不符合奖励种类的效果；`ClearCurrentOffer()` 只在选择已应用或本局结束时调用。

## 9. BuildController 与数值管线

```csharp
namespace Game1;

public sealed class BuildController
{
    public event Action<BuildSnapshot> SnapshotChanged;
    public BuildSnapshot Snapshot { get; }

    public BuildController(
        RunState state,
        ContentCatalog catalog);

    public void SelectCore(CoreId coreId);
    public void ApplyStatUpgrade(StatId stat);
    public void ApplyProtocol(string protocolId);
    public void ApplyEvolution(string evolutionId);
    public void AddOrUpgradeAuxiliary(string auxiliaryId);
    public float EvaluateStat(StatId stat, float baseValue);
    public void EndRun();
}
```

`StatId` 迁移后的最小属性集合：

```csharp
public enum StatId
{
    Damage,
    FireCooldown,
    ProjectileSpeed,
    TurretTurnSpeed,
    MoveSpeed,
    DashCooldown,
    ArmorMax,
    PickupRadius,
    CriticalChance,
    CriticalDamage,
    AuxiliaryEfficiency,
    ProjectileBounces,
    ProjectilePiercing,
    ProjectileSplitCount,
    ExplosionRadius,
    ShieldCapacity,
    RebootArmorPercent,
    DashTrailDamage
}
```

删除 `RelayRepair` 和 `RelayShield`。新协议不得复用旧中继协议的稳定 Id，否则旧存档会把不同语义错误映射为同一内容。

属性计算继续使用固定值 → 加法百分比 → 乘法百分比 → 安全钳制。移动速度、射速、冷却和炮塔速度的具体软上限由内容资源声明；受控随机生成器在达到上限时过滤选项。

## 10. 数据资源

所有资源继承 Godot `Resource`，公开 `Validate()`；资源加载完成后视为只读。

### 10.1 CoreDefinition

```text
Id, CoreId, DisplayName, Description, Icon,
BaseWeapon, BaseMoveSpeed, BaseMaximumArmor,
DashDistanceMultiplier, DashArmorMultiplier,
Tags, EvolutionIds
```

校验：稳定 Id 非空且唯一；`CoreId` 必须是三个可玩核心之一，不能是 `Unselected`；基础数值为正有限值；必须恰有 3 个 Boss 1 进化 Id；所有引用存在。

### 10.2 ProtocolDefinition

```text
Id, DisplayName, Description, Department, Rarity,
BaseWeight, Tags, RequiredTags, ConflictTags,
PrerequisiteIds, ConflictIds, MaxRank, RankEffects
```

普通协议 `MaxRank = MkIII` 且必须提供 Mk.I、Mk.II、Mk.III 三组效果；稀有协议为一次性，`MaxRank = MkI`。标签、前置和冲突引用必须存在且无循环。满级协议退出普通奖励池。

### 10.3 AuxiliaryDefinition

```text
Id, DisplayName, Description, RuntimeScene,
TargetMode, BaseCooldown, MaximumRank,
Tags, RankEffects
```

校验：运行场景非空；冷却为正有限值；最大等级和效果组数一致；目标模式合法。首个纵切目录恰有 4 种辅助系统。

### 10.4 EnemyDefinition

```text
Id, DisplayName, RuntimeScene, Behavior,
MaximumArmor, MoveSpeed, ThreatCost,
CombatDataDrop, VisualScaleTiles,
Weapon, AvailableArenaIds, Tags
```

校验：生命、威胁、掉落和速度非负且符合职责；视觉尺度在该类别允许区间；场景与武器引用有效；普通敌人不能声明基地目标。

### 10.5 EliteModifierDefinition

```text
Id, DisplayName, Description, EffectId,
ThreatMultiplier, ArmorMultiplier, VisualScaleTiles,
TelegraphProfile, ConflictModifierIds
```

每个精英最多应用一个 modifier。效果必须在已登记的精英效果处理器中存在；视觉和预警资源不能缺失。

### 10.6 WaveDefinition

```text
WaveNumber, SpawnDurationSeconds,
ThreatPerSecondCurve, EnemyPool,
EntrancePressureLimit, MinimumPlayerDistance,
RewardKind, IncludesElite
```

每个竞技场恰有 5 波；波号 1–5 唯一；默认时长依次为 45、50、55、60、70；奖励依次为普通协议、维护、普通协议、维护、稀有协议；只有第 5 波 `IncludesElite = true`。

### 10.7 ArenaDefinition

```text
Id, DisplayName, ArenaIndex, Scene,
Waves, Boss, FullRepairAfterBoss,
AvailableEnemyIds, EliteModifierIds
```

竞技场索引 0–4；场景非空；恰有 5 个有效波次；Boss 引用存在；`FullRepairAfterBoss = true`。场景中的入口节点 Id 必须与内容定义一致。

### 10.8 BossDefinition

保留现有名称、总装甲、阶段阈值和场景引用，增加 `ArenaId`、`PhaseDefinitions`、`MajorRewardKind` 和 `ClearsTemporaryHazardsBeforeIntro`。Boss 1–4 必须提供重大成长；Boss 5 的奖励种类为空并进入通关结算。

### 10.9 ContentCatalog

目录集中引用核心、协议、辅助、敌人、精英、竞技场和 Boss。`Validate()` 必须检查：

- 所有稳定 Id 全局唯一且无首尾空白；
- 所有资源非空且引用可解析；
- 前置/冲突无自引用和循环；
- 三核心、首区 16 普通协议、4 稀有协议、4 辅助和每核心 3 个首次进化满足当前纵切内容合同；
- 不存在 `RelayRepair`、`RelayShield` 或基地目标语义；
- 第五竞技场不引入新的普通敌人 Id。

## 11. 玩家、武器与辅助系统

`PlayerTank` 保持自由移动、独立瞄准、按住连续开火和动力冲刺。核心差异通过 `BuildSnapshot` 与 `CoreDefinition` 注入，不在输入脚本中使用 `switch (protocolId)`。

主炮继续由 `WeaponController` 管理冷却和炮弹实例化。它读取伤害、冷却、弹速、穿透、反弹、分裂和爆炸等计算值。主炮永远由玩家瞄准；首版不提供自动主炮目标选择。

辅助系统统一实现：

```csharp
namespace Game1;

public interface IAuxiliaryRuntime
{
    string AuxiliaryId { get; }
    void Configure(AuxiliaryDefinition definition, BuildSnapshot build);
    void Activate(Godot.Node2D owner);
    void Deactivate();
}
```

辅助运行时由玩家的 `AuxiliaryHost` 节点持有，最多两个。构筑快照变化时重新配置；竞技场卸载或本局结束时调用 `Deactivate()` 并解除事件订阅。

## 12. 受伤与重启

`RunState` 是跨场景装甲真值；`HealthComponent` 是当前玩家实例的战斗适配器。进入竞技场时从 `RunState` 初始化；每次 `ValueChanged` 把当前值同步回 `RunState`，不能出现两边各自独立扣血。

`RebootController` 新流程：

1. 监听玩家 `HealthComponent.Depleted`；
2. 请求 `RunController` 消耗一次重启；
3. 没有重启则触发本局失败；
4. 有重启则记录当前坐标，禁用玩家移动、射击和碰撞；
5. 在原地播放 1.2 秒核心重构动画，期间不可受伤；
6. 恢复向上取整的 50% 最大装甲；
7. 释放无伤害击退脉冲，只推动脉冲范围内的普通敌人，不推动 Boss、不改变地形；
8. 恢复碰撞和输入，授予 2 秒保护；
9. 上报 `RebootCompleted`。

重启不移动玩家、不清除整场敌人或炮弹、不重置波次和 Boss、不恢复重启次数。击败 Boss 后只把装甲恢复为最大值。

## 13. 敌军目标与导航

迁移后所有普通敌人和 Boss 的战斗目标都是玩家或玩家制造的临时装置。删除 `TargetId.Relay`、`TargetSnapshot.RelayAvailable` 和“首选基地、失败后回退玩家”的策略。

敌军行为职责仍由数据和小型策略组合，不在一个巨型 `EnemyTank` 中无限增加条件。首区建议拆为共享移动/攻击适配器加四个职责策略：侦察追踪、巡逻直射、突击包抄、迫击区域预警。

导航继续使用 `NavigationGrid` / `IEnemyPathProvider`。地形破坏后局部刷新；无路径时敌人停止并按退避间隔重试，不能直线穿墙。入口安全判断与寻路是独立条件：入口可生成不代表路径必然有效，导演必须过滤或回退到另一入口。

## 14. 确定性随机

所有随机流由以下稳定输入派生：

```text
RunSeed | StreamName | ArenaIndex | WaveIndex | OfferOrdinal
```

`StreamName` 至少区分 `wave_spawn`、`combat_drop`、`stat_offer`、`protocol_offer`、`maintenance_offer` 和 `boss_offer`。使用稳定哈希，不使用进程随机化的 `string.GetHashCode()`。

相同种子、内容目录版本和选择历史必须生成相同波次请求与奖励候选。玩家实时位置只影响入口安全过滤，不改变候选敌军池的随机序列；过滤后按稳定顺序选择可用入口。

## 15. HUD 与验收入口

`CombatHudController` 订阅只读状态：

- 左上：当前/最大装甲、重启次数、核心；
- 上中：波次剩余秒数、精英/Boss 状态；
- 右上：竞技场 1–5、波次 1–5；
- 下方：经验进度、冲刺冷却、两个辅助槽；
- 不显示中继站、基地耐久或第二条生命值。

`AcceptanceMenu` 必须有可见按钮入口，不依赖 F1。仅 Debug 构建显示，可选择核心、竞技场和波次，并触发升级、维护、精英、Boss 和重启。它只能调用正式控制器公开命令，不能直接修改私有字段或创建第二套规则。

## 16. 存档迁移

现有 schema 版本 1 升级为版本 2：

```csharp
public sealed class LastRunSummary
{
    public int Seed { get; set; }
    public string CoreId { get; set; } = string.Empty;
    public int ArenaIndex { get; set; }
    public int WaveIndex { get; set; }
    public int Level { get; set; }
    public double ElapsedSeconds { get; set; }
    public string Result { get; set; } = string.Empty;
}
```

迁移规则：

- 保留音量设置和 `UnlockedIds`；
- 读取 schema 1 时忽略旧 `RelayIntegrity`，用空核心、0 区、0 波、1 级建立最近一局摘要；
- 不把旧协议 Id 自动映射成新语义；找不到的历史解锁 Id 保留在未知列表或忽略并记录警告，不能导致启动失败；
- 继续采用临时文件写入、可重读校验和原子替换；
- 当前阶段不保存战斗中途进度。

## 17. 错误处理

- 固定子节点用 `GetNode<T>` 尽早失败；可选验收节点用 `GetNodeOrNull<T>`；
- 内容资源在进入运行流程前统一 `Validate()`；无效稳定 Id、缺失引用、非法数值和循环前置必须阻止本局开始；
- 奖励池不足三项属于内容错误，不允许复制候选或静默降为二选一；
- 无安全入口时允许选最远入口并延长预警，但不能场内随机生成；
- 运行对象释放前解除长生命周期事件订阅；
- 可恢复的存档错误记录警告并回退默认数据；生产内容错误使用 `GD.PushError` 并阻止进入战斗。

## 18. 测试架构

### 18.1 纯 C# 测试

位于 `tests/Game1.Tests/`：

- `RunState` 装甲、重启、竞技场/波次边界；
- `ArenaController` 五波和 Boss 状态迁移；
- `ExperienceCurve`、连续升级队列和经验回收顺序；
- 受控属性候选过滤、确定性和安全上限；
- 三阶协议、满级退出、两个辅助槽；
- 30% 以下维护必含修复，等于 30% 不强制；
- schema 1 → 2 存档迁移；
- 固定种子派生流可复现。

### 18.2 Godot headless 测试

位于 `game1/tests/headless/`：

- 场景树不存在有效 `RelayStation` 节点或中继 HUD；
- 暂停时玩家、敌人、炮弹、Boss 和计时器全部冻结，UI 可输入；
- 连续升级后只在全部完成时恢复；
- 四周入口预警、玩家安全距离和导航可达；
- 第 5 波精英未死不能结算；
- 原地重启坐标不变、50% 装甲、击退脉冲和 2 秒保护；
- Boss 前不回血，Boss 后全修且重启不恢复；
- 资源目录数量、稳定 Id 和引用完整。

### 18.3 实际 Godot 验收

每个迭代提供“操作 → 预期画面 / 数值 / 状态变化”脚本。自动化不能替代 OpenGL Compatibility 实际启动、HUD 可读性、输入手感、敌军预警和 Boss 输出窗口验收。

完整 30 敌人、160 炮弹和持续效果压力测试只在 Alpha 08 执行。此前只做当前迭代必要的启动冒烟和局部数量检查。

## 19. 禁止实现方式

- 禁止让 `AppRoot` 同时实现运行、竞技场、波次、奖励、重启和 Boss 规则；
- 禁止在 `PlayerTank` 根据协议 Id 分支；
- 禁止运行时修改共享 `Resource`；
- 禁止隐藏 `RelayStation` 节点或把中继耐久固定为 0 来伪装移除；
- 禁止保留 `RelayIntegrity` 作为第二生命线；它只允许出现在历史迁移说明和 schema 1 读取代码中；
- 禁止复用旧中继协议 Id 承载新的玩家效果；
- 禁止在入口不安全时直接生成到场内随机空地；
- 禁止每个私有方法完成后运行全量回归；
- 禁止在无分析证据时引入 ECS、通用对象池或依赖注入；
- 禁止新增外部插件或改变引擎/.NET 版本而不先获得用户确认。

## 20. 技术完成定义

移动核心首区纵切达到技术完成必须同时满足：

1. 生产运行时、HUD、敌军目标和存档摘要没有独立中继站语义；
2. `RunController`、`ArenaController`、`WaveDirector`、`BuildController` 和奖励/升级职责分离；
3. 五波、即时升级、协议维护、精英、Boss、全修和失败可完成闭环；
4. 三核心、16 普通协议、4 稀有协议、4 辅助和 9 个首次进化通过目录校验；
5. 固定种子可复现波次与奖励；
6. 纯 C#、Godot headless、构建、启动和策划可见验收全部通过；
7. 未经确认的依赖、素材、架构或范围偏离为零。

满足上述条款并通过用户验收后，才能进入后续竞技场内容扩展。
