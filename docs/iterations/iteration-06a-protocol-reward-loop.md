# 迭代 06A：协议构筑与奖励循环

## 目标与非目标

目标：在现有 MVP 房间交付十项数据协议、ContentCatalog、BuildController、确定性三选一、统一属性管线、完整房间生命周期，以及奖励后的同布局第二战。

非目标：TileMapLayer、AStarGrid2D、RoomDefinition、不同布局房间、敌人/Boss 扩充、存档和音频。

## 权威文档版本

- `AGENTS.md`
- `docs/superpowers/specs/2026-07-15-roguelite-tank-design.md`
- `docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md`
- `docs/superpowers/specs/2026-07-17-iteration-06-protocol-room-loop-design.md`
- `docs/superpowers/plans/2026-07-17-iteration-06a-protocol-reward-loop.md`
- `docs/decisions/0001-prototype-architecture-deferrals.md`

## 策划验收标准

1. 相同局种子、房间索引、已选协议集合、目录版本，生成相同有序三选一；无重复、无冲突、无满层，并至少有一项不依赖当前流派的通用候选。
2. 第一房清场后可见 Cleared、清场修复反馈与 Reward；奖励卡支持鼠标和 `1/2/3` 选择。
3. 选择后构筑区即时显示；进入下一战后才影响坦克。反弹联动、电能履带联动和中继拦截均有可读反馈。
4. 选择后经过 Exiting，保留本局状态并重建相同 `mvp_combat_room`，用派生种子开始第二战；不得表述为第二张地图。
5. 仅 Combat 可进入 Failed；Intro 固定 0.6 秒，所有状态在 HUD/事件栏可辨认。

## 执行方式（2026-07-18 修订）

06A 属于重大任务，但其协议数据、构筑、状态机、UI 与资源存在连续依赖，当前没有两个以上无文件交叉的独立并行工作。因此由主智能体直接执行开发、测试、架构自检与策划可见复验；不启动子智能体。原角色分工仅保留为职责检查清单。

## 开发任务合同

### 必须实现

- `ProtocolDefinition`、`ProtocolEffectDefinition`、`ContentCatalog` Resource 和十项 `.tres`；ProtocolDefinition 必须有部门、稀有度、基础权重、标签、需求标签、冲突标签、效果和叠加上限。
- 十项固定内容：前线兵工局 5（额外反弹、反弹伤害+30%、首次命中分裂、射速提升但伤害降低、重炮弹但射速降低）；侦察电子组 2（电能冲刺履带、冲刺冷却-20%）；后勤维修署 2（清场修复装甲、清场修复中继站）；工程兵团 1（中继站拦截护盾）。
- `ContentCatalog` 校验唯一 Id、空引用、稀有度/权重/数值范围与可满足前置；`RewardGenerator` 按稳定 Id 排序，以确定性随机流按基础权重抽取，并基于完整输入排除冲突/前置不满足/满层。
- `BuildController` 独占当前 Run 的选择和 `ShotFired`、`ProjectileHit`、`DashStarted`、`RelayDamaged`、`RoomCleared` 订阅，并在结束时解除订阅。
- `StatPipeline` 使用“基础→固定→加法百分比→乘法百分比→最终钳制”；`10 + 2、+50%、×2` 结果为 36。
- `Loading → Intro(0.6秒) → Combat → Cleared → Reward → Exiting`，且仅 Combat 可 Failed；Cleared 先冻结生成、清危险炮弹、结算清场修复。
- 可见奖励面板/HUD 构筑区和同场景第二战。

### 必须保持不变

- 480×270 逻辑画布、1440×810 验收窗口、中继总血量、坦克总装甲及一次重启规则。
- 当前 `mvp_combat_room.tscn` 静态地形、`DefenseRoutePlanner`、敌人内容和既有战斗输入。

### 禁止实现方式

- 不得硬编码奖励 Id、协议效果、部门分配、内容顺序或在武器/冲刺/中继/HUD 中以协议 Id 分支改数值。
- 不得提前加入 TileMapLayer、AStarGrid2D、RoomDefinition 或不同布局房间。
- 不得以自动化测试替代 Godot 运行和可见策划验收。
- 单元测试只能调用真实生产类型和真实 Resource/ContentCatalog；禁止反射、动态代理、mock/stub/fake、测试专用生产接口或替换生产行为。
- 需要构造真实 Godot Resource 的目录、奖励、构筑测试必须由 `godot --headless` 的正式测试宿主执行；纯 C# 属性管线与状态机测试保留 NUnit。Headless 宿主以退出码和清晰失败日志报告，不得用崩溃替代红灯。

