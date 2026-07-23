# 《废土中继》移动核心竞技场重构总实施路线图

> **执行约束：** 本项目使用单主智能体模式。每个正式迭代在开始前另写可执行的详细计划，并由主智能体按 `executing-plans` 逐项实施；默认禁止子智能体。

**目标：** 在保留现有 Godot 坦克移动、射击、弹道、地形、导航与 Boss 基础的前提下，把固定中继站房间防守原型迁移为五竞技场、即时升级、多核心构筑的坦克动作肉鸽。

**架构：** 新增 `ArenaController` 与时间/威胁预算驱动的 `WaveDirector`，由 `RunController` 统一持有跨竞技场状态；构筑、奖励、敌军内容继续数据驱动。旧 `RelayStation` 及所有基地状态被完整移除，不以隐藏节点或兼容字段保留。

**技术栈：** Godot 4.7 .NET、C#、游戏程序集 `net8.0`、独立测试项目 `net10.0`、Godot `Resource` / `.tres`、OpenGL Compatibility、480×270 逻辑画布。

## 全局约束

- 当前玩法权威规格为 `docs/superpowers/specs/2026-07-20-mobile-core-arena-roguelite-redesign.md`。
- 新技术架构必须先写入并通过用户审阅，之后才能修改生产代码。
- 不引入完整肉鸽模板；新插件、依赖、素材下载或购买必须单独确认。
- 不新开 Godot 工程，不为每次迭代创建需要用户重新打开的项目副本。
- 所有运行时数值与内容通过明确类型和只读资源配置，不把竞技场坐标、奖励结果或构筑效果硬编码进 `PlayerTank` / `AppRoot`。
- 同一迭代内先完成可玩闭环，再集中执行构建、相关自动化、Godot 启动和策划可见自检。
- 完整敌军/炮弹压力测试只在全部功能迭代完成后的 Alpha 08 执行。
- 每个迭代由用户明确验收通过后才能提交和推送 `main`；计划步骤中的提交动作必须等待该门禁。
- `.superpowers/` 是本地证据目录，不得暂存或提交。
- 当前工作区已有 Alpha 01A 生产代码和早期生成素材差异；所有迭代必须保存初始状态清单并使用精确路径暂存，不能用 `git add .` 混入旧改动。
- **交付节奏（2026-07-23 用户确认）：** Alpha 02F、02G、02H 是 Alpha 02 的内部连续开发子项；主智能体不得在其间要求用户验收、提交或推送。仅在首区“五波 + 精英 + Boss + 全修 + 下一竞技场占位”闭环完成且集中自检通过后，统一交付 Alpha 02 验收。后续每个大迭代同样只设置一次用户验收门禁。

---

## 1. 文档处理策略

这次变更属于核心玩法与运行时架构的实质重构，采用“新建当前权威文档 + 保留历史正文并加替代声明 + 更新项目状态文档”的方式。

### 1.1 新建当前权威文档

- 已建立策划规格：`docs/superpowers/specs/2026-07-20-mobile-core-arena-roguelite-redesign.md`。
- Alpha 02A 新建技术规格：`docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md`。
- 本文作为总路线图；每个正式迭代另建形如 `docs/superpowers/plans/2026-07-20-alpha-02b-mobile-core-foundation.md` 的详细计划。
- 每个迭代另建形如 `docs/iterations/iteration-alpha-02b-mobile-core-foundation.md` 的执行记录。

### 1.2 保留并标记为历史基线

以下文档不删除正文，只在顶部加入清晰的“历史 MVP 基线 / 当前已被替代”声明，并链接新策划、新技术和本路线图：

- `docs/superpowers/specs/2026-07-15-roguelite-tank-design.md`
- `docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md`
- `docs/superpowers/plans/2026-07-15-roguelite-tank-mvp.md`
- `docs/superpowers/specs/2026-07-19-alpha-01a-battlefield-composition-design.md`
- `docs/superpowers/plans/2026-07-19-alpha-01a-battlefield-composition.md`
- `docs/iterations/iteration-alpha-01a-battlefield-composition.md`

Alpha 01A 的玩家比例、竞技场边界等已经验证的技术结果仍可复用，但“底部基地、上方来敌”不再是当前玩法规则。

### 1.3 原位更新当前状态文档

