using System;
using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>集中维护 Batch 09 的运行路径；表现控制器不散落字符串资源引用。</summary>
public static class AudioCueCatalog
{
    private const string Root = "res://assets/audio/blockade_city/";

    private static readonly IReadOnlyDictionary<AudioCue, string[]> Files =
        new Dictionary<AudioCue, string[]>
        {
            [AudioCue.PlayerTrack] = new[] { "player_track_loop.wav" },
            [AudioCue.PlayerFire] = new[] { "player_fire_01.wav", "player_fire_02.wav", "player_fire_03.wav" },
            [AudioCue.PlayerDash] = new[] { "player_dash.wav" },
            [AudioCue.PlayerHit] = new[] { "player_hit.wav" },
            [AudioCue.ArmorLow] = new[] { "armor_low.wav" },
            [AudioCue.RebootStart] = new[] { "reboot_start.wav" },
            [AudioCue.RebootComplete] = new[] { "reboot_complete.wav" },
            [AudioCue.EnemyScoutFire] = new[] { "enemy_scout_fire.wav" },
            [AudioCue.EnemyPatrolFire] = new[] { "enemy_patrol_fire.wav" },
            [AudioCue.EnemyAssaultFire] = new[] { "enemy_assault_fire.wav" },
            [AudioCue.EnemyMortarFire] = new[] { "enemy_mortar_fire.wav" },
            [AudioCue.SpawnWarning] = new[] { "spawn_warning.wav" },
            [AudioCue.EnemyDestroy] = new[] { "enemy_destroy.wav" },
            [AudioCue.EliteOverdrive] = new[] { "elite_overdrive.wav" },
            [AudioCue.BossIntro] = new[] { "boss_intro.wav" },
            [AudioCue.BossBarrier] = new[] { "boss_barrier.wav" },
            [AudioCue.BossTurret] = new[] { "boss_turret.wav" },
            [AudioCue.BossChargeWarning] = new[] { "boss_charge_warning.wav" },
            [AudioCue.BossCharge] = new[] { "boss_charge.wav" },
            [AudioCue.BossWeakpoint] = new[] { "boss_weakpoint.wav" },
            [AudioCue.BossPhase] = new[] { "boss_phase.wav" },
            [AudioCue.BossDestroy] = new[] { "boss_destroy.wav" },
            [AudioCue.UiMove] = new[] { "ui_move.wav" },
            [AudioCue.UiConfirm] = new[] { "ui_confirm.wav" },
            [AudioCue.UiLevelUp] = new[] { "ui_level_up.wav" },
            [AudioCue.UiMaintenance] = new[] { "ui_maintenance.wav" },
            [AudioCue.UiFailure] = new[] { "ui_failure.wav" },
            [AudioCue.UiVictory] = new[] { "ui_victory.wav" },
            [AudioCue.Ambience] = new[] { "ambience_blockade_city.wav" },
            [AudioCue.CombatBase] = new[] { "music_combat_base.wav" },
            [AudioCue.CombatIntensity] = new[] { "music_combat_intensity.wav" },
            [AudioCue.BossMusic] = new[] { "music_boss.wav" }
        };

    public static int RequiredFileCount
    {
        get
        {
            int count = 0;
            foreach (string[] files in Files.Values) count += files.Length;
            return count;
        }
    }

    public static IEnumerable<string> RequiredPaths()
    {
        foreach (string[] files in Files.Values)
        foreach (string file in files)
            yield return Root + file;
    }

    public static AudioStream[] Load(AudioCue cue)
    {
        if (!Files.TryGetValue(cue, out string[] files))
            throw new ArgumentOutOfRangeException(nameof(cue), cue, "未登记音频语义。");
        AudioStream[] streams = new AudioStream[files.Length];
        for (int index = 0; index < files.Length; index++)
        {
            string path = Root + files[index];
            streams[index] = GD.Load<AudioStream>(path)
                ?? throw new InvalidOperationException($"无法加载音频资源：{path}");
        }
        return streams;
    }

    public static AudioStream LoadLoop(AudioCue cue)
    {
        AudioStream source = Load(cue)[0];
        if (source is not AudioStreamWav wav) return source;
        AudioStreamWav loop = (AudioStreamWav)wav.Duplicate();
        loop.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        return loop;
    }
}
