using System;
using Godot;

namespace Game1;

/// <summary>Boss 的静态配置；运行时生命、阶段与表现均不写回此资源。</summary>
[GlobalClass]
public partial class BossDefinition : Resource
{
    [Export] public PackedScene Scene { get; set; }
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public int MaximumHealth { get; set; }
    [Export] public Vector2I GridSize { get; set; }
    [Export] public int CellSize { get; set; }
    [Export] public float BarrierIntervalSeconds { get; set; } = 3.4f;
    [Export] public float ThreatIntervalSeconds { get; set; } = 4.6f;
    [Export] public float ChargeIntervalSeconds { get; set; } = 1.8f;
    [Export] public float ChargeTelegraphSeconds { get; set; } = 0.85f;
    [Export] public float VulnerableSeconds { get; set; } = 2f;

    public void Validate()
    {
        if (Scene is null) throw new InvalidOperationException("BossDefinition 缺少场景引用。");
        if (string.IsNullOrWhiteSpace(DisplayName)) throw new InvalidOperationException("BossDefinition 缺少显示名称。");
        if (MaximumHealth <= 0) throw new InvalidOperationException("BossDefinition 最大生命必须为正数。");
        if (GridSize.X <= 0 || GridSize.Y <= 0) throw new InvalidOperationException("BossDefinition 的导航网格尺寸必须为正数。");
        if (CellSize <= 0) throw new InvalidOperationException("BossDefinition 的导航格宽必须为正数。");
        if (!float.IsFinite(BarrierIntervalSeconds) || BarrierIntervalSeconds <= 0f ||
            !float.IsFinite(ThreatIntervalSeconds) || ThreatIntervalSeconds <= 0f ||
            !float.IsFinite(ChargeIntervalSeconds) || ChargeIntervalSeconds <= ChargeTelegraphSeconds ||
            !float.IsFinite(ChargeTelegraphSeconds) || ChargeTelegraphSeconds <= 0f ||
            !float.IsFinite(VulnerableSeconds) || VulnerableSeconds < 1.8f)
            throw new InvalidOperationException("BossDefinition 的阶段节奏必须为正数，且保留可读预警与不少于 1.8 秒弱点窗口。");
    }
}
