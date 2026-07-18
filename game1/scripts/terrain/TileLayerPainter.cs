using Godot;

namespace Game1;

/// <summary>将场景声明的格子数据绘制到指定 TileMapLayer，避免把房间布局写死进运行时代码。</summary>
public partial class TileLayerPainter : Node
{
    [Export] public Godot.Collections.Array<Vector2I> Cells { get; set; } = new();
    [Export] public int SourceId { get; set; }
    [Export] public Vector2I AtlasCoordinates { get; set; }

    public override void _Ready()
    {
        if (GetParent() is not TileMapLayer layer) return;
        foreach (Vector2I cell in Cells) layer.SetCell(cell, SourceId, AtlasCoordinates);
    }
}
