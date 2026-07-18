using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>冲刺履带留下的短时电能残影：以敌方碰撞层做一次性范围伤害，并提供可见的青色轨迹反馈。</summary>
public partial class DashTrail : Node2D
{
    private const float LifetimeSeconds = 0.45f;
    private const float Radius = 16f;
    private readonly HashSet<ulong> _hitInstanceIds = new();
    private float _remainingLifetime;
    private int _damage;

    public void Initialize(Vector2 worldPosition, int damage)
    {
        GlobalPosition = worldPosition;
        _damage = Mathf.Max(0, damage);
        _remainingLifetime = LifetimeSeconds;
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        _remainingLifetime -= (float)delta;
        if (_remainingLifetime <= 0f)
        {
            QueueFree();
            return;
        }

        PhysicsShapeQueryParameters2D query = new()
        {
            Shape = new CircleShape2D { Radius = Radius },
            Transform = new Transform2D(0f, GlobalPosition),
            CollisionMask = 8
        };
        foreach (Godot.Collections.Dictionary hit in GetWorld2D().DirectSpaceState.IntersectShape(query))
        {
            if (hit["collider"].AsGodotObject() is not IDamageable damageable || damageable is not GodotObject nativeObject) continue;
            ulong instanceId = nativeObject.GetInstanceId();
            if (!_hitInstanceIds.Add(instanceId)) continue;
            damageable.ApplyDamage(new DamageContext(_damage));
        }
    }

    public override void _Draw()
    {
        float alpha = Mathf.Clamp(_remainingLifetime / LifetimeSeconds, 0f, 1f) * 0.75f;
        DrawCircle(Vector2.Zero, Radius, new Color(0.1f, 0.9f, 1f, alpha));
        DrawArc(Vector2.Zero, Radius + 3f, 0f, Mathf.Tau, 20, new Color(0.65f, 1f, 1f, alpha), 1.5f);
    }
}
