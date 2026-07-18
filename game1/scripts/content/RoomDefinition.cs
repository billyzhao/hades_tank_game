using System;
using Godot;

namespace Game1;

/// <summary>数据驱动的战斗房间定义；运行时代码只通过该资源选择场景和导航格信息。</summary>
[GlobalClass]
public partial class RoomDefinition : Resource
{
    [Export] public PackedScene Scene { get; set; }
    [Export] public Vector2I GridSize { get; set; }
    [Export] public int CellSize { get; set; }
    /// <summary>房间定义拥有出生边；导演不再以代码常量假定所有房间从同一侧来敌。</summary>
    [Export] public Godot.Collections.Array<Vector2> EnemySpawnPoints { get; set; } = new();
    [Export] public Godot.Collections.Array<RoomWaveDefinition> Waves { get; set; } = new();

    public void Validate()
    {
        if (Scene is null) throw new InvalidOperationException("RoomDefinition 缺少场景引用。");
        if (GridSize.X <= 0 || GridSize.Y <= 0) throw new InvalidOperationException("RoomDefinition 的导航网格尺寸必须为正数。");
        if (CellSize <= 0) throw new InvalidOperationException("RoomDefinition 的格子宽度必须为正数。");
        if (Waves is null || Waves.Count == 0) throw new InvalidOperationException("RoomDefinition 至少需要一波敌军定义。");
        if (EnemySpawnPoints is null || EnemySpawnPoints.Count == 0) throw new InvalidOperationException("RoomDefinition 至少需要一个敌军出生点。");
        foreach (RoomWaveDefinition wave in Waves)
        {
            if (wave is null || wave.Behaviors is null || wave.Behaviors.Count == 0)
                throw new InvalidOperationException("RoomDefinition 包含空波次定义。");
        }
    }
}
