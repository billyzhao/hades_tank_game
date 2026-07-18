using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>将房间像素坐标映射到 NavigationGrid；空路径由调用方安全等待重试。</summary>
public sealed class RoomPathProvider : IEnemyPathProvider
{
    private readonly NavigationGrid _grid;
    private readonly int _cellSize;

    public RoomPathProvider(NavigationGrid grid, int cellSize) { _grid = grid; _cellSize = cellSize; }
    public IReadOnlyList<Vector2> GetWorldPath(Vector2 fromWorld, Vector2 toWorld) =>
        _grid.FindPath(ToCell(fromWorld), ToCell(toWorld)).Select(ToWorld).ToArray();
    private Vector2I ToCell(Vector2 world) => new(Mathf.FloorToInt(world.X / _cellSize), Mathf.FloorToInt(world.Y / _cellSize));
    private Vector2 ToWorld(Vector2I cell) => new((cell.X + .5f) * _cellSize, (cell.Y + .5f) * _cellSize);
}
