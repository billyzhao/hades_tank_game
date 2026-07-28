# Batch 10：坦克构筑外观与辅助机进阶

## 批次信息

- 生成日期：2026-07-28
- 生成工具：Codex 内置 `image_gen`
- 当前状态：`CONFIRMED`，2026-07-28 已通过用户当前阶段实机验收
- 完整提示词：[`PROMPT.md`](PROMPT.md)

## 原始文件

| 文件 | 内容 | 结论 |
| --- | --- | --- |
| `raw/build-visual-modules-3x4.png` | 四部门协议模块、四辅助 Mk.II、四辅助 Mk.III | 第 1～8 格采用；第 9、10、12 格越过分格边缘，整行 Mk.III 不采用 |
| `raw/auxiliary-mk3-1x4.png` | 四辅助 Mk.III 重生成 | 四格边缘检查通过，采用 |

## 加工与拒收

- 3×4 母版按 3 行 4 列、128×128 单格、最近邻缩放、洋红去背处理；
- `processed/modules-source/pipeline-meta.json` 记录原始分格，其中第三行格位 9、10、12 为 `edge_touch`；
- 第 1～8 格使用 `processed/modules-largest/` 的最大连通组件版本，去除格内零散污染；
- Mk.III 使用 `processed/auxiliary-mk3/`，其 `pipeline-meta.json` 的 `edge_touch_frames` 为空；
- 被拒的 3×4 第三行只保留来源审计，不复制到运行目录。

## 运行映射

| 运行文件 | 源格位 | 用途 |
| --- | --- | --- |
| `player/upgrades/protocol_arsenal.png` | 3×4 格 1 | 军械协议模块 |
| `player/upgrades/protocol_recon.png` | 3×4 格 2 | 侦察协议模块 |
| `player/upgrades/protocol_logistics.png` | 3×4 格 3 | 后勤协议模块 |
| `player/upgrades/protocol_engineering.png` | 3×4 格 4 | 工程协议模块 |
| `auxiliaries/orbit_drone_mk2.png` | 3×4 格 5 | 环绕无人机 Mk.II |
| `auxiliaries/side_cannon_mk2.png` | 3×4 格 6 | 侧挂速射炮 Mk.II |
| `auxiliaries/mine_layer_mk2.png` | 3×4 格 7 | 履带布雷器 Mk.II |
| `auxiliaries/suppression_field_mk2.png` | 3×4 格 8 | 区域压制器 Mk.II |
| `auxiliaries/orbit_drone_mk3.png` | 1×4 格 1 | 环绕无人机 Mk.III |
| `auxiliaries/side_cannon_mk3.png` | 1×4 格 2 | 侧挂速射炮 Mk.III |
| `auxiliaries/mine_layer_mk3.png` | 1×4 格 3 | 履带布雷器 Mk.III |
| `auxiliaries/suppression_field_mk3.png` | 1×4 格 4 | 区域压制器 Mk.III |

运行目录前缀为 `game1/assets/sprites/`。素材只承担外观；协议部门、等级、槽位、伤害和行为继续由正式资源及 `BuildController` 决定。
