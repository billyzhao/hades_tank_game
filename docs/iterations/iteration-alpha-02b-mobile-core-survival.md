# Alpha 02B：移动核心生存基础

## 目标与非目标

目标：完整移除独立中继站运行语义，让玩家装甲与一次原地重启成为唯一生存闭环，并提供策划可见验收面板。

非目标：不实现 Alpha 02C 之后的五波、经验、核心、奖励、敌军扩展或新 Boss 内容。

## 权威文档版本

- 策划：`docs/superpowers/specs/2026-07-20-mobile-core-arena-roguelite-redesign.md`
- 技术：`docs/superpowers/specs/2026-07-20-mobile-core-arena-technical-design.md`
- 路线图：`docs/superpowers/plans/2026-07-20-mobile-core-arena-roadmap.md`
- 执行计划：`docs/superpowers/plans/2026-07-20-alpha-02b-mobile-core-survival.md`

## 策划验收标准

按执行计划“用户验收标准”七项执行；验收对象是 Godot 实际运行画面和状态变化，不是隐藏调试键或自动化日志。

## 开发任务合同

### 必须实现

- 玩家唯一失败目标、跨房间装甲真值、一次重启；
- 原地 1.2 秒、向上取整 50%、无伤击退、2 秒保护；
- 无中继场景/HUD/目标/协议/摘要；
- schema 1→2 迁移；
- Debug 可见策划验收面板。

### 必须保持不变

- 自由移动、独立炮塔、射击、冲刺、墙体和 Alpha 01A 单位比例；
- 当前旧房间与 Boss 仅作迁移容器，不提前实现 Alpha 02C～02H 内容；
- 用户验收前不提交、不推送本迭代。

### 禁止实现方式

- 不隐藏中继节点伪装删除；
- 不保留 `RelayIntegrity` 第二生命线；
- 不复用旧中继协议 Id；
- 不把重启规则塞入 `AppRoot`；
- 不用隐藏快捷键作为唯一验收入口；
- 不在每个私有方法后执行全量回归。

### 允许/禁止修改的文件

以执行计划的文件边界为准。

## 测试矩阵

| 层级 | 目标 | 状态 |
| --- | --- | --- |
| 纯 C# 定向红灯 | 装甲/重启、唯一目标、schema 迁移 | 已观察 6 项预期失败后实现 |
| 纯 C# 全量 | 所有领域与旧回归 | 37/37 通过 |
| Godot headless | 无中继场景、原地重启、保护、目标 | 5 套相关 suite 与集成套件通过 |
| 构建 | Godot C# 项目 0 错误 | 通过，0 警告/0 错误 |
| Godot 可见验收 | HUD、验收面板、重构与失败全过程 | 待用户按七项标准验收 |

## 开发理解回执

Alpha 02B 是运行时迁移迭代，不是新内容迭代。先写失败测试，再连续完成领域、场景、重启和 UI，最后一次性集中自检。模块共享同一状态契约，本轮由主智能体独立实现，不拆分并行修改。

## 架构合规矩阵

| 架构条款 | 实现文件/接口 | 验证证据 | 状态 |
|---|---|---|---|
| RunState 为跨场景装甲真值 | `SynchronizeArmor`、`RestoreAfterReboot`、`RestoreArmorForNextArena` | 纯 C# + headless | 通过 |
| 重启规则不进入 AppRoot | `RebootController.Configure` + `RunController.OnTankDefeated` | 代码审计 + headless | 通过 |
| 敌军/Boss 只攻击玩家 | `TargetPolicy`、`EnemyTank`、`BossEncounterController` | 纯 C# + headless | 通过 |
| 无 RelayStation/RelayIntegrity | 三张房间场景、生产脚本与资源删除 | `rg` + 场景测试 | 通过；仅 schema 1 迁移 DTO 保留旧字段名 |
| schema 1→2 保留设置/解锁 | `SaveService` legacy DTO 迁移 | 文件往返测试 | 通过 |
| 验收面板调用正式入口 | `AcceptanceMenu` 信号 → `AppRoot` 正式伤害/重开入口 | 场景测试 + 主场景启动 | 通过，交互待用户验收 |

## 偏离记录

- 2026-07-21 用户确认：竞技场改为 75%～85% 连续开阔、3～5 组小型障碍岛的“极稀疏战术竞技场”；已同步策划、路线图与素材计划，实际地图改造属于 Alpha 02C。
- 2026-07-21 用户验收发现：窗口失焦自动暂停后，重新聚焦仍需按 Esc，容易误判为坦克无法移动。根因是旧 `PauseController` 直接写 `SceneTree.Paused`，没有区分 `FocusLost` 与 `Manual`。已按技术设计提前落地 `PauseCoordinator`：聚焦只释放 `FocusLost`，不释放手动暂停。

## 门禁与进度

- [x] 策划前置
- [x] 测试前置
- [x] 开发理解
- [x] 开发
- [x] 架构合规
- [x] 测试后置
- [x] 策划复验
- [x] 主智能体自检
- [x] 用户验收
- [ ] 提交/推送

## 工作区与仓库状态

- 当前分支：`main`
- Alpha 02A：本地提交 `40684d9`，推送因当前无法连接 GitHub 443 端口而等待重试
- Alpha 02B：未暂存、未提交
- 执行前排除项：`.superpowers/`、未确认 `batch-01-units/`

## 自动化与运行验证

- `dotnet build GodotTank.sln --no-restore`：通过，0 警告/0 错误；
- `dotnet test GodotTank.sln --no-restore`：37/37 通过；
- Godot `reward_catalog`、`navigation_grid`、`boss_phase`、`boss_encounter`：全部通过；
- Godot `mobile_core_survival`：三房无中继、原地 50% 重启、2 秒保护、无伤击退、炮弹保留全部通过；
- Godot `mobile_core_survival`：追加失焦/聚焦自动恢复与手动暂停保留回归，通过；
- Godot `mvp_test_runner`：旧相关集成与压力基线全部通过；
- 主场景 OpenGL Compatibility headless 运行 180 帧：退出码 0，无运行时错误。

## 策划复验

实现未引入五波、经验、核心选择、新敌人或其他 Alpha 02C+ 内容；玩家装甲已经成为唯一生命线，验收入口可见且不依赖冲突快捷键。自动化复验和用户实际窗口验收均已通过。

## 用户验收

2026-07-21 用户明确确认“验收通过”，并批准 Batch 1 玩家坦克核心状态第二版作为后续素材基准。Alpha 02B 玩法、暂停恢复体验和单张视觉门禁完成。

## 提交与推送

用户已完成验收，但尚未明确要求本次提交或推送，因此继续保持未暂存、未提交状态。
