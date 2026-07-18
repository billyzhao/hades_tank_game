using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>房间运行时唯一的格点寻路入口。网格只在加载、地形变化或显式重置时重建。</summary>
public sealed class NavigationGrid
{
    private readonly Vector2I _gridSize;
    private AStarGrid2D _grid = null!;

    public NavigationGrid(Vector2I gridSize)
    {
        if (gridSize.X <= 0 || gridSize.Y <= 0) throw new ArgumentOutOfRangeException(nameof(gridSize));
        _gridSize = gridSize;
        Rebuild(new HashSet<Vector2I>());
    }

    public void Rebuild(IReadOnlySet<Vector2I> blockedCells)
    {
        if (blockedCells is null) throw new ArgumentNullException(nameof(blockedCells));

        _grid = new AStarGrid2D
        {
            Region = new Rect2I(Vector2I.Zero, _gridSize),
            CellSize = Vector2.One,
            DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never
        };
        _grid.Update();
        foreach (Vector2I cell in blockedCells)
        {
            if (IsInside(cell)) _grid.SetPointSolid(cell, true);
        }
    }

    public IReadOnlyList<Vector2I> FindPath(Vector2I from, Vector2I to)
    {
        if (!IsInside(from) || !IsInside(to) || _grid.IsPointSolid(from) || _grid.IsPointSolid(to))
        {
            return Array.Empty<Vector2I>();
        }

        Vector2I[] path = _grid.GetIdPath(from, to).ToArray();
        return path.Length == 0 ? Array.Empty<Vector2I>() : path;
    }

    private bool IsInside(Vector2I cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < _gridSize.X && cell.Y < _gridSize.Y;
}