- `README.md`
- `game1/README.md`
- `asset_sources/README.md`
- `asset_sources/AI_PROTOTYPE_ASSETS.md`
- `asset_sources/THIRD_PARTY_ASSETS.md`

素材登记保留旧中继站文件的来源证据，但把其状态改为“弃用历史原型，不进入游戏、不作为生成参考”；不能通过删除登记掩盖历史来源。

新比例基线通过且 Alpha 02B 不再依赖旧素材后，删除运行资产和源文件区中的中继站图片、提示词及含独立中继站的旧比例图；登记表只保留文件曾经存在、来源和删除原因的文字证据。最终素材目录不得保留可被重新投入游戏的中继站图像文件。

---

## 2. 迭代总览

| 迭代 | 玩家可见结果 | 主要风险 | 用户验收焦点 |
|---|---|---|---|
| Alpha 02A | 权威文档统一、无中继站真实比例图、首区素材规格 | 文档分叉、比例仍不适合玩法 | 规则无冲突；真实游戏画面比例成立 |
| Alpha 02B | 中继站彻底移除，玩家成为唯一失败目标，原地重启可见 | 旧状态/场景/存档残留 | HUD、装甲、重启、失败全过程可验收 |
| Alpha 02C | 单竞技场五个限时波次和第 5 波精英槽位 | 双状态机冲突、计时/清场边界 | 45/50/55/60/70 秒流程与清场正确 |
| Alpha 02D | 战斗数据、即时完全暂停升级、波末自动回收 | 暂停语义、连续升级队列 | 敌弹冻结、连升、恢复保护和回收顺序 |
| Alpha 02E | 三核心、受控属性、协议三阶、维护和奖励层级 | 奖励死选项、状态职责越界 | 三核心节奏、Mk.I–III、30% 修理保障 |
| Alpha 02F | 四种自动辅助系统和两个槽位 | 目标选择、重复升级、满槽池过滤 | 主炮手动 + 最多两套自动辅助闭环 |
| Alpha 02G | 城区四种敌人、四周增援和一个精英变体 | 导航、贴脸生成、职责不可读 | 四周警告、组合压力和精英单规则 |
| Alpha 02H | 路障指挥车适配、五波 + Boss + 换区占位纵切 | 旧基地 Boss 逻辑、地形继承 | 第一竞技场完整闭环与全修装甲 |
| Alpha 03 | 废弃工厂、三种新敌人、熔炉装甲列车 | 地雷/自爆/轨道机制互相干扰 | 路线管理、冷却车厢 Boss |
| Alpha 04 | 干涸水库、三种新敌人、双联重炮平台 | 远程交叉火力公平性 | 预警、换位、通道变化 |
| Alpha 05 | 军阀要塞、三种支援敌人、旗舰坦克 | 支援编队优先级不清 | 护盾、维修、指挥链和追猎阶段 |
| Alpha 06 | 移动堡垒竞技场、履带战争城塞、完整五区单局 | 终局时长、机制堆叠 | 不新增普通规则的综合考试 |
| Alpha 07 | 横向解锁、完整素材/音频、手感和 UI 统一 | 内容量膨胀、授权遗漏 | 长期目标、反馈品质和授权清单 |
| Alpha 08 | 全局平衡、稳定性、压力与性能收尾 | 30 敌人/160 炮弹帧率 | 完整回归、60 FPS、最终验收包 |

---

## 3. 目标运行时文件结构

现有目录结构继续使用；只按职责新增文件，不建立第二套平行游戏框架。

### 3.1 新增核心文件

```text
game1/scripts/run/
  RunPhase.cs
  RunState.cs                 # 改造现有文件
  RunController.cs            # 改造现有文件
  ArenaState.cs
  ArenaController.cs
  RewardController.cs
  RewardKind.cs
  RewardOffer.cs
  ExperienceCurve.cs
  LevelUpController.cs
  ControlledStatOfferGenerator.cs
  CoreId.cs
  ProtocolRank.cs
  AuxiliarySlotState.cs

game1/scripts/combat/
  WaveDirector.cs
  WaveDirectorState.cs
  SpawnEntrance.cs
  CombatDataPickup.cs
  CombatDataCollector.cs
  RebootController.cs         # 改造现有文件

game1/scripts/content/
  CoreDefinition.cs
  ArenaDefinition.cs
  WaveDefinition.cs
  EnemyDefinition.cs
  EliteModifierDefinition.cs
  AuxiliaryDefinition.cs
  ProtocolDefinition.cs       # 改造现有文件
  ContentCatalog.cs           # 改造现有文件

game1/scripts/ui/
  CombatHudController.cs
  StatLevelUpPanel.cs
  RewardPanel.cs              # 改造现有文件
  AcceptanceMenu.cs
```

