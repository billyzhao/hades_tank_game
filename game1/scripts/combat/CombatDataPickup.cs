using System;
using Godot;

namespace Game1;

/// <summary>敌军掉落的正整数战斗数据；靠近玩家或波末回收时只收集一次。</summary>
public partial class CombatDataPickup : Node2D
{
    public int Amount { get; private set; }
    public event Action<CombatDataPickup, int> Collected;

    public void Initialize(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Amount = amount;
        QueueRedraw();
    }

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

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 4f, new Color(0.32f, 0.9f, 1f));
        DrawArc(Vector2.Zero, 6f, 0f, Mathf.Tau, 12, new Color(0.85f, 1f, 1f), 1f);
    }

    public void Collect()
    {
        if (Amount <= 0) return;
        int collected = Amount;
        Amount = 0;
        Collected?.Invoke(this, collected);
        QueueFree();
    }
}
