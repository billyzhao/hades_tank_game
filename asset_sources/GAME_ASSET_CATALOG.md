# 《废土中继》游戏素材主清单

> 文档版本：asset-catalog-v1
> 最近更新：2026-07-24
> 适用范围：封锁城区可交付版；后四区仅保留延期索引
> 维护原则：游戏元素使用稳定素材 ID；确认新批次后更新本清单，不用文件名承载玩法规则。

## 1. 文档用途

本文是“游戏元素、运行素材、源素材批次、确认状态”的唯一总索引，用于回答：

- 游戏中的某个坦克、敌人、地形、特效、UI 或音频由哪个文件表现；
- 当前文件是否已经接入 Godot，是否经过用户实机确认；
- 源图、提示词和处理记录保存在哪个批次；
- 哪些素材只是过渡版本、共用版本或尚未接入的候选；
- 后续替换素材时需要修改哪些运行引用，以及旧素材如何退役。

本文不保存伤害、生命、射速、碰撞、波次、奖励或 AI 行为等玩法数据。玩法配置继续由 `.tres`、场景和 C# 代码维护。

相关文档：

- [移动核心竞技场素材生产方案](MOBILE_CORE_ASSET_PLAN.md)
- [AI 原型资源登记](AI_PROTOTYPE_ASSETS.md)
- [第三方素材与授权清单](THIRD_PARTY_ASSETS.md)
- [Godot 运行素材目录说明](../game1/assets/README.md)

## 2. 状态定义

| 状态 | 含义 | 是否允许继续开发 |
| --- | --- | --- |
| `CONFIRMED` | 已接入游戏并通过用户当前阶段实机确认 | 可以；后续仍可按新批次升级 |
| `PROVISIONAL` | 已接入且当前可用，但仍是旧原型、共用图或缺少完整状态 | 可以；进入正式美术阶段前必须替换 |
| `PARTIAL` | 只完成部分组件或只替换框架，不能称为整项完成 | 可以验证已有部分，不能关闭素材任务 |
| `CANDIDATE` | 已生成或已加工，但未接入或未经过用户确认 | 不得自动替换运行素材 |
| `MISSING` | 策划已定义，但尚无对应生产素材 | 只能使用明确登记的临时表现 |
| `DEFERRED` | 长期内容已定义，但不在封锁城区当前交付范围 | 不生产、不接入、不采购 |
| `RETIRED` | 已废止，不得重新接入当前玩法 | 不允许 |

“文件存在”不等于 `CONFIRMED`；必须同时满足运行引用、游戏内可读性验证和用户确认。

封锁城区交付门禁只统计当前玩家可见范围。关键素材不得保留 `MISSING`，现有 `PROVISIONAL` / `PARTIAL` 必须升级为经过实机确认的正式素材或在书面验收中明确列为非关键限制。后四区 `DEFERRED` 不计入当前缺口，也不得因此提前生产。

## 3. 全局角色与战斗单位

| 素材 ID | 游戏元素 | 运行素材 | 源批次/来源 | 当前状态 | 维护说明 |
| --- | --- | --- | --- | --- | --- |
| `player_hull` | 玩家坦克车体 | `game1/assets/art/actors/hero_hull.png` | 2026-07-16 AI 原型拆分 | `PROVISIONAL` | 已接入并可用，但仍是早期原型；后续三核心正式版替换时保持车体节点不变 |
| `player_turret` | 玩家独立炮塔 | `game1/assets/art/actors/hero_turret.png` | 2026-07-16 AI 原型拆分 | `PROVISIONAL` | 已支持独立旋转；正式版本需要补炮口、受击和核心差异 |
| `player_assembled_reference` | 玩家整车历史参考 | `game1/assets/art/actors/hero_tank.png` | 2026-07-16 AI 原型 | `CANDIDATE` | 当前场景不直接引用，只用于车体/炮塔来源审计 |
| `core_breakthrough_visual` | 突破重炮核心视觉 | 尚无独立运行素材 | Batch 01 第二版样张是视觉基准 | `MISSING` | 需要核心嵌入部件、激活光色、冲刺与开火反馈 |
| `core_overload_visual` | 过载速射核心视觉 | 尚无独立运行素材 | 尚未生产 | `MISSING` | 不得只用文字区分 |
| `core_electric_visual` | 电驱游骑核心视觉 | 尚无独立运行素材 | 尚未生产 | `MISSING` | 需要独立能量色和冲刺轨迹 |
| `aux_orbit_drone` | 环绕无人机 | `game1/assets/sprites/auxiliaries/orbit_drone.png` | Batch 07 | `CONFIRMED` | 当前为静态部件加程序环绕；后续可补开火帧 |
| `aux_side_cannon` | 侧挂速射炮 | `game1/assets/sprites/auxiliaries/side_cannon.png` | Batch 07 | `CONFIRMED` | 当前显示与自动开火逻辑已接入 |
| `aux_mine_layer` | 履带布雷器 | `game1/assets/sprites/auxiliaries/mine_layer.png` | Batch 07 | `CONFIRMED` | 当前是随车部件；独立地雷实体表现尚未制作 |
| `aux_suppression_field` | 区域压制器 | `game1/assets/sprites/auxiliaries/suppression_field.png` | Batch 07 | `CONFIRMED` | 部件已接入，压制范围特效仍缺失 |

