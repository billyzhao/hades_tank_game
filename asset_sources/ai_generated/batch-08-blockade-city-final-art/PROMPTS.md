# Batch 08 完整生成提示词

生成工具：Codex 内置 `image_gen`。所有源图使用固定洋红底，运行素材仅由确定性去底、分格、缩放和命名脚本加工。

## `player-components-2x3-raw.png`

Use case: stylized-concept. Asset type: production sprite component sheet for a top-down Godot tank game. Use the approved Batch 01 v2 tank image as the strict visual reference for silhouette family, heavy mechanical construction, sand-gold armor, dark steel tracks, cyan energy core, crisp pixel-arcade readability, and detailed sci-fi material language. Create exactly six isolated sprite components in an exact 2 rows × 3 columns grid: turretless heavy tracked hull facing upward; separate cannon turret facing upward; compact neutral cyan core housing overlay; breakthrough heavy-artillery core overlay with reinforced bronze armor and a powerful cyan capacitor; overload rapid-fire core overlay with orange-red heat fins and a pulsing reactor; electric ranger core overlay with cyan-blue coils and angular mobility vanes. Each cell contains one isolated component only. High-quality modern pixel-art-inspired game sprite, heavier and mechanically detailed Hades-like sci-fi tank language, bright yellow-sand industrial palette, crisp dark outlines. Components centered in six equal invisible cells with generous padding. Perfectly flat solid `#FF00FF` background, no texture, shadows, gradients, borders, labels or separators. No text, UI or watermark; nothing crosses a cell edge; accents remain readable after downscaling to 48 pixels.

## `scout-drone-raw.png`

Use case: stylized-concept. Asset type: production enemy sprite for a top-down Godot tank arena. Use the Batch 06 enemy roster as the strict reference for pixel density, top-down perspective, dark industrial armor, orange hostile markings, purple/cyan electronics, crisp outline and gameplay scale. Create one isolated light reconnaissance drone vehicle: compact diamond/oval hover chassis, two side thruster pods, forward sensor eye, short light weapon, exposed antenna and orange hostile panels. No tracks, no heavy cannon and no patrol-tank silhouette. High-quality modern pixel-art-inspired sci-fi sprite, exact matching top-down/three-quarter angle, facing upward, readable around 24–28 pixels. Perfectly flat solid `#FF00FF` background. One sprite only; no text, UI, watermark, shadow, particles or detached effects.

## `ui-icons-4x4-raw.png`

Use case: stylized-concept. Asset type: production 4×4 HUD icon atlas. Use Batch 07 industrial UI frames as the material/palette reference and the 4×4 guide only for invisible slot geometry. Create exactly 16 icons: row 1 armored shield, reboot arrow around cyan core, wave/radar pulse, elite chevron skull; row 2 breakthrough capacitor core, overload rapid-fire heat core, electric ranger coil core, auxiliary socket; row 3 shell damage, rapid-fire rounds, projectile velocity, tracked movement; row 4 arsenal crossed cannons, engineering gear/armor, reconnaissance radar eye, logistics wrench/crate. Dark gunmetal, sand-gold trim, cyan energy and orange danger accent, readable at 12–18 pixels. Perfectly flat `#FF00FF` background; no visible grid, text, numbers, frames or watermark; symbols must differ by shape rather than color alone.

加工结论：V1 的移动与四部门图标触碰分格边缘，已拒绝作为运行素材。

## `ui-icons-4x4-v2-raw.png`

沿用 V1 的 16 图标顺序、工业语言和 4×4 分格，要求每个完整图标（包括武器尖端、速度线、天线、扳手和箱体）只占单格中央约 55%，四边至少保留 20%～22% 的纯洋红安全边距；第四行必须显著缩小。安全边距优先于装饰细节，其余无文字、无边框、固定 `#FF00FF` 背景约束不变。

## `combat-vfx-4x4-raw.png`

