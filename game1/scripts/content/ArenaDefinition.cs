using System;
using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>单个竞技场的只读内容入口，集中声明场景、导航尺度与五波配置。</summary>
[GlobalClass]
public partial class ArenaDefinition : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public int ArenaIndex { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public Vector2I GridSize { get; set; }
    [Export] public int CellSize { get; set; }
    [Export] public Godot.Collections.Array<WaveDefinition> Waves { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || !string.Equals(Id, Id.Trim(), StringComparison.Ordinal))
            throw new InvalidOperationException("ArenaDefinition.Id 必须是无首尾空白的稳定标识。");
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new InvalidOperationException($"竞技场 '{Id}' 缺少可读名称。");
        if (ArenaIndex is < 0 or > 4)
            throw new InvalidOperationException($"竞技场 '{Id}' 的索引必须位于 0～4。");
        if (Scene is null)
            throw new InvalidOperationException($"竞技场 '{Id}' 缺少场景引用。");
        if (GridSize.X <= 0 || GridSize.Y <= 0 || CellSize <= 0)
            throw new InvalidOperationException($"竞技场 '{Id}' 的导航尺度必须为正数。");
        if (Waves is null || Waves.Count != 5)
            throw new InvalidOperationException($"竞技场 '{Id}' 必须恰有五波。");

        IReadOnlyList<WaveSchedule> approved = WaveSchedule.CreateApproved();
        HashSet<int> waveNumbers = new();
        for (int index = 0; index < Waves.Count; index++)
        {
            WaveDefinition wave = Waves[index] ??
                throw new InvalidOperationException($"竞技场 '{Id}' 包含空波次引用。");
            wave.Validate();
            if (!waveNumbers.Add(wave.WaveNumber) || wave.WaveNumber != index + 1)
                throw new InvalidOperationException($"竞技场 '{Id}' 的波号必须按 1～5 唯一排列。");
            if (Math.Abs(wave.SpawnDurationSeconds - approved[index].SpawnDurationSeconds) > 0.001d)
                throw new InvalidOperationException($"第 {wave.WaveNumber} 波必须使用已确认的 {approved[index].SpawnDurationSeconds:0} 秒时长。");
            if (wave.RewardKind != approved[index].RewardKind)
                throw new InvalidOperationException($"第 {wave.WaveNumber} 波奖励顺序不符合已确认方案。");
            if (wave.IncludesElite != approved[index].IncludesElite)
                throw new InvalidOperationException("只有第 5 波必须包含且只能包含一个精英槽位。");
        }
    }

    public WaveDefinition GetWave(int waveNumber)
    {
        if (waveNumber is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(waveNumber));
        Validate();
        return Waves[waveNumber - 1];
    }
}
