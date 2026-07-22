# 第一竞技场可用素材小样（Batch 06）

生成日期：2026-07-22
状态：用户于 2026-07-22 确认方向；已完成首批裁切、去底、Godot 导入及首区运行引用。

本批次用于补齐封锁城区首张竞技场的实机素材方向，保持已确认的重型机械坦克、明亮黄沙和清晰街机可读性。样张仍保留为审计源图；首批运行资产由单独的洋红底运行图源裁切而来。

| 文件 | 用途 | 后续处理 |
| --- | --- | --- |
| `arena-terrain-kit-v1.png` | 开阔竞技场地面、低掩体、工业边界、摆件与入口预警模块 | 确认后按 24/48 逻辑格裁切；碰撞数据仍由 TileMap 独立维护 |
| `enemy-roster-kit-v1.png` | 四种普通敌军与一名精英的轮廓、四向和炮口帧参考 | 确认职责可读性后重制为独立透明运行图集 |
| `combat-vfx-kit-v1.png` | 玩家/敌方弹丸、命中、爆炸、战斗数据吸附、冲刺与预警 | 确认后裁切为紧凑逐帧特效资源 |

## 已接入的首批运行资产

- `game1/assets/art/tiles/blockade_city_foundation.png`：首区三个运行房间的背景地面；不承载碰撞或导航。
- `game1/assets/sprites/enemies/{patrol_tank,assault_vehicle,siege_tank,elite_tank}.png`：按敌军职责和精英状态选择的透明运行贴图。
- `game1/assets/sprites/effects/{player_shell,enemy_shell,steel_impact,enemy_burst}.png`：双方炮弹、命中与击毁反馈。
- 运行图源、提示词和裁切过程均保留于本目录；新运行图源为 `enemy-runtime-sheet-v1-raw.png` 与 `combat-vfx-runtime-sheet-v1-raw.png`。

禁止事项：不得直接把本批次图像作为场景背景整体铺入；不得从图中推断或引入中继站、基地生命线或固定守点玩法。