### 允许/禁止修改的文件

| 角色 | 允许写入 | 禁止写入 |
| --- | --- | --- |
| 开发智能体 | `game1/scripts/content/`、`game1/scripts/run/`、奖励 UI、玩家武器/冲刺、中继、房间/AppRoot、相关 `.tscn/.tres` | 导航、TileMap、敌人 AI、非本轮素材 |
| 测试智能体 | `tests/Game1.Tests/`、`game1/tests/headless/` 和测试证据 | 生产运行时代码、正式场景、资源 |
| 架构审查智能体 | 只读，审查报告草稿 | 所有运行时代码与资产 |
| 策划智能体 | 只读，验收记录草稿 | 代码、资源、权威规格 |

## 测试矩阵

| 层级 | 证据 | 通过条件 |
| --- | --- | --- |
| 单元 | RewardGeneratorTests、ContentCatalogTests | 完整输入可复现、已选顺序无关、目录版本参与、冲突/满层排除、通用候选、目录校验 |
| 单元 | StatPipelineTests、BuildControllerTests | 10+2、+50%、×2=36；钳制、本局作用域、结束后无协议/快照/订阅残留 |
| 单元 | RunControllerTests | 完整状态机、Intro 0.6 秒、非法迁移拒绝、仅 Combat 失败、选择仅推进一次 |
| 构建 | `dotnet build game1/Game1.csproj -c Release` | 0 warning、0 error |
| 编辑器 | `godot --headless --path game1 --editor --quit` | 无脚本、场景、资源解析错误 |
| Headless 运行时 | `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite reward_catalog` | 真实 Resource 测试退出码和失败日志正确 |
| 运行 | 固定种子三轮验收 | 满足五项策划验收标准 |

## 开发理解回执

开发智能体必须确认：只做 06A；完整协议架构不可缩减；奖励/数值不可硬编码；BuildController 独占订阅；第二战同布局；Intro 为 0.6 秒。为让 C# 红灯测试能编译，Task 1、2、3 都允许先建立仅含公开签名、方法体全部抛 `NotImplementedException` 的 API 骨架；随后必须运行真实行为红灯测试，才可实现功能。未回执不得写入。

## 架构合规矩阵

| 架构条款 | 实现文件/接口 | 验证证据 | 状态 |
|---|---|---|---|
| 数据驱动协议 | ProtocolDefinition、EffectDefinition、Catalog、`.tres` | 10 项目录测试 | 未开始 |
| 确定性奖励 | RewardGenerator | 固定完整输入测试 | 未开始 |
| 单一数值入口 | StatPipeline、BuildController 快照 | 管线/组件测试 | 未开始 |
| 生命周期集中管理 | RunController | 状态机测试和运行验证 | 未开始 |
| 不触碰 06B 导航迁移 | 变更审查 | diff 无 TileMap/A* 文件 | 未开始 |

## 偏离记录

用户于 2026-07-17 确认：以 06A 设计规格为准修订此前错误的协议分配/简化架构/Intro 时长；确认“无行为 API 骨架→可编译红灯测试→功能实现”的 TDD 编译支撑步骤；确认真实 Resource 测试改由 Godot Headless 宿主运行；并确认补回协议稀有度、基础权重与标签数据及其确定性加权抽取。除此之外无偏离。

## 门禁与进度

- [ ] 主智能体前置检查：复核十项内容、0.6 秒 Intro、测试矩阵与 API 骨架后的真实红灯测试。
- [ ] 主智能体实现规划：确认接口、文件范围、公共影响和最小修改方案。
- [ ] 开发。
- [ ] 架构合规。
- [ ] 测试后置。
- [ ] 策划复验。
- [ ] 主智能体自检。
- [ ] 用户验收。
- [ ] 提交/推送：仅用户明确通过后执行。

## 工作区与仓库状态

- 当前分支：`main`；用户验收前不得提交或推送。
- 不得暂存/提交 `.superpowers/` 本地证据。

## 自动化与运行验证

执行 NUnit、Headless Resource 测试、Release 构建、headless 编辑器检查；固定种子完成三轮“清场→修复→选协议→HUD→第二战→失败入口”验收。

## 策划复验、用户验收与提交

策划复验必须确认功能可见而非只存在代码中。全部内部门禁通过后交付操作脚本给用户；只有用户明确验收通过，主智能体才能检查差异、提交中文说明并推送 `main`。