## 4. 竞技场 1：封锁城区

### 4.1 场景与地形

| 素材 ID | 游戏元素 | 运行素材 | 源批次/来源 | 当前状态 | 维护说明 |
| --- | --- | --- | --- | --- | --- |
| `arena_01_foundation` | 黄沙工业城区基础地面 | `game1/assets/art/tiles/blockade_city_foundation.png` | Batch 06 | `CONFIRMED` | 只作为无碰撞地面背景；三个首区房间共用 |
| `arena_01_tileset` | 首区碰撞 TileSet 图集 | `game1/assets/sprites/terrain/blockade_city_tileset.png` | Batch 07 | `CONFIRMED` | 48×24，左格钢墙、右格可破坏墙 |
| `arena_01_steel` | 固定钢制掩体 | `game1/assets/sprites/terrain/blockade_steel.png` | Batch 07 | `CONFIRMED` | 单图用于来源和复用；运行 TileMap 使用合并图集 |
| `arena_01_brick` | 可破坏墙 | `game1/assets/sprites/terrain/blockade_brick.png` | Batch 07 | `CONFIRMED` | 耐久由 `TileTerrainAdapter` 维护，不从图片推断 |
| `arena_01_spawn_beacon` | 四周敌军刷新预警信标 | `game1/assets/sprites/terrain/spawn_beacon.png` | Batch 07 | `CONFIRMED` | 与程序圆环共同显示；不参与碰撞 |
| `arena_01_boss_barrier` | 路障指挥车部署路障 | `game1/assets/sprites/terrain/roadblock_barrier.png` | Batch 07 | `CANDIDATE` | 文件已生成，但当前 Boss 部署仍写入钢墙 TileSet，尚未独立引用 |
| `arena_01_boundary_props` | 场地边界工业设施 | 烘在 `blockade_city_foundation.png` 的非交互边缘 | Batch 06 | `PROVISIONAL` | 后续若需要交互或遮挡，必须拆成独立对象 |
| `legacy_desert_arena` | 旧黄沙工业背景 | `game1/assets/art/tiles/desert_industrial_arena.png` | 2026-07-16 AI 原型 | `RETIRED` | 当前首区不再引用，不得作为新竞技场正式背景 |

### 4.2 普通敌军与精英

| 素材 ID | 游戏元素 | 运行素材 | 源批次/来源 | 当前状态 | 维护说明 |
| --- | --- | --- | --- | --- | --- |
| `enemy_scout` | 侦察无人机/轻型侦察单位 | `game1/assets/sprites/enemies/patrol_tank.png` | Batch 06 | `PARTIAL` | 当前与巡逻坦克共用贴图；需要独立轻型轮廓 |
| `enemy_patrol` | 巡逻坦克 | `game1/assets/sprites/enemies/patrol_tank.png` | Batch 06 | `CONFIRMED` | 当前可用 |
| `enemy_assault` | 突击车 | `game1/assets/sprites/enemies/assault_vehicle.png` | Batch 06 | `CONFIRMED` | 当前可用 |
| `enemy_mortar` | 迫击炮/攻城车 | `game1/assets/sprites/enemies/siege_tank.png` | Batch 06 | `CONFIRMED` | 当前可用；范围落点特效仍缺失 |
| `enemy_elite_overdrive` | 第五波过载精英 | `game1/assets/sprites/enemies/elite_tank.png` | Batch 06 | `CONFIRMED` | 过载与冷却目前主要靠调色和速度表现 |
| `enemy_legacy_fallback` | 旧敌军通用底盘 | `game1/assets/art/actors/enemy_vehicle.png` | 2026-07-16 AI 原型 | `PROVISIONAL` | `enemy_tank.tscn` 的场景默认图；运行时会被职责贴图覆盖 |

