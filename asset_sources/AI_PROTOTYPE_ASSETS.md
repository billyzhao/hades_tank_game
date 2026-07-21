# AI 原型资源登记

| 文件 | 工具 | 生成日期 | 用途 | 人工处理 | 状态 |
| --- | --- | --- | --- | --- | --- |
| `game1/assets/art/actors/hero_tank.png` | Codex 内置 image generation | 2026-07-16 | 重型主角坦克视觉原型 | 移除洋红背景、nearest 缩放至 64×64、保留透明通道 | 仅原型，不作最终商用资产 |
| `game1/assets/art/actors/hero_hull.png`、`hero_turret.png` | 项目内像素处理 | 2026-07-16 | 主角车体与独立炮塔运行图层 | 从已登记主角原型按透明区域拆分，未新增外部来源 | 仅原型，不作最终商用资产 |
| `game1/assets/art/tiles/desert_industrial_arena.png` | Codex 内置 image generation | 2026-07-16 | 黄沙废土工业战场背景原型 | nearest 缩放至 480×270 | 仅原型，不作最终商用资产 |
| `game1/assets/art/actors/enemy_vehicle.png` | Codex 内置 image generation | 2026-07-16 | 三类敌军共用灰阶底盘，由运行时职责色区分 | 移除洋红背景、nearest 缩放至 48×48、保留透明通道 | 仅原型，不作最终商用资产 |
| `game1/assets/art/actors/relay_station.png`（已删除） | Codex 内置 image generation | 2026-07-16 | 已弃用的历史中继站原型 | 曾移除洋红背景、nearest 缩放至 64×64、保留透明通道 | Gate 0 已确认且 Alpha 02B 已解除运行依赖，图片与导入文件已删除；本行只保留来源与删除原因 |
| `asset_sources/ai_generated/batch-02-mobile-core/full-gameplay-proportion/mobile-core-gameplay-proportion.png` | Codex 内置 image generation | 2026-07-20 | 无中继站移动核心竞技场 Gate 0 真实游戏比例、HUD 与奖励占屏验证 | 原始 1672×941 PNG 复制入源文件区，未裁切、未缩放、未调色；完整提示词见同目录 `.prompt.txt` | 用户已确认 Gate 0；只作布局与后续素材批次基线，不直接进入游戏 |
| `asset_sources/ai_generated/batch-01-mobile-core/player-core-sample/player-core-state-comparison-v2-raw.png` | Codex 内置 image generation | 2026-07-21 | Batch 1 玩家坦克核心休眠/激活状态视觉门禁（第二版） | 以已确认 Gate 0 和第一版样张为输入；第一版因炮管不够明确退回，第二版只修正独立炮塔与长炮管可读性；本地按左右两格清除洋红背景，生成 64×64 QC 帧与 256×256 放大预览；完整提示词和 QC 元数据见同目录 | 2026-07-21 用户验收通过并确认为 Batch 1 基准；尚未复制进 `game1/assets/`，后续扩展仍执行少量样张门禁 |
| `asset_sources/ai_generated/batch-02-mobile-core/sparse-arena-sample/blockade-city-sparse-arena-v1.png` | Codex 内置 image generation | 2026-07-21 | Batch 2 封锁城区极稀疏竞技场环境与比例门禁 | 原始 PNG 未裁切、未缩放、未调色；完整提示词及使用边界见同目录 | 待用户确认；仅作环境、开放区和障碍密度基准，不进入 `game1/assets/` |
| `asset_sources/ai_generated/batch-05-mobile-core/level-up-ui-sample/level-up-hud-v1.png` | Codex 内置 image generation | 2026-07-21 | Batch 5 经验 HUD 与完全暂停三选一比例门禁 | 原始 PNG 未裁切、未缩放、未调色；使用边界见同目录 | 待用户确认；仅作 HUD 与升级面板信息层级参考，不进入 `game1/assets/` |

当前精确提示词仅存在于原始 Codex 对话，仓库内没有可验证的完整提示词副本。不得根据成品反向编造提示词；若原型继续使用，应在正式素材替换前重新生成并把提示词、模型、日期和处理步骤保存到仓库外源文件区。

`asset_sources/ai_generated/batch-00-style/` 中带有独立中继站、基地守卫布局或第二条基地血量的旧图、提示词和比例稿已在 Gate 0 通过、Alpha 02B 解除运行依赖后删除；本表继续保留文字来源与弃用原因。`batch-01-units/` 中的坦克候选仍需按新批次门禁重新确认，未经确认不得复制进 `game1/assets/`。
