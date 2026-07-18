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

    public void Validate()
    {
        if (Scene is null) throw new InvalidOperationException("BossDefinition 缺少场景引用。");
        if (string.IsNullOrWhiteSpace(DisplayName)) throw new InvalidOperationException("BossDefinition 缺少显示名称。");
        if (MaximumHealth <= 0) throw new InvalidOperationException("BossDefinition 最大生命必须为正数。");
        if (GridSize.X <= 0 || GridSize.Y <= 0) throw new InvalidOperationException("BossDefinition 的导航网格尺寸必须为正数。");
        if (CellSize <= 0) throw new InvalidOperationException("BossDefinition 的导航格宽必须为正数。");
    }
}
