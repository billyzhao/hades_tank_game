# AI 原型资源登记

| 文件 | 工具 | 生成日期 | 用途 | 人工处理 | 状态 |
| --- | --- | --- | --- | --- | --- |
| `game1/assets/art/actors/hero_tank.png` | Codex 内置 image generation | 2026-07-16 | 重型主角坦克视觉原型 | 移除洋红背景、nearest 缩放至 64×64、保留透明通道 | 仅原型，不作最终商用资产 |
| `game1/assets/art/actors/hero_hull.png`、`hero_turret.png` | 项目内像素处理 | 2026-07-16 | 主角车体与独立炮塔运行图层 | 从已登记主角原型按透明区域拆分，未新增外部来源 | 仅原型，不作最终商用资产 |
| `game1/assets/art/tiles/desert_industrial_arena.png` | Codex 内置 image generation | 2026-07-16 | 黄沙废土工业战场背景原型 | nearest 缩放至 480×270 | 仅原型，不作最终商用资产 |
| `game1/assets/art/actors/enemy_vehicle.png` | Codex 内置 image generation | 2026-07-16 | 三类敌军共用灰阶底盘，由运行时职责色区分 | 移除洋红背景、nearest 缩放至 48×48、保留透明通道 | 仅原型，不作最终商用资产 |
| `game1/assets/art/actors/relay_station.png` | Codex 内置 image generation | 2026-07-16 | 已弃用的历史中继站原型 | 移除洋红背景、nearest 缩放至 64×64、保留透明通道 | 仅保留来源与处理记录，不进入游戏、不用于新素材参考；新比例基线确认且 Alpha 02B 解除运行依赖后，删除图片文件，只保留来源与删除原因的文字记录 |
| `asset_sources/ai_generated/batch-02-mobile-core/full-gameplay-proportion/mobile-core-gameplay-proportion.png` | Codex 内置 image generation | 2026-07-20 | 无中继站移动核心竞技场 Gate 0 真实游戏比例、HUD 与奖励占屏验证 | 原始 1672×941 PNG 复制入源文件区，未裁切、未缩放、未调色；完整提示词见同目录 `.prompt.txt` | 等待用户比例验收；只作布局基线，不进入游戏、不授权 Batch 1 |

当前精确提示词仅存在于原始 Codex 对话，仓库内没有可验证的完整提示词副本。不得根据成品反向编造提示词；若原型继续使用，应在正式素材替换前重新生成并把提示词、模型、日期和处理步骤保存到仓库外源文件区。

`asset_sources/ai_generated/batch-00-style/` 中带有独立中继站、基地守卫布局或第二条基地血量的旧图、提示词和比例稿，全部排除在移动核心视觉基线之外。待新的无中继站比例基线通过、并且 Alpha 02B 解除运行时依赖后，删除这些图像文件；本表继续保留文字来源与弃用原因。`batch-01-units/` 中的坦克候选仍需按新批次门禁重新确认，未经确认不得复制进 `game1/assets/`。
