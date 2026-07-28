# Batch 10：构筑外观模块生成提示词

生成日期：2026-07-28
工具：Codex 内置 `image_gen`
原始输出：`raw/build-visual-modules-3x4.png`

## 生成提示词

Create a production-oriented top-down pixel-art game prop pack for the same heavy sci-fi wasteland tank shown in the references. Match its bright sand-readable palette, dark crisp outline, brass/steel mechanical material language, cyan energy accents, compact FC-arcade readability, and exact pixel density. Use the layout-guide image only for an invisible 3-row by 4-column grid with equal slots, centering, spacing, and safe padding. Do not reproduce any guide boxes, lines, labels, borders, white background, or center marks.

Exactly 12 independent compact transparent-ready tank upgrade modules in a 3x4 grid, read left to right:

- Row 1, protocol modules: (1) Arsenal heavy muzzle-brake and compact ammunition feed, orange-red hazard accents, designed to mount on the player turret/front; (2) Recon cyan radar scanner and slim antenna pod, designed for turret top; (3) Logistics paired reinforced side-skirt armor plates with repair canisters, military green accents, designed for hull left/right; (4) Engineering yellow industrial induction coil and compact mechanical actuator, designed for hull rear/top.
- Row 2, Mk.II auxiliary forms: (1) upgraded twin-fin orbit drone, still one compact drone; (2) upgraded twin-barrel side cannon module; (3) upgraded dual-port rear mine-layer module with visible compact magazine; (4) upgraded dual-coil suppression-field generator.
- Row 3, Mk.III auxiliary forms: (1) larger command orbit drone with cyan energy ring; (2) heavy rotary side cannon module; (3) heavy armored multi-port mine-layer module; (4) complete suppression-field generator with a compact cyan core and four industrial emitters.

Every cell contains only one isolated module, viewed directly from above, no tank body, no ground, no shadow plate, no pedestal, no UI, no text, no labels, no numbers, no detached effects, no bullets, no smoke. Each module must fit fully within the central 60% of its cell with solid magenta margin on all four sides. No module may cross a cell edge. Maintain consistent top-down perspective and consistent pixel scale. Background must be exactly 100% flat solid `#FF00FF` with no gradients or texture.

## 格位映射

| 格位 | 运行语义 |
| --- | --- |
| 1 | 军械局协议模块 |
| 2 | 侦察组协议模块 |
| 3 | 后勤组协议模块 |
| 4 | 工程组协议模块 |
| 5 | 环绕无人机 Mk.II |
| 6 | 侧挂速射炮 Mk.II |
| 7 | 履带布雷器 Mk.II |
| 8 | 区域压制器 Mk.II |
| 9 | 环绕无人机 Mk.III |
| 10 | 侧挂速射炮 Mk.III |
| 11 | 履带布雷器 Mk.III |
| 12 | 区域压制器 Mk.III |

## Mk.III 辅助重生成提示词

首张 3×4 母版的第三行未通过分格边缘检查，因此第三行不作为运行素材；四项 Mk.III 辅助改为独立 1×4 紧凑道具条重新生成：

Create a top-down pixel-art prop strip containing exactly four Mk.III evolved auxiliary modules for the heavy sci-fi wasteland tank shown in the references. Preserve the identity, silhouette lineage, dark crisp outlines, brass/steel mechanical material language, cyan energy accents, and compact FC-arcade readability of each referenced auxiliary. Use the shown layout guide only to understand four equal invisible cells, centering, spacing, and safe padding. Do not reproduce the guide boxes, lines, labels, borders, white background, or center marks.

Exactly one row with four independent compact props, left to right: (1) evolved command orbit drone, clearly descended from the small round drone, larger core, armored fins, compact cyan energy ring; (2) evolved heavy rotary side cannon, clearly descended from the side cannon, multiple short barrels and reinforced mount; (3) evolved heavy rear mine-layer, clearly descended from the mine layer, armored multi-port dispenser and visible compact magazine; (4) evolved complete suppression-field generator, clearly descended from the suppression unit, bright cyan core and four short industrial emitters.

Directly top-down view. One isolated module per cell. No tank body, no ground, no shadow plate, no pedestal, no UI, no text, no labels, no numbers, no detached bullets, no smoke. Every module must fit fully inside the central 55% of its cell with generous magenta margin on all four sides. No glow, fin, barrel, emitter, or pixel may cross a cell edge. Keep a consistent pixel scale and top-down perspective. Background must be exactly 100% flat solid `#FF00FF` with no gradient or texture.
