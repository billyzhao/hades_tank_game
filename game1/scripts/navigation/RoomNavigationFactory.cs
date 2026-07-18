using System;
using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>
/// 为单个房间维护唯一的 A* 网格。Structure 与可破坏砖墙变化时，已有 provider
/// 保持不变，但其内部网格立即重建，避免敌军回退到旧的直线路径。
/// </summary>
public sealed class RoomNavigationFactory : IDisposable
{
    private readonly Node2D _room;
    private readonly NavigationGrid _grid;
    private readonly TileMapLayer _structure;
    private readonly TileTerrainAdapter _terrain;
    private bool _disposed;

    public IEnemyPathProvider Provider { get; }

    public RoomNavigationFactory(Node2D room, Vector2I gridSize, int cellSize)
    {
        _room = room ?? throw new ArgumentNullException(nameof(room));
        _grid = new NavigationGrid(gridSize);
        _structure = room.GetNodeOrNull<TileMapLayer>("Structure");
        _terrain = room.GetNodeOrNull<TileTerrainAdapter>("TileTerrainAdapter");
        Provider = new RoomPathProvider(_grid, cellSize);

        if (_terrain is not null) _terrain.BrickDestroyed += OnBrickDestroyed;
        Rebuild();
    }

    public void Rebuild()
    {
        if (_disposed) return;

        HashSet<Vector2I> blockedCells = new();
        if (_structure is not null)
        {
            foreach (Vector2I cell in _structure.GetUsedCells()) blockedCells.Add(cell);
        }

        if (_terrain is not null)
        {
            foreach (Vector2I cell in _terrain.BlockedNavigationCells) blockedCells.Add(cell);
        }

        _grid.Rebuild(blockedCells);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_terrain is not null) _terrain.BrickDestroyed -= OnBrickDestroyed;
    }

    private void OnBrickDestroyed(Vector2I _) => Rebuild();
}