### 4.3 Boss：路障指挥车

| 素材 ID | 游戏元素 | 运行素材 | 源批次/来源 | 当前状态 | 维护说明 |
| --- | --- | --- | --- | --- | --- |
| `boss_01_hull` | 路障指挥车车体 | `game1/assets/sprites/bosses/roadblock_commander_hull.png` | Batch 07 | `CONFIRMED` | 约 2.4 格视觉尺度 |
| `boss_01_turret` | Boss 独立炮塔 | `game1/assets/sprites/bosses/roadblock_commander_turret.png` | Batch 07 | `CONFIRMED` | 与车体分层 |
| `boss_01_weakpoint` | 二阶段散热弱点 | `game1/assets/sprites/bosses/roadblock_commander_weakpoint.png` | Batch 07 | `CONFIRMED` | 只在冲锋结束后的可受伤窗口显示 |
| `boss_01_emplacement` | Boss 火力哨位 | `game1/assets/sprites/bosses/roadblock_gun_emplacement.png` | Batch 07 | `CONFIRMED` | 直线预警仍由程序线条表现 |
| `boss_01_phase_transition_fx` | 阶段转换表现 | 尚无独立素材 | 尚未生产 | `MISSING` | 当前只使用短时调色 |
| `boss_01_charge_telegraph_fx` | 二阶段冲锋预警 | 程序 `Line2D` | 尚未生产 | `PROVISIONAL` | 需要与 Boss 机械语言一致的地面危险带 |
| `boss_01_death_fx` | Boss 击毁表现 | 尚无 Boss 专属素材 | 尚未生产 | `MISSING` | 不能长期复用普通敌军爆炸 |

## 5. 弹道与战斗特效

> 用户于 2026-07-23 明确指出特效尚未完成替换，因此本节不得整体标记为完成。

| 素材 ID | 游戏元素 | 运行素材/当前表现 | 源批次/来源 | 当前状态 | 下一步 |
| --- | --- | --- | --- | --- | --- |
| `fx_player_shell` | 玩家炮弹 | `game1/assets/sprites/effects/player_shell.png` | Batch 06 | `PROVISIONAL` | 补旅行帧、炮口焰和核心差异 |
| `fx_enemy_shell` | 敌军炮弹 | `game1/assets/sprites/effects/enemy_shell.png` | Batch 06 | `PROVISIONAL` | 按直射、迫击、Boss 炮弹区分 |
| `fx_steel_impact` | 钢墙命中 | `game1/assets/sprites/effects/steel_impact.png` | Batch 06 | `PROVISIONAL` | 当前为单张闪光，需要短动画 |
| `fx_enemy_burst` | 普通敌军击毁 | `game1/assets/sprites/effects/enemy_burst.png` | Batch 06 | `PROVISIONAL` | 当前为单张爆点，需要四帧爆炸与残骸 |
| `fx_muzzle_flash` | 玩家炮口焰 | `player_tank.tscn` 中的多边形闪光 | 无 | `MISSING` | 制作独立炮口帧 |
| `fx_tank_dust` | 履带扬尘 | `TankVisualAnimator` 程序多边形 | 无 | `MISSING` | 制作黄沙扬尘短循环 |
| `fx_dash_trail` | 动力冲刺轨迹 | `DashTrail` 程序圆形 | 无 | `MISSING` | 三核心需要可区分版本 |
| `fx_combat_data` | 战斗数据掉落与吸附 | `CombatDataPickup` 程序圆形 | 无 | `MISSING` | 制作数据芯片、吸附拖尾和收集闪光 |
| `fx_player_hit` | 玩家受击 | 运行时调色闪烁 | 无 | `MISSING` | 补装甲火花和方向反馈 |
| `fx_armor_break` | 装甲破裂 | 尚无 | 无 | `MISSING` | 与低装甲 HUD 状态联动 |
| `fx_reboot` | 原地重启 1.2 秒表现 | 尚无正式序列 | 无 | `MISSING` | 制作核心重构、脉冲和保护罩 |
| `fx_level_up` | 即时升级反馈 | 尚无正式特效 | 无 | `MISSING` | 暂停前后需要清晰但不遮挡 |
| `fx_spawn_warning` | 普通敌军刷新预警 | 信标贴图 + 程序圆环 | Batch 07 | `PARTIAL` | 可补闪烁帧和声音 |
| `fx_barrier_warning` | Boss 路障落点 | 程序红色多边形 | 无 | `MISSING` | 使用独立地面警示动画 |
| `fx_suppression_field` | 区域压制范围 | 尚无 | 无 | `MISSING` | 不能只靠敌人掉血表达 |
| `fx_mortar_warning` | 迫击炮范围预警 | 尚无正式素材 | 无 | `MISSING` | 需要形状和倒计时共同提示 |

