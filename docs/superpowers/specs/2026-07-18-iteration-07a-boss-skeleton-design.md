# 《废土中继》迭代 07A：路障指挥车 Boss 骨架设计

## 1. 目标

在不扩大 Boss 攻击内容的前提下，建立可验证的路障指挥车 Boss 房间、独立生命条和两阶段状态机，为 07B 的路障部署、冲锋、召唤与胜利结算提供稳定接口。

## 2. 已确认范围

- Boss 使用独立的 `RoadblockCommander` 场景和 `BossDefinition` 只读资源；运行时不得修改 Resource。
- Boss 拥有独立生命值和 HUD 血条，不占用、不重定义玩家装甲或中继站耐久。
- 阶段规则固定：生命值大于最大值的 50% 时为 `PhaseOne`；首次降至或低于 50% 时切换为 `PhaseTwo`；生命值归零时进入 `Defeated`，之后不可回到任何战斗阶段。
- `PhaseChanged` 只在 `PhaseOne → PhaseTwo` 时发送一次；`Defeated` 只发送一次。
- 阶段切换必须可见：Boss 血条颜色与阶段文字变化，Boss 本体短暂预警闪烁。
- 07A 允许提供仅用于验收的 Boss 房间入口或调试按钮，但正常战斗、协议奖励、双生命线与战场重启的语义不得改变。

## 3. 明确不在 07A 实现

- 不部署钢墙、机枪哨或其他路障。
- 不召唤普通敌人，不新增普通敌军种类。
- 不执行撞毁砖墙、冲向中继站、钢墙打断或脆弱窗口。
- 不接入 Boss 胜利结算、重试或返回主菜单。
- 不引入新付费或 AI 生成素材；继续使用当前已批准的像素原型资源与程序化 UI。

这些内容全部归入后续 07B，且不得借由 07A 的占位实现提前改变玩法。

## 4. 架构与数据流

```text
BossDefinition (静态配置)
        ↓
RoadblockCommander (受击实体，持有运行时 BossHealth)
        ↓ ReportHealth(current, maximum)
BossPhaseController (唯一阶段裁决者)
        ↓ PhaseChanged / Defeated
BossHudController (独立显示，不写入 RunState)
```

- `BossPhaseController` 是纯 C# 状态机，只接收当前/最大生命值并输出 `BossPhase`；不依赖 Godot 节点、HUD 或房间。
- `RoadblockCommander` 实现已有 `IDamageable` 契约，内部使用运行时生命值；只将生命变化报告给阶段控制器，不直接操纵 HUD。
- `BossHudController` 订阅 Boss 信号并显示名称、阶段文字、当前/最大生命值和阶段颜色；房间释放时显式解绑。
- 07A Boss 房间由 `BossDefinition` 关联的独立 Boss 场景组合加载，不复用要求普通敌军波次与出生点的 `RoomDefinition`；场景仍使用 `Ground`、`Structure`、`Destructible` 三层 TileMap。

## 5. 约束

- Godot 4.7 .NET / C#；逻辑画布保持 480×270、默认显示窗口保持 1440×810。
- 移动、受击与碰撞只能在物理帧更新；阶段状态机不得在每帧分配或重建资源。
- Boss 使用敌人碰撞层，玩家炮弹可命中；Boss 不得穿过 Structure 或 Destructible 地形。
- 当前协议效果仍通过 `BuildController` 和属性管线作用；07A 不得向协议资源写入 Boss 特例。
- 发现需要改动 Boss 阶段阈值、双生命线规则、普通敌人池、导航架构或 07B 内容时，必须暂停并请求用户确认。

## 6. 测试矩阵

| 层级 | 证据 | 通过条件 |
|---|---|---|
| 纯逻辑 | `BossPhaseController` 测试 | 50% 阈值准确；阶段/击败事件均只触发一次；击败不可逆 |
| 资源 | `BossDefinition` 与 Boss 房间资源测试 | 资源完整、场景合法、三层 TileMap 存在 |
| 运行时 | Godot headless Boss 测试宿主 | 受击后 HUD 数值和阶段状态同步，无异常 |
| 构建 | Release build、编辑器解析、启动冒烟 | 0 warning / 0 error，场景可加载 |
| 可见验收 | 玩家运行 Godot | 独立 Boss 血条、50% 阶段提示、归零击败提示均可观察 |

## 7. 用户验收脚本

| 操作 | 预期画面/状态 |
|---|---|
| 进入 Boss 验收房 | 顶部显示“路障指挥车”独立血条；玩家装甲和中继站 HUD 数值不被替换 |
| 将 Boss 打至略高于 50% | 血条仍为第一阶段颜色，阶段文字为“第一阶段” |
| 再造成伤害使其首次达到/低于 50% | 血条立即切换第二阶段颜色与“第二阶段”文字；Boss 出现一次短暂预警闪烁 |
| 继续反复命中 | 阶段不重复切换、不反复闪烁；玩家、中继站与重启规则保持正常 |
| 将 Boss 生命降至 0 | Boss 停止战斗并显示击败提示一次；不出现结算页，也不重复触发击败 |

## 8. 与 07B 的接口边界

07A 只暴露 `BossPhaseController.CurrentPhase`、`PhaseChanged` 和 `Defeated`。07B 的部署、召唤、冲锋和结算只能订阅这些接口；不得修改 07A 阶段判定逻辑或直接读取/写入 Boss 私有生命值。

## 9. 偏离记录

2026-07-18：用户确认 Boss 验收房采用独立 `BossDefinition + BossRoom` 组合，不扩展普通 `RoomDefinition`。原因是普通房间资源强制包含敌军波次与出生点，而 07A Boss 骨架不含普通波次；该调整不改变 Boss 两阶段规则、双生命线、动态地形或 07B 内容范围。
