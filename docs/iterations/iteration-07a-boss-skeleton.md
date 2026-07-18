# 迭代 07A：路障指挥车 Boss 骨架

## 目标与非目标

目标：交付独立 Boss 验收房、路障指挥车独立生命条、50% 两阶段切换和一次性击败事件。

非目标：路障部署、普通敌军召唤、冲锋、砖墙撞毁、钢墙打断、脆弱窗口与胜利结算均保留给 07B。

## 权威依据

- `docs/superpowers/specs/2026-07-15-roguelite-tank-design.md`
- `docs/superpowers/specs/2026-07-15-roguelite-tank-technical-design.md`
- `docs/superpowers/specs/2026-07-18-iteration-07a-boss-skeleton-design.md`
- `docs/superpowers/plans/2026-07-18-iteration-07a-boss-skeleton.md`
- `AGENTS.md`

## 实现与架构合规

| 条款 | 实现 | 证据 | 状态 |
|---|---|---|---|
| 不可逆两阶段 | `BossPhaseController` | `boss_phase_transitions_once_and_is_irrevocable` | 通过 |
| 静态/运行时分离 | `BossDefinition` 与 `RoadblockCommander` 运行时生命 | 资源与实体测试 | 通过 |
| 独立 Boss 血条 | `BossHudController` 订阅 Boss 信号 | `boss_hud_tracks_health_and_phase` | 通过 |
| 可见验收入口 | `UI/BossValidationButton` | `app_has_visible_boss_validation_entry` | 通过 |
| Boss 房三层 TileMap | `mvp_boss_room.tscn` | `boss_definition_and_room_are_valid` | 通过 |
| 07B 范围隔离 | 未创建攻击、路障、召唤或结算逻辑 | 差异审查 | 通过 |

## 测试证据

- `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite reward_catalog`：24/24 通过。
- `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite navigation_grid`：4/4 通过。
- `godot --headless --path game1 tests/headless/protocol_runtime_test_host.tscn -- --suite boss_phase`：4/4 通过。
- `dotnet build game1/Game1.csproj -c Release --no-restore`：0 warning / 0 error。
- `godot --headless --path game1 --editor --quit`：解析通过。
- `godot --headless --path game1 --quit-after 180`：启动冒烟通过。

## 用户验收脚本

| 操作 | 预期画面/状态 |
|---|---|
| 运行项目，点击右上角“Boss 验收” | 进入独立 Boss 房；顶部出现“路障指挥车”血条与“第一阶段”文字 |
| 持续攻击至 Boss 血条首次到 50% | 血条转为红色、“第二阶段”文字出现，Boss 本体只闪烁一次 |
| 继续攻击 | 第二阶段不会重复切换；玩家装甲、中继站耐久和重启仍按原规则工作 |
| 将 Boss 打至 0 | 显示“已击败”和底部 07B 结算提示；不出现胜利结算页 |

## 偏离记录

用户于 2026-07-18 确认：Boss 房采用独立 `BossDefinition + BossRoom`，不扩展强制普通波次/出生点的 `RoomDefinition`。规格与计划已同步；未发现其他实质偏离。

## 门禁状态

- [x] 策划与测试前置
- [x] 开发理解、架构合规、测试后置与主智能体自检
- [ ] 用户可见验收
- [ ] 用户验收后提交/推送
