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
| `player_hull` | 玩家坦克车体 | `game1/assets/sprites/player/player_hull.png` | Batch 08 | `CONFIRMED` | 已接入正式重型履带底盘；2026-07-24 实机验收通过 |
| `player_turret` | 玩家独立炮塔 | `game1/assets/sprites/player/player_turret.png` | Batch 08 | `CONFIRMED` | 已接入独立炮塔并保留原瞄准节点；2026-07-24 实机验收通过 |
| `player_assembled_reference` | 玩家整车历史参考 | `game1/assets/art/actors/hero_tank.png` | 2026-07-16 AI 原型 | `CANDIDATE` | 当前场景不直接引用，只用于车体/炮塔来源审计 |
| `core_breakthrough_visual` | 突破重炮核心视觉 | `game1/assets/sprites/player/core_breakthrough.png` | Batch 08 | `CONFIRMED` | 选择核心后替换坦克中央部件，并与对应 HUD 图标一致 |
| `core_overload_visual` | 过载速射核心视觉 | `game1/assets/sprites/player/core_overdrive.png` | Batch 08 | `CONFIRMED` | 橙红散热结构已独立，不再只靠文字区分 |
| `core_electric_visual` | 电驱游骑核心视觉 | `game1/assets/sprites/player/core_electric.png` | Batch 08 | `CONFIRMED` | 青蓝线圈结构与冲刺序列已接入 |
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
| `enemy_scout` | 侦察无人机/轻型侦察单位 | `game1/assets/sprites/enemies/scout_drone.png` | Batch 08 | `CONFIRMED` | 独立悬浮轮廓已由敌军定义资源引用；2026-07-24 群体实机验收通过 |
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
| `boss_01_phase_transition_fx` | 阶段转换表现 | `game1/assets/sprites/effects/animations/boss_phase_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 二阶段信号触发专属机械能量序列 |
| `boss_01_charge_telegraph_fx` | 二阶段冲锋预警 | `game1/assets/sprites/effects/animations/charge_warning_1.png` 等四帧 + `Line2D` | Batch 08 | `CONFIRMED` | 地面终点序列与方向线共同表达冲锋 |
| `boss_01_death_fx` | Boss 击毁表现 | `game1/assets/sprites/effects/animations/boss_death_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 使用 Boss 专属重型击毁序列 |

## 5. 弹道与战斗特效

> 用户于 2026-07-23 明确指出特效尚未完成替换，因此本节不得整体标记为完成。

