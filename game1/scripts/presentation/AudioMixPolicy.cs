using System;

namespace Game1;

/// <summary>不依赖 Godot 节点的音量、危险阈值与职责映射，便于纯 C# 验证。</summary>
public static class AudioMixPolicy
{
    public const float SilenceDb = -80f;

    public static float LinearToDecibels(float linear)
    {
        if (!float.IsFinite(linear)) throw new ArgumentOutOfRangeException(nameof(linear));
        if (linear <= 0f) return SilenceDb;
        float clamped = Math.Clamp(linear, 0.0001f, 1f);
        return Math.Max(SilenceDb, 20f * MathF.Log10(clamped));
    }

    public static float CombatIntensityDb(int waveNumber) => waveNumber switch
    {
        <= 1 => -30f,
        2 => -24f,
        3 => -19f,
        4 => -15f,
        _ => -11f
    };

    public static bool IsLowArmor(int current, int maximum) =>
        maximum > 0 && current > 0 && current * 10 <= maximum * 3;

    public static AudioCue EnemyFireCue(BehaviorId behavior) => behavior switch
    {
        BehaviorId.Scout => AudioCue.EnemyScoutFire,
        BehaviorId.Patrol => AudioCue.EnemyPatrolFire,
        BehaviorId.Assault => AudioCue.EnemyAssaultFire,
        BehaviorId.Mortar => AudioCue.EnemyMortarFire,
        _ => AudioCue.EnemyPatrolFire
    };
}
