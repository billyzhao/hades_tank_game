"""生成《废土中继》封锁城区自有程序化音频。

不使用第三方采样；相同脚本与参数会生成相同 PCM WAV。
"""

from __future__ import annotations

import json
import math
import random
import struct
import wave
from pathlib import Path
from typing import Callable


ROOT = Path(__file__).resolve().parent
OUTPUT = ROOT / "generated"
SAMPLE_RATE = 22_050
TAU = math.tau


def clamp(value: float) -> float:
    return max(-0.96, min(0.96, value))


def envelope(t: float, duration: float, attack: float = 0.008, release: float = 0.12) -> float:
    attack_gain = min(1.0, t / max(attack, 0.0001))
    release_gain = min(1.0, (duration - t) / max(release, 0.0001))
    return max(0.0, min(attack_gain, release_gain))


def sine(frequency: float, t: float, phase: float = 0.0) -> float:
    return math.sin(TAU * frequency * t + phase)


def square(frequency: float, t: float) -> float:
    return 1.0 if sine(frequency, t) >= 0.0 else -1.0


def triangle(frequency: float, t: float) -> float:
    return 2.0 * abs(2.0 * ((frequency * t) % 1.0) - 1.0) - 1.0


def write_mono(
    name: str,
    duration: float,
    seed: int,
    sample: Callable[[float, random.Random], float],
    *,
    loop: bool = False,
) -> dict:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    path = OUTPUT / name
    rng = random.Random(seed)
    frames = bytearray()
    count = max(1, round(duration * SAMPLE_RATE))
    for index in range(count):
        t = index / SAMPLE_RATE
        value = clamp(sample(t, rng))
        frames.extend(struct.pack("<h", round(value * 32767)))
    with wave.open(str(path), "wb") as target:
        target.setnchannels(1)
        target.setsampwidth(2)
        target.setframerate(SAMPLE_RATE)
        target.writeframes(frames)
    return manifest_entry(path, duration, 1, loop=loop)