| 素材 ID | 游戏元素 | 运行素材/当前表现 | 源批次/来源 | 当前状态 | 下一步 |
| --- | --- | --- | --- | --- | --- |
| `fx_player_shell` | 玩家炮弹 | `game1/assets/sprites/effects/animations/player_projectile_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 青色核心与金属弹体循环，阵营伤害规则不变 |
| `fx_enemy_shell` | 敌军炮弹 | `game1/assets/sprites/effects/animations/enemy_projectile_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 橙红危险弹道循环；迫击差异由落点预警补充 |
| `fx_steel_impact` | 钢墙命中 | `game1/assets/sprites/effects/animations/steel_impact_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 已由单张闪光改为短序列 |
| `fx_enemy_burst` | 普通敌军击毁 | `game1/assets/sprites/effects/animations/enemy_burst_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 已由单张爆点改为四帧击毁 |
| `fx_muzzle_flash` | 玩家炮口焰 | `game1/assets/sprites/effects/animations/muzzle_flash_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 由开火信号重播并在结束后隐藏 |
| `fx_tank_dust` | 履带扬尘 | `game1/assets/sprites/effects/animations/tank_dust_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 普通移动按节奏生成短寿命序列 |
| `fx_dash_trail` | 动力冲刺轨迹 | `game1/assets/sprites/effects/animations/dash_trail_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 冲刺期间替代普通扬尘 |
| `fx_combat_data` | 战斗数据掉落与吸附 | `game1/assets/sprites/effects/animations/combat_data_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 芯片循环保留原磁吸与实时经验结算 |
| `fx_player_hit` | 玩家受击 | `game1/assets/sprites/effects/animations/player_hit_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 与原调色闪烁叠加，仍不参与伤害结算 |
| `fx_armor_break` | 装甲破裂 | 复用 `player_hit_*.png` 强火花段 | Batch 08 | `PARTIAL` | 已有可读火花；低装甲专属持续态留到 BC-04/验收缺口处理 |
| `fx_reboot` | 原地重启 1.2 秒表现 | `game1/assets/sprites/effects/animations/reboot_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 重构开始与恢复完成各触发一次 |
| `fx_level_up` | 即时升级反馈 | `game1/assets/sprites/effects/animations/level_up_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 每次确认即时升级后在玩家位置播放 |
| `fx_spawn_warning` | 普通敌军刷新预警 | `game1/assets/sprites/effects/animations/spawn_warning_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 替换程序圆环，出生安全规则不变 |
| `fx_barrier_warning` | Boss 路障落点 | `game1/assets/sprites/effects/animations/barrier_warning_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 独立矩形落点预警后才写入 TileMap |
| `fx_suppression_field` | 区域压制范围 | 复用 `mortar_warning_*.png` 的青色友军环 | Batch 08 | `PARTIAL` | 已有范围触发反馈；后续可补独立持续场纹理 |
| `fx_mortar_warning` | 迫击炮范围预警 | `game1/assets/sprites/effects/animations/mortar_warning_1.png` 等四帧 | Batch 08 | `CONFIRMED` | 迫击职责进入攻击前摇时锁定玩家落点显示 |

## 6. UI 与图标

> 用户于 2026-07-23 明确指出 UI 尚未完成替换。Batch 07 只接入工业框架，因此本节整体状态为 `PARTIAL`。

| 素材 ID | 游戏元素 | 运行素材/当前表现 | 源批次/来源 | 当前状态 | 下一步 |
| --- | --- | --- | --- | --- | --- |
| `ui_hud_frame` | 左上装甲/核心/重启 HUD 框 | `game1/assets/sprites/ui/hud_status_frame.png` + `ui/icons/` | Batch 07 + Batch 08 | `CONFIRMED` | 框架和语义图标已接入；2026-07-24 实机验收通过 |
| `ui_experience_frame` | 等级与经验区域 | `game1/assets/sprites/ui/experience_frame.png` | Batch 07 | `PARTIAL` | 框架已接入；经验条仍为 Godot 基础样式 |
| `ui_reward_card_frame` | 核心、升级、协议、维护卡片框 | `game1/assets/sprites/ui/reward_card_frame.png` + 语义图标 | Batch 07 + Batch 08 | `CONFIRMED` | 核心、属性和部门图标已接入卡片；2026-07-24 实机验收通过 |
| `ui_boss_status_frame` | Boss 名称、阶段和血条框 | `game1/assets/sprites/ui/boss_status_frame.png` + 精英/阶段图标 | Batch 07 + Batch 08 | `CONFIRMED` | 工业血条样式和阶段徽记已接入 |
| `ui_armor_icon` | 装甲图标 | `game1/assets/sprites/ui/icons/armor.png` | Batch 08 | `CONFIRMED` | HUD、维护和结算可复用 |
| `ui_core_icons` | 三核心图标 | `game1/assets/sprites/ui/icons/core_*.png` | Batch 08 | `CONFIRMED` | 与坦克核心部件使用相同色形语言 |
| `ui_stat_icons` | 即时属性升级图标 | `game1/assets/sprites/ui/icons/{armor,move_speed,damage,fire_rate,projectile_speed}.png` | Batch 08 | `CONFIRMED` | 已接入即时升级三选一 |
| `ui_department_icons` | 四部门协议图标 | `game1/assets/sprites/ui/icons/{arsenal,engineering,reconnaissance,logistics}.png` | Batch 08 | `CONFIRMED` | 奖励 ID 按部门前缀选择图标 |
| `ui_auxiliary_icons` | 辅助系统 HUD 图标 | `game1/assets/sprites/ui/icons/auxiliary.png` | Batch 08 | `PARTIAL` | HUD 已有统一辅助槽图标；四种辅助独立小图仍使用战场部件 |
| `ui_wave_elite_icons` | 波次、残敌、精英标识 | `game1/assets/sprites/ui/icons/{wave,elite}.png` | Batch 08 | `CONFIRMED` | 与实时文字和计数并列，不只依靠颜色 |
| `ui_pause_overlay` | 暂停界面 | 工业卡框 + `wave.png` + 遮罩 | Batch 07 + Batch 08 | `CONFIRMED` | Esc 暂停语义和输入规则不变 |
| `ui_result_screen` | 失败/首区完成结算 | 工业卡框 + 核心图标 + 统计与按钮 | Batch 07 + Batch 08 | `CONFIRMED` | 保留控制器焦点并显示本局核心 |
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
| 1 封锁城区 | 基础地面、钢墙、砖墙、信标已接入 | 四类职责图 + 1 张精英图 | 路障指挥车拆件及专属阶段/击毁序列已接入 | Batch 08 已完成实机验收 | `CONFIRMED` |
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
| 2026-07-24 | Batch 08 | 玩家/三核心、侦察、弹道与战斗序列、关键预警、正式 UI 图标 | 用户实机验收通过 | 正式运行素材升级为 `CONFIRMED`；独立扩展项继续保持 `PARTIAL` |

后续新增批次按时间追加，不覆盖历史结论。
