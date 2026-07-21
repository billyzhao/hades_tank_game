using System.Collections.Generic;

namespace Game1;

/// <summary>五波状态机使用的纯 C# 只读值；Godot WaveDefinition 负责提供生成内容。</summary>
public readonly record struct WaveSchedule(
    int WaveNumber,
    double SpawnDurationSeconds,
    RewardKind RewardKind,
    bool IncludesElite)
{
    public static IReadOnlyList<WaveSchedule> CreateApproved() =>
    [
        new(1, 45d, RewardKind.NormalProtocol, false),
        new(2, 50d, RewardKind.Maintenance, false),
        new(3, 55d, RewardKind.NormalProtocol, false),
        new(4, 60d, RewardKind.Maintenance, false),
        new(5, 70d, RewardKind.RareProtocol, true)
    ];
}
