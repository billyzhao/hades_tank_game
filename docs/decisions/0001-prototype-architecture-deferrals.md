# 0001：单房间原型架构暂缓项

状态：已登记的历史原型简化；仅适用于当前单房间守点与表现验证，不授权未来继续偏离。

## 已发生的简化

1. 砖墙使用独立 `StaticBody2D`，未采用 `TileMapLayer` 运行时格子耐久。
2. 敌军使用 `DefenseRoutePlanner` 固定通路，未采用 `AStarGrid2D`。
3. 敌军职责集中在 `EnemyTank` + `BehaviorId`，尚未采用 `EnemyDefinition` Resource 与独立行为策略。
4. 房间状态目前只有 Loading/Combat/Cleared/Failed，尚未接入 Intro/Reward/Exiting。
5. 重启流程尚未实现敌方炮弹清理和重启后短暂无敌。
6. 部分表现参数仍硬编码，尚未集中到表现配置 Resource。

## 强制偿还门槛

- 迭代 6 接入三选一和第二场战斗前，必须按技术文档实现 `ProtocolDefinition`、运行时构筑作用域、确定性奖励和完整的计划房间生命周期 `Loading → Intro → Combat → Cleared → Reward → Exiting`，并使 `Failed` 从 `Combat` 分支；不得沿用硬编码奖励。
- 添加第二个不同布局的战斗房间前，必须重新确认并落地 `RoomDefinition`、TileMap 地形和可更新导航方案。
- 扩充普通敌人池前，必须将敌人属性和职责迁移到 `EnemyDefinition` Resource/策略边界。
- 引入敌方实体炮弹前，必须补齐重启清弹和短暂无敌。
- 扩充成套 VFX/音频参数前，必须建立集中、可替换的表现配置资源。

任何门槛的变更都属于实质偏离，必须再次获得用户确认并更新技术设计。
# 2026-07-18：迭代 06B 架构偿还记录

- 已采用 `RoomDefinition`：场景、网格、波次和敌军出生边均由房间资源提供。
- 已采用 `Ground`、`Structure`、`Destructible` 三层 `TileMapLayer`；可破坏砖墙不再由逐块 `StaticBody2D` 承载。
- 已采用事件驱动的 `AStarGrid2D`：加载或砖块摧毁时重建，敌军无路时停下并在 0.25 秒后重试。
