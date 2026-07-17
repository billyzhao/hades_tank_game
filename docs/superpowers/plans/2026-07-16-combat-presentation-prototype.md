# Combat Presentation Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将《废土中继》现有灰盒战斗升级为可验收的黄沙废土像素街机表现，同时不改变任何战斗规则。

**Architecture:** 规则节点只发出 Fired、Impacted、Destroyed、ValueChanged 等事实信号；新的表现节点订阅信号，负责精灵切换、特效、镜头与音频。所有表现配置放入可替换资源，物理和伤害状态保持在现有 C# 逻辑中。

**Tech Stack:** Godot 4.7 .NET、C#、Compatibility renderer、NUnit 4.6.1、项目内像素 PNG/程序化占位资源。

## Global Constraints

- 逻辑画布固定为 480×270，窗口保持 1440×810，纹理关闭过滤与 mipmap。
- 表现层不得调用 `IDamageable.ApplyDamage`、不得修改 `RunState`、不得改变现有碰撞层或输入映射。
- 任何新增纯逻辑方法必须先有 NUnit 红灯测试；视觉节点以 Godot 运行烟测和可见验收脚本验证。
- 不购买或下载未获用户确认的资源；来源与用途必须登记。
- 每个任务完成后运行单测、Debug/Release 构建、Godot headless 场景解析和运行烟测；用户验收前不提交、不推送。

## 2026-07-17 基线状态

混合表现专项已完成用户可见验收。Task 1 与 Task 2 已完成；Task 3 除可替换音频引用资源化外已完成；Task 4 的连续三波烟测、策划验收脚本和最终验证已完成，但空资源降级与生命周期清理尚缺独立回归证据。未完成项继续保留，不能由整体验收反推为已完成。

---

### Task 1: 资源目录、像素视觉基线与场景替换

**Files:**
- Create: `game1/assets/art/actors/`, `game1/assets/art/tiles/`, `game1/assets/art/props/`, `game1/assets/vfx/`, `game1/assets/audio/`
- Create: `game1/assets/README.md`
- Modify: `game1/scenes/actors/player_tank.tscn`, `game1/scenes/actors/enemy_tank.tscn`, `game1/scenes/rooms/mvp_combat_room.tscn`, `game1/scenes/app/main.tscn`
- Modify: `asset_sources/THIRD_PARTY_ASSETS.md`

**Produces:** 重型坦克、三类敌军、中继站、沙漠工业地面与军工 HUD 的统一像素基线；所有现有节点路径和碰撞形状不变。

- [x] **Step 1: 建立资源说明和来源登记模板**

在 `game1/assets/README.md` 写明目录用途、32×32/24×24/32×40 尺寸约束、无过滤导入要求和“资源可替换、逻辑不引用文件名以外语义”的规则；在 `asset_sources/THIRD_PARTY_ASSETS.md` 为每项资源登记来源、许可、用途、是否修改。

- [x] **Step 2: 制作并导入核心像素原型资源**

创建玩家车体、炮塔、三类敌军、中继站、沙地、砖墙、钢墙和 2–3 个工业摆件。敌军颜色固定为黄/橙/紫；砖墙红褐、钢墙蓝灰、中继站绿色。为每张 PNG 设置 nearest 采样与禁用 mipmap。

- [x] **Step 3: 只替换视觉子节点**

保留 `PlayerTank`、`EnemyTank`、`RelayStation`、`BrickWall` 和 `SteelWall` 根节点、脚本、组、碰撞层、碰撞形状及子节点名称。将其 `Polygon2D` 占位视觉替换为精灵/分层精灵，确保 `Turret/Muzzle` 仍在炮口位置。

- [x] **Step 4: 验证视觉基线**

运行：`dotnet test GodotTank.sln --nologo`、`dotnet build game1/Game1.csproj -c Release --nologo`、`dotnet build game1/Game1.csproj --nologo`、`godot --headless --path game1 --editor --quit`。

