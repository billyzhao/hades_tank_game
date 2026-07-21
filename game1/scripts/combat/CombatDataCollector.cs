using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>场上战斗数据的唯一注册和波末回收入口，不管理等级或暂停。</summary>
public partial class CombatDataCollector : Node
{
    private readonly HashSet<CombatDataPickup> _pickups = new();
    public event Action<int> DataCollected;
    public int PendingPickupCount => _pickups.Count;

    public void Spawn(Node parent, Vector2 position, int amount)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));
        CombatDataPickup pickup = new() { GlobalPosition = position };
        pickup.Initialize(amount);
        pickup.Collected += OnCollected;
        _pickups.Add(pickup);
        parent.AddChild(pickup);
    }

    public void CollectAllAtWaveEnd()
    {
        foreach (CombatDataPickup pickup in _pickups.Where(IsInstanceValid).ToArray()) pickup.Collect();
    }

    private void OnCollected(CombatDataPickup pickup, int amount)
    {
        _pickups.Remove(pickup);
        DataCollected?.Invoke(amount);
    }
}