## 6. UI 与图标

> 用户于 2026-07-23 明确指出 UI 尚未完成替换。Batch 07 只接入工业框架，因此本节整体状态为 `PARTIAL`。

| 素材 ID | 游戏元素 | 运行素材/当前表现 | 源批次/来源 | 当前状态 | 下一步 |
| --- | --- | --- | --- | --- | --- |
| `ui_hud_frame` | 左上装甲/核心/重启 HUD 框 | `game1/assets/sprites/ui/hud_status_frame.png` | Batch 07 | `PARTIAL` | 框架已接入；缺装甲、核心、重启图标与状态变化 |
| `ui_experience_frame` | 等级与经验区域 | `game1/assets/sprites/ui/experience_frame.png` | Batch 07 | `PARTIAL` | 框架已接入；经验条仍为 Godot 基础样式 |
| `ui_reward_card_frame` | 核心、升级、协议、维护卡片框 | `game1/assets/sprites/ui/reward_card_frame.png` | Batch 07 | `PARTIAL` | 框架已接入；缺卡片图标、稀有度、部门和选中状态 |
| `ui_boss_status_frame` | Boss 名称、阶段和血条框 | `game1/assets/sprites/ui/boss_status_frame.png` | Batch 07 | `PARTIAL` | 框架已接入；血条填充和阶段徽记仍是基础控件 |
| `ui_armor_icon` | 装甲图标 | 尚无 | 无 | `MISSING` | 需含低装甲警告状态 |
| `ui_core_icons` | 三核心图标 | 尚无 | 无 | `MISSING` | 与三核心车体视觉一致 |
| `ui_stat_icons` | 即时属性升级图标 | 尚无 | 无 | `MISSING` | 伤害、射速、弹速、移动、装甲等 |
| `ui_department_icons` | 四部门协议图标 | 尚无 | 无 | `MISSING` | 兵工、工程、侦察电子、后勤维修 |
| `ui_auxiliary_icons` | 四辅助系统 HUD 图标 | 当前仅使用文字 | Batch 07 只有战场部件 | `MISSING` | 可从部件提炼但必须单独确认可读性 |
| `ui_wave_elite_icons` | 波次、残敌、精英标识 | 当前文字 | 无 | `MISSING` | 不能只靠颜色区分精英状态 |
| `ui_pause_overlay` | 暂停界面 | 纯色遮罩与文字 | 无 | `PROVISIONAL` | 后续统一工业框架 |
| `ui_result_screen` | 失败/首区完成结算 | 纯色遮罩与按钮 | 无 | `PROVISIONAL` | 需要统计层级、按钮状态和控制器焦点 |
| `ui_acceptance_menu` | Debug 策划验收菜单 | Godot 基础控件 | Debug 工具 | `CONFIRMED` | 不属于正式玩家 UI，可保持工具风格 |

## 7. 音频

当前 `game1/assets/audio/` 只有目录占位，尚未接入正式音频。

| 素材 ID 组 | 游戏元素 | 当前状态 | 最小需求 |
| --- | --- | --- | --- |
| `audio_player_*` | 移动、开火、冲刺、受击、重启 | `MISSING` | 5～8 个核心动作声音 |
| `audio_enemy_*` | 敌军开火、迫击预警、精英过载、击毁 | `MISSING` | 按职责提供可辨识提示 |
| `audio_boss_01_*` | 入场、部署路障、哨位预警、冲锋、弱点、击毁 | `MISSING` | 与阶段机制一一对应 |
| `audio_ui_*` | 卡片移动、确认、升级、维护、失败 | `MISSING` | 支持鼠标和手柄反馈 |
| `music_arena_01` | 封锁城区战斗音乐 | `MISSING` | 五波循环与 Boss 过渡 |
| `ambience_arena_01` | 黄沙工业城区环境 | `MISSING` | 风沙、远处机械和空间底噪 |

