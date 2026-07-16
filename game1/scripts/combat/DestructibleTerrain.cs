using Godot;

namespace Game1;

/// <summary>砖墙运行时耐久表；仅删除本场景节点，不修改共享资源或钢墙。</summary>
public partial class DestructibleTerrain : StaticBody2D, IDamageable
{
    [Export] public int HitPoints { get; set; } = 20;
    public DamageResult ApplyDamage(DamageContext context)
    {
        int applied = System.Math.Min(HitPoints, System.Math.Max(0, context.Amount));
        bool depleted = HitPoints > 0 && applied == HitPoints;
        HitPoints -= applied;
        if (depleted) QueueFree();
        return new DamageResult(applied, depleted);
    }
}