Use case: stylized-concept. Asset type: production 4×4 animated combat VFX atlas. Use Batch 06 combat VFX as the pixel-density and palette reference and the 4×4 guide only for invisible geometry. Four horizontal four-frame sequences: row 1 cannon muzzle flash (ignition, cone, peak, ember fade); row 2 steel impact (spark, shards, peak contact, fading fragments); row 3 enemy destruction (ignition, expanding fireball, peak fire/smoke/fragments, collapsing smoke); row 4 yellow-sand tread dust (small puff, twin lobes, broad cloud, fade). Crisp modern pixel-art-inspired gameplay VFX readable at 12–28 pixels. Perfectly flat `#FF00FF` background; no text, UI, vehicles or scenery; every effect remains inside its cell.

## `player-state-vfx-4x4-raw.png`

Use case: stylized-concept. Asset type: production 4×4 player-state VFX atlas. Four horizontal four-frame sequences: row 1 cyan dash trail; row 2 armor hit/break sparks and shield fragments; row 3 reboot ring around a central cyan core; row 4 cyan/gold level-up circuit burst. Crisp modern pixel-art-inspired VFX, gunmetal fragments, cyan energy, amber highlights and orange danger accents, readable at 16–32 pixels. Perfectly flat `#FF00FF` background; no text, UI, vehicle body or scenery; all effects contained within equal invisible cells.

## `warning-vfx-4x4-raw.png`

Use case: stylized-concept. Asset type: production 4×4 ground-warning VFX atlas. Use the confirmed yellow-sand foundation as contrast/perspective context and the guide only for invisible geometry. Four horizontal four-frame sequences: row 1 segmented enemy spawn ring; row 2 contracting mortar target; row 3 rectangular roadblock deployment footprint; row 4 right-facing Boss charge corridor. Industrial holographic amber/orange/red warnings with small cyan accents, readable over yellow sand and distinguishable by geometry. Perfectly flat `#FF00FF` output background; no sand baked in, text, labels, UI, vehicles, walls or scenery; nothing crosses a cell edge.

## `boss-vfx-2x4-raw.png`

Use case: stylized-concept. Asset type: production 2×4 Boss VFX atlas. Use the Batch 07 Roadblock Commander as the strict gunmetal, rust-orange, cyan-reactor and hazard-stripe reference and the 2×4 guide only for invisible geometry. Top row: phase-transition sequence from broken ring through energy build and shockwave to stable exposed weakpoint. Bottom row: heavy destruction from ignition through cyan reactor rupture and peak multi-lobed fireball to collapsing smoke and scorched debris. Mechanically shaped modern pixel-art-inspired VFX. Perfectly flat `#FF00FF` background; no complete tank, text, labels, UI or watermark; all fragments contained.

加工结论：V1 两个峰值帧越格，已拒绝作为运行素材。

## `boss-vfx-2x4-v2-raw.png`

沿用 V1 的两行序列和 Boss 材质语言，要求每个完整效果（包括火花、烟团、冲击波、电缆和碎片）只占单格中央 55%～60%，四边至少保留 18%～20% 的纯洋红安全边距；第三列两个峰值帧必须明显缩小。安全边距优先于效果尺寸，其余固定 `#FF00FF`、无坦克本体、无文字约束不变。

## `combat-data-2x2-raw.png`

Use case: stylized-concept. Asset type: production 2×2 animated combat-data pickup. The same compact dark-gunmetal rectangular microchip with gold contacts and cyan circuit core in all four frames: dim idle, cyan pulse, bright scan lines, return to dim. High-quality modern pixel-art-inspired pickup readable at 10–14 pixels. Exact 2×2 invisible cells, same size/orientation/center, perfectly flat `#FF00FF` background, no text, labels, UI, watermark or scenery.

## `projectiles-2x4-raw.png`

Create a production-ready pixel-art sprite sheet for the top-down Godot game 《废土中继》, matching the approved wasteland-industrial combat VFX style and the exact 2 columns × 4 rows layout guide. Exactly eight equal cells: the left column is four sequential frames of one compact player shell with cyan energy core and brass-gold steel casing; the right column is four sequential frames of one compact enemy shell with hot orange-red core and dark steel casing. All projectiles point upward, remain centered with generous safe padding, and contain no muzzle flash, impact burst, smoke or cross-cell particles. Crisp pixel arcade art readable at 24×24, hard edges, no antialiasing, gradients, text or borders. Uniform flat `#FF00FF` background.

加工结论：四组玩家帧和四组敌军帧均通过 `edge_touch_frames: []` 检查，按奇数/偶数帧分别进入运行序列。
