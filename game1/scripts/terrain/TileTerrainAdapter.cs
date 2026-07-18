using System;
using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>管理 Destructible TileMapLayer 的砖块耐久，并把砖块变化同步为导航阻塞格事件。</summary>
public partial class TileTerrainAdapter : Node
{
    [Export] public Godot.Collections.Array<Vector2I> InitialBrickCells { get; set; } = new();
    [Export] public int BrickHitPoints { get; set; } = 20;
    private readonly Dictionary<Vector2I, int> _hitPointsByCell = new();
    private readonly HashSet<Vector2I> _blockedCells = new();
    private TileMapLayer _destructibleLayer;

    public event Action<Vector2I> BrickDestroyed;
    public IReadOnlySet<Vector2I> BlockedNavigationCells => _blockedCells;

    public void Initialize(TileMapLayer destructibleLayer, IEnumerable<Vector2I> brickCells, int hitPoints)
    {
        if (destructibleLayer is null) throw new ArgumentNullException(nameof(destructibleLayer));
        if (brickCells is null) throw new ArgumentNullException(nameof(brickCells));
        if (hitPoints <= 0) throw new ArgumentOutOfRangeException(nameof(hitPoints));

        _destructibleLayer = destructibleLayer;
        _hitPointsByCell.Clear();
        _blockedCells.Clear();
        foreach (Vector2I cell in brickCells)
        {
            _hitPointsByCell[cell] = hitPoints;
            _blockedCells.Add(cell);
            _destructibleLayer.SetCell(cell, 0, Vector2I.Zero);
        }
    }

    public bool DamageBrick(Vector2I cell, int damage)
    {
        if (damage <= 0 || !_hitPointsByCell.TryGetValue(cell, out int remaining)) return false;

        remaining -= damage;
        if (remaining > 0)
        {
            _hitPointsByCell[cell] = remaining;
            return false;
        }

        return DestroyBrick(cell);
    }

    /// <summary>供 Boss 等受控事件一次性拆除指定砖墙；已不存在的格安全返回 false。</summary>
    public bool DestroyBrick(Vector2I cell)
    {
        if (!_hitPointsByCell.Remove(cell)) return false;

        _blockedCells.Remove(cell);
        _destructibleLayer?.EraseCell(cell);
        BrickDestroyed?.Invoke(cell);
        return true;
    }
    public override void _Ready()
    {
        TileMapLayer layer = GetParent().GetNodeOrNull<TileMapLayer>("Destructible");
        if (layer is not null) Initialize(layer, InitialBrickCells, BrickHitPoints);
    }
}
