using System;
using Godot;

namespace Game1;

/// <summary>一波限时增援的只读配置；导演读取它，但不能在运行时改写资源。</summary>
[GlobalClass]
public partial class WaveDefinition : Resource
{
    [Export] public int WaveNumber { get; set; }
    [Export] public double SpawnDurationSeconds { get; set; }
    [Export] public float SpawnIntervalSeconds { get; set; }
    [Export] public int MaximumAliveEnemies { get; set; }
    [Export] public float MinimumPlayerDistance { get; set; }
    [Export] public RewardKind RewardKind { get; set; }
    [Export] public bool IncludesElite { get; set; }
    [Export] public Godot.Collections.Array<BehaviorId> Behaviors { get; set; } = new();

    public void Validate()
    {
        if (WaveNumber is < 1 or > 5)
            throw new InvalidOperationException("WaveDefinition.WaveNumber 必须位于 1～5。");
        if (!double.IsFinite(SpawnDurationSeconds) || SpawnDurationSeconds <= 0d)
            throw new InvalidOperationException($"第 {WaveNumber} 波刷新时长必须是正有限值。");
        if (!float.IsFinite(SpawnIntervalSeconds) || SpawnIntervalSeconds <= 0f)
            throw new InvalidOperationException($"第 {WaveNumber} 波生成间隔必须是正有限值。");
        if (MaximumAliveEnemies <= 0)
            throw new InvalidOperationException($"第 {WaveNumber} 波场上敌军上限必须为正数。");
        if (!float.IsFinite(MinimumPlayerDistance) || MinimumPlayerDistance < 0f)
            throw new InvalidOperationException($"第 {WaveNumber} 波玩家安全距离无效。");
        if (!Enum.IsDefined(RewardKind))
            throw new InvalidOperationException($"第 {WaveNumber} 波奖励种类无效。");
        if (Behaviors is null || Behaviors.Count == 0)
            throw new InvalidOperationException($"第 {WaveNumber} 波至少需要一种现有敌军行为。");
        foreach (BehaviorId behavior in Behaviors)
        {
            if (!Enum.IsDefined(behavior))
                throw new InvalidOperationException($"第 {WaveNumber} 波包含无效敌军行为。");
        }
    }
}