### 3.2 废止目标

完成 Alpha 02B 后，以下生产职责不得继续存在：

- `scripts/combat/RelayStation.cs`
- `RunState.RelayIntegrity`
- `RunController.OnRelayDestroyed()`
- `BuildController.RelayDamaged`
- `StatId.RelayRepair`
- `StatId.RelayShield`
- `TargetId.Relay` 及敌军基地目标回退；
- `AppRoot` 中继站节点绑定和 `UI/Hud/RelayLabel`；
- 运行中的 `resources/protocols/engineering_shield.tres` 与 `logistics_repair.tres` 旧语义。

旧资源文件可以在同一迭代中删除或以全新玩家坦克协议替换，但新协议必须使用新的稳定 Id，不能让旧存档 Id 获得不同语义。

### 3.3 模块接口边界

详细签名在 Alpha 02A 技术设计中冻结，最低边界如下：

```csharp
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
}

public sealed class ArenaController
{
    public ArenaState State { get; }
    public int CurrentWave { get; }
    public event Action<ArenaState> StateChanged;
    public void BeginArena(ArenaDefinition definition);
    public void OnWaveSpawnWindowEnded();
    public void OnAllEnemiesCleared();
    public void ConfirmReward(string rewardId);
    public void OnBossDefeated();
}

public partial class WaveDirector : Node
{
    public void Configure(WaveDefinition definition, IReadOnlyList<SpawnEntrance> entrances, int runSeed);
    public void StartWave();
    public void StopSpawning();
    public event Action SpawnWindowEnded;
    public event Action AllEnemiesCleared;
}
```

`AppRoot` 只负责组合场景与控制器、转发顶层事实和切换界面；不得再次成长为包含波次、奖励、重启、存档和 Boss 全部规则的单体脚本。

---

## 4. Alpha 02A：文档与视觉基线

详细执行计划：`docs/superpowers/plans/2026-07-20-alpha-02a-document-and-visual-baseline.md`。

交付物：

1. 新移动核心技术设计；
2. 旧权威文档的历史/替代声明；
3. README 与素材登记同步；
4. 新素材生产清单与尺寸规范；
5. 无中继站的真实游戏比例图；
6. 用户对比例、HUD、单位密度和卡片占屏的确认。

Alpha 02A 不修改生产代码，不删除现有中继站运行资源，不下载或购买第三方素材。

---

## 5. Alpha 02B：移动核心生存基础

**目标：** 玩家坦克成为唯一战斗生命线，旧中继系统从生产运行时、HUD、存档摘要、敌军目标和测试中完整移除。

**主要修改：**

- 改造 `RunState`、`RunController`、`AppRoot`、`RebootController`、`EnemyTank`、`TargetPolicy`、`RunResultSnapshot`、`SaveData` 与主场景 HUD；
- 删除 `RelayStation` 场景节点和生产脚本；
- 删除 `game1/assets/` 和 `asset_sources/ai_generated/` 中独立中继站图片，以及包含独立中继站的旧比例图；登记文字继续保留；
- 将 `HealthComponent` 的初始装甲、最大装甲和当前装甲与 `RunState` 显式同步；
- 重启改为原地 1.2 秒、恢复 50%、无伤击退脉冲、2 秒保护；
- 新建可视化策划验收菜单，替代依赖快捷键的隐藏路径。

**测试边界：**

- 纯 C#：默认 1 次重启、计数不为负、无重启失败、Boss 后全修接口；
- headless：场景无 `RelayStation`，原地重启不移动坐标，敌军只选择玩家；
- 实机：HUD 无中继耐久，报废、重启、保护和失败全程可见。

**禁止：** 本迭代不实现五波计时、经验升级、核心选择或新敌人。

---

## 6. Alpha 02C：五波竞技场状态机