人工验收：启动后静止观察。预期：可一眼认出重型主角坦克、黄/橙/紫三类敌军、绿色中继站、砖墙和钢墙，且玩家仍能移动、瞄准、开火、冲刺。

### Task 2: 规则事实信号与战斗动态反馈

**Files:**
- Create: `game1/scripts/presentation/CameraShakeState.cs`, `game1/scripts/presentation/CameraShakeController.cs`, `game1/scripts/presentation/VisualFeedbackController.cs`, `game1/scenes/presentation/impact_vfx.tscn`, `game1/scenes/presentation/explosion_vfx.tscn`
- Create: `tests/Game1.Tests/Presentation/CameraShakeStateTests.cs`
- Modify: `game1/scripts/combat/WeaponController.cs`, `game1/scripts/combat/Projectile.cs`, `game1/scripts/enemies/EnemyTank.cs`, `game1/scripts/combat/DestructibleTerrain.cs`, `game1/scripts/combat/RelayStation.cs`, `game1/scenes/app/main.tscn`

**Interfaces:**
- Produces: `CameraShakeState.Start(float strength, float seconds)`, `CameraShakeState.Advance(float delta) : Vector2`
- Produces: `WeaponController.Fired(Vector2 origin, Vector2 direction, int team)`
- Produces: `Projectile.Impacted(Vector2 position, bool destroyedTarget, bool reflected)`

- [x] **Step 1: 写镜头震动的失败测试**

在 `CameraShakeStateTests.cs` 测试 `Start(4f, 0.1f)` 后第一次 `Advance(0.02f)` 的偏移长度不大于 4，连续 `Advance` 总计超过 0.1 秒后返回 `Vector2.Zero`。运行：`dotnet test tests/Game1.Tests/Game1.Tests.csproj --filter FullyQualifiedName~CameraShakeStateTests --nologo`。预期：因类型不存在而失败。

- [x] **Step 2: 实现最小纯状态类并转绿**

`CameraShakeState` 保存剩余时间和初始强度，`Advance` 以剩余时间比例衰减并返回确定性的二维偏移；不访问 Godot 节点。重跑同一命令，预期 2 个测试通过。

- [x] **Step 3: 新增事实信号但不改变结算顺序**

在 `WeaponController.TryFire` 成功实例化并初始化炮弹后发出 `Fired`；在 `Projectile` 已知命中点时发出 `Impacted`，再沿原有分支处理伤害/反弹/销毁；在敌军、砖墙和中继站的现有销毁或受击位置发出表现所需信号。信号回调不得写回伤害状态。

- [x] **Step 4: 实现表现订阅节点**

`VisualFeedbackController` 订阅现有节点和新增事实信号：开火生成 0.08 秒炮口火光与拖尾，钢墙命中生成火花，砖墙销毁生成碎片，敌军销毁生成爆炸，受击对象白闪后还原。`CameraShakeController` 只偏移渲染容器，结束时恢复精确原位。

- [x] **Step 5: 验证战斗动态**

运行全量单测、两种构建与 `godot --headless --path game1 --quit-after 8`。

人工验收：按住鼠标左键向钢墙、砖墙、敌军分别开火，再按空格冲刺。预期：每一次射击有火光/拖尾，钢墙有火花，砖墙有碎片，敌军有爆炸，冲刺有沙尘；HUD 数值与现有伤害结果保持不变。

### Task 3: 重量感、攻击预警、HUD 与最小音效

**Files:**
- Create: `game1/scripts/presentation/AudioFeedbackController.cs`, `game1/scripts/presentation/TankVisualAnimator.cs`
- Modify: `game1/scripts/player/PlayerTank.cs`, `game1/scripts/enemies/EnemyTank.cs`, `game1/scenes/actors/player_tank.tscn`, `game1/scenes/actors/enemy_tank.tscn`, `game1/scenes/app/main.tscn`

**Produces:** 履带运动、炮塔后坐、冲刺沙尘、敌方攻击预警、军工 HUD 与可关闭的最小音效集。

