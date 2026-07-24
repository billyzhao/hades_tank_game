using System;
using Godot;

namespace Game1;

/// <summary>敌军掉落的正整数战斗数据；靠近玩家或波末回收时只收集一次。</summary>
public partial class CombatDataPickup : Node2D
{
    private AnimatedSprite2D _visual = null!;
    public int Amount { get; private set; }
    public event Action<CombatDataPickup, int> Collected;

    public void Initialize(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Amount = amount;
        if (IsInsideTree()) EnsureVisual();
    }

    public override void _Ready() => EnsureVisual();

    public override void _Process(double delta)
    {
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player is null) return;

        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
        if (distance <= 180f && distance > 12f)
        {
            // 进入磁吸范围后可见地飞向坦克，而不是瞬间消失。
            GlobalPosition = GlobalPosition.MoveToward(player.GlobalPosition, (135f + (180f - distance) * 2f) * (float)delta);
        }
        else if (distance <= 12f)
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (Amount <= 0) return;
        int collected = Amount;
        Amount = 0;
        Collected?.Invoke(this, collected);
        QueueFree();
    }

    private void EnsureVisual()
    {
        if (_visual is not null) return;
        _visual = SpriteEffectPlayer.Create("CombatDataVisual", ArtTextureCatalog.CombatData, 8f, true);
        _visual.Scale = Vector2.One * .65f;
        _visual.ZIndex = 5;
        AddChild(_visual);
        _visual.Play();
    }
}