def boundary_blend(samples: list[tuple[float, float]], seconds: float = 0.045) -> None:
    blend_count = min(round(seconds * SAMPLE_RATE), len(samples) // 4)
    if blend_count <= 1:
        return
    starts = samples[:blend_count]
    ends = samples[-blend_count:]
    for index in range(blend_count):
        mix = index / (blend_count - 1)
        left = ends[index][0] * (1.0 - mix) + starts[index][0] * mix
        right = ends[index][1] * (1.0 - mix) + starts[index][1] * mix
        samples[index] = (left, right)
        samples[-blend_count + index] = (left, right)


def write_stereo_loop(
    name: str,
    duration: float,
    seed: int,
    sample: Callable[[float, random.Random], tuple[float, float]],
) -> dict:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    path = OUTPUT / name
    rng = random.Random(seed)
    values = [sample(index / SAMPLE_RATE, rng) for index in range(round(duration * SAMPLE_RATE))]
    boundary_blend(values)
    frames = bytearray()
    for left, right in values:
        frames.extend(struct.pack("<hh", round(clamp(left) * 32767), round(clamp(right) * 32767)))
    with wave.open(str(path), "wb") as target:
        target.setnchannels(2)
        target.setsampwidth(2)
        target.setframerate(SAMPLE_RATE)
        target.writeframes(frames)
    return manifest_entry(path, duration, 2, loop=True)


def manifest_entry(path: Path, duration: float, channels: int, loop: bool = False) -> dict:
    return {
        "file": path.name,
        "sample_rate": SAMPLE_RATE,
        "channels": channels,
        "duration_seconds": duration,
        "loop": loop,
        "source": "project-owned deterministic procedural synthesis",
    }


def sweep(start: float, end: float, duration: float, noise: float, body: float = 0.55):
    def render(t: float, rng: random.Random) -> float:
        progress = min(1.0, t / duration)
        frequency = start * ((end / start) ** progress)
        tonal = triangle(frequency, t) * 0.55 + sine(frequency * 0.5, t) * 0.45
        grit = rng.uniform(-1.0, 1.0)
        return (tonal * (1.0 - noise) + grit * noise) * envelope(t, duration, 0.003, duration * 0.48) * body
    return render


def pulse_tone(base: float, duration: float, pulses: int, noise: float = 0.1):
    def render(t: float, rng: random.Random) -> float:
        local = (t * pulses) % 1.0
        gate = max(0.0, 1.0 - local * 1.5)
        tone = square(base * (1.0 + 0.08 * math.sin(t * TAU * pulses)), t) * 0.35
        tone += sine(base * 0.5, t) * 0.3
        return (tone + rng.uniform(-1.0, 1.0) * noise) * gate * envelope(t, duration, 0.004, 0.06)
    return render


def music_note(midi: int) -> float:
    return 440.0 * (2.0 ** ((midi - 69) / 12.0))


def combat_base(t: float, rng: random.Random) -> tuple[float, float]:
    beat = 0.5
    beat_index = int(t / beat)
    local = (t % beat) / beat
    bass_notes = (38, 38, 41, 36, 38, 43, 41, 36)
    freq = music_note(bass_notes[beat_index % len(bass_notes)])
    bass = triangle(freq, t) * math.exp(-local * 3.2) * 0.25
    kick_phase = t % beat
    kick_freq = 70.0 * (0.45 + 0.55 * math.exp(-kick_phase * 20.0))
    kick = sine(kick_freq, t) * math.exp(-kick_phase * 13.0) * 0.32
    machine = (rng.uniform(-1.0, 1.0) * 0.025 + sine(27.5, t) * 0.04)
    pan = sine(0.125, t) * 0.06
    return bass + kick + machine + pan, bass + kick + machine - pan


def combat_intensity(t: float, rng: random.Random) -> tuple[float, float]:
    eighth = 0.25
    step = int(t / eighth)
    local = (t % eighth) / eighth
    notes = (62, 65, 67, 65, 62, 69, 67, 60)
    freq = music_note(notes[step % len(notes)])
    lead = square(freq, t) * math.exp(-local * 4.0) * 0.11
    metal = rng.uniform(-1.0, 1.0) * math.exp(-local * 18.0) * (0.08 if step % 2 else 0.14)
    pulse = sine(110.0, t) * math.exp(-local * 7.0) * 0.08
    return lead + metal + pulse * 0.6, lead * 0.82 - metal * 0.75 + pulse


def boss_music(t: float, rng: random.Random) -> tuple[float, float]:
    beat = 0.5
    index = int(t / beat)
    local = (t % beat) / beat
    notes = (31, 31, 34, 30, 31, 36, 34, 29)
    freq = music_note(notes[index % len(notes)])
    drone = sine(freq, t) * 0.22 + triangle(freq * 2.0, t) * 0.12
    stomp = sine(55.0 * (1.0 - 0.35 * local), t) * math.exp(-local * 9.0) * 0.35
    siren = sine(185.0 + 22.0 * sine(0.25, t), t) * 0.07
    grit = rng.uniform(-1.0, 1.0) * math.exp(-local * 15.0) * 0.09
    return drone + stomp + siren + grit, drone + stomp - siren - grit * 0.7


def ambience(t: float, rng: random.Random) -> tuple[float, float]:
    gust = 0.08 + 0.05 * (0.5 + 0.5 * sine(0.125, t))
    wind = rng.uniform(-1.0, 1.0) * gust
    machinery = sine(28.0, t) * 0.055 + sine(56.0, t) * 0.018
    distant = sine(93.0 + 8.0 * sine(0.25, t), t) * 0.018
    pan = sine(0.0625, t) * 0.04
    return wind + machinery + distant + pan, wind * 0.82 + machinery - distant - pan


def generate() -> list[dict]:
    items: list[dict] = []
    add = items.append

    add(write_mono("player_track_loop.wav", 1.2, 101, pulse_tone(52, 1.2, 9, 0.16), loop=True))
    add(write_mono("player_fire_01.wav", 0.16, 102, sweep(145, 45, 0.16, 0.44, 0.68)))
    add(write_mono("player_fire_02.wav", 0.16, 103, sweep(155, 48, 0.16, 0.40, 0.66)))
    add(write_mono("player_fire_03.wav", 0.17, 104, sweep(132, 41, 0.17, 0.48, 0.70)))
    add(write_mono("player_dash.wav", 0.28, 105, sweep(680, 90, 0.28, 0.34, 0.58)))
    add(write_mono("player_hit.wav", 0.22, 106, sweep(920, 110, 0.22, 0.67, 0.65)))
    add(write_mono("armor_low.wav", 0.62, 107, pulse_tone(210, 0.62, 4, 0.04)))
    add(write_mono("reboot_start.wav", 1.05, 108, sweep(70, 520, 1.05, 0.12, 0.45)))
    add(write_mono("reboot_complete.wav", 0.48, 109, sweep(180, 880, 0.48, 0.08, 0.48)))

    add(write_mono("enemy_scout_fire.wav", 0.11, 201, sweep(720, 260, 0.11, 0.24, 0.47)))
    add(write_mono("enemy_patrol_fire.wav", 0.16, 202, sweep(220, 72, 0.16, 0.42, 0.58)))
    add(write_mono("enemy_assault_fire.wav", 0.18, 203, sweep(170, 48, 0.18, 0.55, 0.64)))
    add(write_mono("enemy_mortar_fire.wav", 0.34, 204, sweep(105, 36, 0.34, 0.62, 0.72)))
    add(write_mono("spawn_warning.wav", 0.52, 205, pulse_tone(340, 0.52, 5, 0.06)))
    add(write_mono("enemy_destroy.wav", 0.42, 206, sweep(190, 28, 0.42, 0.76, 0.78)))
    add(write_mono("elite_overdrive.wav", 0.72, 207, pulse_tone(115, 0.72, 8, 0.18)))

    add(write_mono("boss_intro.wav", 1.25, 301, sweep(55, 180, 1.25, 0.38, 0.58)))
    add(write_mono("boss_barrier.wav", 0.46, 302, sweep(410, 62, 0.46, 0.58, 0.65)))
    add(write_mono("boss_turret.wav", 0.17, 303, sweep(240, 58, 0.17, 0.46, 0.62)))
    add(write_mono("boss_charge_warning.wav", 0.78, 304, pulse_tone(275, 0.78, 6, 0.08)))
    add(write_mono("boss_charge.wav", 0.55, 305, sweep(120, 38, 0.55, 0.52, 0.74)))
    add(write_mono("boss_weakpoint.wav", 0.62, 306, sweep(260, 960, 0.62, 0.06, 0.44)))
    add(write_mono("boss_phase.wav", 0.92, 307, sweep(82, 620, 0.92, 0.30, 0.58)))
    add(write_mono("boss_destroy.wav", 1.35, 308, sweep(150, 22, 1.35, 0.78, 0.82)))

    add(write_mono("ui_move.wav", 0.06, 401, sweep(620, 760, 0.06, 0.02, 0.24)))
    add(write_mono("ui_confirm.wav", 0.13, 402, sweep(420, 840, 0.13, 0.02, 0.32)))
    add(write_mono("ui_level_up.wav", 0.55, 403, sweep(280, 1080, 0.55, 0.03, 0.38)))
    add(write_mono("ui_maintenance.wav", 0.42, 404, sweep(210, 720, 0.42, 0.04, 0.34)))
    add(write_mono("ui_failure.wav", 0.95, 405, sweep(170, 42, 0.95, 0.30, 0.52)))
    add(write_mono("ui_victory.wav", 1.0, 406, sweep(190, 1240, 1.0, 0.03, 0.42)))

    add(write_stereo_loop("ambience_blockade_city.wav", 8.0, 501, ambience))
    add(write_stereo_loop("music_combat_base.wav", 8.0, 502, combat_base))
    add(write_stereo_loop("music_combat_intensity.wav", 8.0, 503, combat_intensity))
    add(write_stereo_loop("music_boss.wav", 8.0, 504, boss_music))
    return items


if __name__ == "__main__":
    manifest = {
        "batch": "batch-09-blockade-city-audio",
        "generated_by": "generate_audio.py",
        "license": "project-owned; no third-party samples",
        "assets": generate(),
    }
    (ROOT / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"generated {len(manifest['assets'])} audio files in {OUTPUT}")