- [x] **Step 1: 为重量表现建立无规则依赖的接口**

`TankVisualAnimator.SetMotion(Vector2 velocity, bool isDashing)` 仅驱动车体帧、轻微摇晃与沙尘；`PlayRecoil(Vector2 direction)` 仅驱动炮塔视觉偏移。`PlayerTank` 在计算完既有 `Velocity` 后调用该接口，不改变 `MoveAndSlide` 前后的速度。

- [x] **Step 2: 接入敌军预警与中继站压力反馈**

保留敌军现有白闪攻击预警，并增加仅渲染的地面预警环；攻城炮车锁定中继站时预警为紫色。中继站 `ValueChanged` 时播放绿色能量波和受击声，耐久仍只由 `RunState` 写入。

- [ ] **Step 3: 接入音频控制器**

使用 `AudioStreamPlayer` 和独立 SFX bus，音量以 dB 导出属性配置。开炮、冲刺、钢墙命中、砖墙破裂、敌军爆炸和中继站受击各有一个可替换音频引用；引用为空时跳过播放而不报错。

> 2026-07-17 基线差距：当前使用运行时原型 SFX，最小音效反馈已通过用户验收，但尚未完成可替换音频引用资源化，因此本步骤保持未完成。

- [x] **Step 4: 调整 HUD 为废土军工面板**

保留装甲、中继站耐久、重启、敌军数、波次、房间状态和事件栏的文字层级与颜色语义。面板增加沙尘暗色、边角、轻微扫描线/噪点，不遮挡战场且不吞噬鼠标输入。

- [x] **Step 5: 验证重量和可读性**

人工验收：连续移动、改变鼠标瞄准、开火、冲刺、等待紫色攻城炮车攻击中继站。预期：车体与炮塔的运动独立可见；炮火/命中/预警有声音和画面反馈；紫色单位与绿色中继站的关系无需阅读 HUD 也能理解。

### Task 4: 性能、回归与策划验收包

**Files:**
- Modify: `game1/README.md`, `docs/superpowers/specs/2026-07-16-combat-presentation-prototype-design.md`
- Create: `docs/acceptance/2026-07-16-combat-presentation-acceptance.md`

**Produces:** 可重复执行的验收脚本与资源/性能说明。

- [ ] **Step 1: 添加资源清理与空资源回归检查**

销毁炮弹、敌军、砖墙和房间时，检查表现节点和音频节点随父节点销毁；逐项临时清空音频/特效引用，确保战斗继续且控制台无异常。

> 2026-07-17 基线差距：空资源降级和生命周期清理尚缺独立回归证据，因此本步骤保持未完成。

- [x] **Step 2: 连续三波性能烟测**

在 Godot 运行时连续完成三波，观察调试器错误与帧时间。验收目标为普通战斗期间无持续帧率下降、无不断增长的子节点、无重复信号报错。

- [x] **Step 3: 编写策划验收脚本**

文档必须按“操作 → 预期画面/数值/状态变化”列出：静止识别、移动与瞄准、开火与反弹、砖墙破坏、三类敌军、攻城压力、重启、清场、音频关闭与资源缺失降级。

- [x] **Step 4: 最终验证**

运行：`dotnet test GodotTank.sln --nologo`、`dotnet build game1/Game1.csproj -c Release --nologo`、`dotnet build game1/Game1.csproj --nologo`、`godot --headless --path game1 --editor --quit`、`godot --headless --path game1 --quit-after 8`、`git diff --check`。

预期：全部命令成功；用户验收前工作区不提交、不推送。

## 验收与素材登记

- 实际用户可见验收脚本：[混合表现迭代验收脚本](../../acceptance/2026-07-16-combat-presentation-acceptance.md)
- AI 原型素材来源、处理和使用边界：[AI_PROTOTYPE_ASSETS](../../../asset_sources/AI_PROTOTYPE_ASSETS.md)