## 8. 后续竞技场素材覆盖

| 竞技场 | 地图/地形 | 普通敌军 | Boss | 专属特效 | 当前总体状态 |
| --- | --- | --- | --- | --- | --- |
| 1 封锁城区 | 基础地面、钢墙、砖墙、信标已接入 | 3 张普通职责图 + 1 张精英图；侦察/巡逻共用 | 路障指挥车拆件已接入 | 大部分未完成 | `PARTIAL` |
| 2 废弃工厂 | 延期 | 自爆工程车、布雷车、盾甲车延期 | 熔炉装甲列车延期 | 延期 | `DEFERRED` |
| 3 干涸水库 | 延期 | 掠行艇、狙击炮车、导弹载具延期 | 双联重炮平台延期 | 延期 | `DEFERRED` |
| 4 军阀要塞 | 延期 | 攻城炮车、维修车、指挥坦克延期 | 军阀旗舰坦克延期 | 延期 | `DEFERRED` |
| 5 移动堡垒 | 延期 | 既有兵种精英变体延期 | 履带战争城塞延期 | 延期 | `DEFERRED` |

当前只补齐封锁城区的地形、玩家、敌军、Boss、专属特效、正式 UI 和音频。后续竞技场只有在封锁城区同时通过玩法、美术和交付构建验收后，才重新排期。

## 9. 已废止素材与禁止项

| 素材/概念 | 状态 | 原因 |
| --- | --- | --- |
| 独立中继站实体与血条 | `RETIRED` | 当前玩法只有玩家坦克一条生命线 |
| 基地防区、保护墙和守点构图 | `RETIRED` | 已转为四周来敌的移动核心竞技场 |
| 中继站维修、护盾和警报图标 | `RETIRED` | 对应玩法已删除 |
| 商店、货币、装备背包页面 | `RETIRED` | 当前构筑使用即时属性、协议和辅助系统 |
| Batch 00 旧中继站风格图 | `RETIRED` | Gate 0 后失效并删除 |
| `batch-01-units` 本地候选 | `CANDIDATE` | 未通过独立批次门禁，不得自动进入游戏 |

任何新素材不得重新引入独立中继站、基地耐久、第二失败目标或旧守点地图语义。

## 10. 每批素材确认后的更新流程

每次用户确认一批素材后，主智能体必须在同一次变更中完成：

1. 在 `asset_sources/ai_generated/<batch>/README.md` 记录生成工具、日期、完整提示词、处理步骤、验收结论和被拒版本；
2. 将通过透明度、边界、尺寸和实机可读性检查的运行文件复制到 `game1/assets/`；
3. 更新本文对应素材 ID 的运行路径、源批次、状态、确认日期和维护说明；
4. 如果替换旧文件，将旧行改为 `RETIRED` 或在变更记录中说明，不删除授权与来源审计；
5. 更新 `MOBILE_CORE_ASSET_PLAN.md` 的批次接入记录；
6. 运行资源引用扫描，确认不存在丢失路径和“有文件但没有运行引用”的误报；
7. 用户验收前不得把 `CANDIDATE` 写成 `CONFIRMED`，也不得提交或推送仓库。

## 11. 素材批次变更记录

| 日期 | 批次 | 涉及元素 | 用户结论 | 主清单变化 |
| --- | --- | --- | --- | --- |
| 2026-07-20 | Gate 0 | 全局比例、无中继站竞技场、HUD/卡片占屏 | 确认 | 建立移动核心视觉基线 |
| 2026-07-21 | Batch 01 第二版 | 玩家坦克核心状态样张 | 确认为后续基准 | 样张保留，三核心运行素材仍待生产 |
| 2026-07-22 | Batch 06 | 首区地面、敌军职责图、基础炮弹/命中 | 确认并接入 | 地面和敌军进入运行目录；特效仍为过渡版本 |
| 2026-07-23 | Batch 07 | 首区 Boss、地形模块、四辅助、工业 UI 框架 | 素材总体可用；特效和 UI 尚未完整替换 | Boss/地形/辅助标记确认；UI/特效标记部分或过渡 |
| 2026-07-24 | 范围收敛 | 暂停后四区，优先完成封锁城区正式美术、特效、UI 和音频 | 用户确认方案 1 | 后四区改为 `DEFERRED`；当前缺口只按封锁城区统计 |

后续新增批次按时间追加，不覆盖历史结论。
