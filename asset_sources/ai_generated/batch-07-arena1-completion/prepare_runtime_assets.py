"""将已通过透明边缘检查的 2×2 源图裁切为《废土中继》首区运行素材。

本脚本只做确定性的透明边界裁切、最近邻缩放、居中和图集拼接，
不生成或重绘任何美术内容。
"""

from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[3]
BATCH = Path(__file__).resolve().parent
GAME_ASSETS = ROOT / "game1" / "assets" / "sprites"


def fit_frame(source: Path, destination: Path, size: tuple[int, int], padding: int = 1) -> None:
    image = Image.open(source).convert("RGBA")
    bounds = image.getbbox()
    if bounds is None:
        raise ValueError(f"源图没有可见像素：{source}")

    subject = image.crop(bounds)
    maximum_width = max(1, size[0] - padding * 2)
    maximum_height = max(1, size[1] - padding * 2)
    scale = min(maximum_width / subject.width, maximum_height / subject.height)
    output_width = max(1, round(subject.width * scale))
    output_height = max(1, round(subject.height * scale))
    subject = subject.resize((output_width, output_height), Image.Resampling.NEAREST)

    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    x = (size[0] - output_width) // 2
    y = (size[1] - output_height) // 2
    canvas.alpha_composite(subject, (x, y))
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination)


def assemble_tileset(steel: Path, brick: Path, destination: Path) -> None:
    canvas = Image.new("RGBA", (48, 24), (0, 0, 0, 0))
    canvas.alpha_composite(Image.open(steel).convert("RGBA"), (0, 0))
    canvas.alpha_composite(Image.open(brick).convert("RGBA"), (24, 0))
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination)


def main() -> None:
    boss = BATCH / "boss-processed"
    terrain = BATCH / "terrain-processed"
    auxiliary = BATCH / "auxiliary-processed"
    ui = BATCH / "ui-v2-processed"

    fit_frame(boss / "boss_part-1.png", GAME_ASSETS / "bosses" / "roadblock_commander_hull.png", (64, 64), 2)
    fit_frame(boss / "boss_part-2.png", GAME_ASSETS / "bosses" / "roadblock_commander_turret.png", (48, 32), 1)
    fit_frame(boss / "boss_part-3.png", GAME_ASSETS / "bosses" / "roadblock_commander_weakpoint.png", (36, 24), 1)
    fit_frame(boss / "boss_part-4.png", GAME_ASSETS / "bosses" / "roadblock_gun_emplacement.png", (28, 28), 1)

    terrain_dir = GAME_ASSETS / "terrain"
    fit_frame(terrain / "terrain_module-1.png", terrain_dir / "blockade_steel.png", (24, 24))
    fit_frame(terrain / "terrain_module-2.png", terrain_dir / "blockade_brick.png", (24, 24))
    fit_frame(terrain / "terrain_module-3.png", terrain_dir / "roadblock_barrier.png", (48, 24))
    fit_frame(terrain / "terrain_module-4.png", terrain_dir / "spawn_beacon.png", (24, 24))
    assemble_tileset(
        terrain_dir / "blockade_steel.png",
        terrain_dir / "blockade_brick.png",
        terrain_dir / "blockade_city_tileset.png",
    )

    auxiliary_dir = GAME_ASSETS / "auxiliaries"
    fit_frame(auxiliary / "auxiliary-1.png", auxiliary_dir / "orbit_drone.png", (18, 18))
    fit_frame(auxiliary / "auxiliary-2.png", auxiliary_dir / "side_cannon.png", (18, 18))
    fit_frame(auxiliary / "auxiliary-3.png", auxiliary_dir / "mine_layer.png", (18, 18))
    fit_frame(auxiliary / "auxiliary-4.png", auxiliary_dir / "suppression_field.png", (18, 18))

    ui_dir = GAME_ASSETS / "ui"
    fit_frame(ui / "ui_frame-1.png", ui_dir / "hud_status_frame.png", (160, 48), 1)
    fit_frame(ui / "ui_frame-2.png", ui_dir / "experience_frame.png", (176, 28), 1)
    fit_frame(ui / "ui_frame-3.png", ui_dir / "reward_card_frame.png", (104, 140), 1)
    fit_frame(ui / "ui_frame-4.png", ui_dir / "boss_status_frame.png", (224, 40), 1)


if __name__ == "__main__":
    main()