**目标：** 在一张城区竞技场内运行五个阶梯时长波次，并用独立 `ArenaController` / `WaveDirector` 取代旧房间清场推进。

**空间基线：** Alpha 02C 同时把旧通道式房间替换为极稀疏竞技场灰盒：75%～85% 连续开阔区域、3～5 组小型障碍岛、主要通路至少三台玩家坦克宽。障碍只提供短时破墙、弹射和躲避选择，不得成为持续刷怪节奏的寻路税。

**接口：**

```csharp
public enum ArenaState
{
    Loading, Intro, WaveCombat, Cleanup, Reward, BossIntro, BossCombat, Completed, Failed
}

public readonly record struct WaveSchedule(
    int WaveNumber,
    double SpawnDurationSeconds,
    RewardKind RewardKind,
    bool IncludesElite);
```

五波默认配置固定为 `45/50/55/60/70` 秒，奖励种类为协议/维护/协议/维护/稀有协议，第 5 波 `IncludesElite = true`。

**测试边界：** 时间结束只停止生成；残敌清空后才结算；第 5 波精英未死不得进入奖励；奖励确认后推进下一波；第 5 波奖励后进入 Boss 占位状态。

**禁止：** 不在 `AppRoot._Process()` 中复制一套波次判断；不通过直接 `QueueFree()` 残敌跳过清场。

---

## 7. Alpha 02D：战斗数据与完全暂停升级

**目标：** 敌军掉落数据、玩家靠近吸收、战斗内完全暂停三选一、波末自动回收和连续升级队列形成闭环。

**核心接口：**

```csharp
public sealed class ExperienceCurve
{
    public int GetRequiredExperience(int level);
}

public sealed class LevelUpController
{
    public int PendingLevelUps { get; }
    public event Action<StatOffer> OfferReady;
    public event Action QueueCompleted;
    public void AddExperience(int amount);
    public void Choose(StatId stat);
}

public sealed record StatOffer(IReadOnlyList<StatId> Options);
```

UI 打开时暂停场景树，战斗世界和计时器冻结，UI 使用 `WhenPaused` 处理；最后一项选择完成后恢复并授予约 0.4 秒保护。波末先回收数据，再处理全部升级，最后才发放波间奖励。

**测试边界：** 无重复选项、无死属性、连升队列顺序、暂停前炮弹位置不变、恢复后炮弹继续、波末经验不丢失。

---

## 8. Alpha 02E：三核心与协议奖励

**目标：** 实现三个核心、受控属性权重、三阶协议、维护与 Boss 1 首次进化的数据结构和 UI。

**内容规模：** 3 核心、16 普通协议、4 稀有协议、每核心 3 个 Boss 1 进化。

**核心接口：**

```csharp
public enum ProtocolRank { None = 0, MkI = 1, MkII = 2, MkIII = 3 }

public sealed record OwnedProtocol(string ProtocolId, ProtocolRank Rank);

public sealed record RewardOffer(
    RewardKind Kind,
    IReadOnlyList<RewardChoice> Choices);

public sealed record RewardChoice(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<string> Tags);
```

奖励生成必须确定性读取种子、竞技场、波次、当前核心、协议等级和构筑标签。维护在装甲低于 30% 时强制包含修复 25% 最大装甲；Mk.III 协议退出普通奖励池。

**测试边界：** 同种子可复现、三选一唯一、Mk.I–III 递进、满级过滤、核心软权重、跨部门变化项、30% 修理边界。

---

## 9. Alpha 02F：自动辅助系统

**目标：** 在手动主炮之外实现最多两个自动辅助槽，以及四种职责不同的辅助系统。

**接口：**

```csharp
public sealed record AuxiliarySlotState(string AuxiliaryId, int Rank);

public interface IAuxiliaryRuntime
{
    string AuxiliaryId { get; }
    void Configure(AuxiliaryDefinition definition, BuildSnapshot build);
    void Activate(Node2D owner);
    void Deactivate();
}
```

首批辅助职责为直接侧挂射击、环绕无人机、按移动距离布雷和区域压制。重复获得升级现有槽位；两个槽位满后，新辅助类型退出奖励池。

**测试边界：** 槽位 0/1/2、重复升级、满槽过滤、暂停冻结、房间切换释放订阅、辅助目标选择不读取中继或旧房间状态。

---

## 10. Alpha 02G：城区敌军与四周导演

**目标：** 完成侦察无人机、巡逻坦克、突击车、迫击炮车，以及一个精英附加规则；敌军从手工入口按预算从四周增援。

**接口：**

```csharp
public sealed record SpawnEntrance(
    string Id,
    Vector2 Position,
    Vector2 Facing,
    float WarningSeconds);

public sealed record SpawnRequest(
    string EnemyId,
    string EntranceId,
    string EliteModifierId);
```

导演根据玩家距离、入口占用、当前方向压力、波次池和确定性随机流生成请求。第 5 波只挂接一个精英规则，不组合多个精英词条。

**测试边界：** 不贴脸、入口预警先于实例化、四类职责、第五波精英阻塞结算、导航失败有安全回退但不穿墙。

---

## 11. Alpha 02H：首区 Boss 与完整纵切

**目标：** 把路障指挥车从基地冲锋改造为只针对玩家的拆障/冲撞/弱点 Boss，并完成五波、Boss、全修和下一竞技场占位过渡。

**复用：** `BossPhaseController`、`BarrierDeployment`、`BossGunEmplacement`、`BossSummonController` 和现有碰撞轴优先复用。

**必须移除：** Boss 对中继站目标、底部基地脆弱窗口和基地失败信号。

**测试边界：** 第 5 波残弹与临时危险区清理、地形破坏继承、装甲不在 Boss 前恢复、Boss 后全修、重启次数不恢复、Boss 1 进化后进入下一竞技场占位。

Alpha 02H 通过用户验收后，才认为新的竞技场肉鸽纵切成立，并允许后四区内容扩展。

---

## 12. Alpha 03–06：逐区内容扩展

每个竞技场遵守相同的内容迭代合同：

1. 先确认该区真实游戏比例和素材小样；
2. 增加该区地图资源、三个普通敌人（第五区为零）和一个 Boss；
3. 只增加该区需要的协议/进化，不无条件扩大公共系统；
4. 完成该区五波 + Boss 的策划验收；
5. 回归所有已完成竞技场；
6. 用户验收通过后提交推送。

第五区只能使用既有普通敌人、精英变体和高压混合编队；不得为了制造新鲜感临时加入未在策划规格中确认的新普通行为。

---

## 13. Alpha 07：内容与表现收尾

- 横向解锁：核心、协议池、挑战词条、档案和外观；
- 最终 HUD、卡片、暂停、结算和策划验收界面；
- 逐项集成已确认的正式素材；
- 音频总线、机械移动、主炮、命中、预警、奖励和 Boss 音频；
- 局外存档 schema 迁移与损坏回退；
- 完整授权登记和发布素材审计。

本阶段不引入永久伤害、装甲或射速成长。

---

## 14. Alpha 08：全局验证与性能收尾

Alpha 08 才执行完整压力与性能测试：

- 约 30 个普通敌人；
- 约 160 枚活动炮弹；
- 持续伤害区、掉落物、辅助系统与 UI 同时工作；
- 1440×810 默认窗口、OpenGL Compatibility、稳定 60 FPS；
- 先使用 Godot Profiler 和 Monitors 定位瓶颈；
- 只有实例化、GC 或物理查询被证实为瓶颈后才引入对象池、分帧查询或更新频率调整。

同时执行完整五区通关、失败、重复开局、固定种子复现、存档损坏回退、输入焦点和窗口暂停回归。

---

## 15. 每个迭代的统一完成门禁

1. 本迭代执行记录中的目标、非目标、文件边界和验收脚本已经填写；
2. 迭代全部开发完成后，集中执行 `dotnet build`；
3. 运行全部相关纯 C# 测试；
4. 运行全部相关 Godot headless 套件；
5. Godot OpenGL Compatibility 启动并走通玩家可见路径；
6. 按“操作 → 预期画面 / 数值 / 状态变化”完成主智能体策划复验；
7. 检查架构合规矩阵、素材登记、工作区差异和偏离记录；
8. 向用户交付验收说明；
9. 用户明确验收通过后，才暂存、提交并推送 `main`。

除 Alpha 08 外，常规迭代不执行完整 30 敌人 / 160 炮弹压力流程。
