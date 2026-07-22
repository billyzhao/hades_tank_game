using Godot;

namespace Game1;

public partial class Projectile : Node2D
{
    private static readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");
    private static readonly Texture2D PlayerShellTexture = GD.Load<Texture2D>("res://assets/sprites/effects/player_shell.png");
    private static readonly Texture2D EnemyShellTexture = GD.Load<Texture2D>("res://assets/sprites/effects/enemy_shell.png");
    [Signal] public delegate void ImpactedEventHandler(Vector2 position, bool destroyedTarget, bool reflected);
    [Export] public uint CollisionMask { get; set; } = 27;
    private ProjectileSpec _spec;
    private Vector2 _direction;
    private float _lifetime;
    private int _bounces;
    private int _splits;
    private Team _team;

    public void Initialize(ProjectileSpec spec, Team team, Vector2 direction)
    {
        _spec = spec;
        _direction = direction.Normalized();
        Rotation = _direction.Angle();
        _lifetime = spec.LifetimeSeconds;
        _bounces = spec.Bounces;
        _splits = spec.SplitCount;
        _team = team;
        CollisionMask = ProjectileTargeting.CollisionMaskFor(team);
        GetNode<Sprite2D>("Visual").Texture = team == Team.Player ? PlayerShellTexture : EnemyShellTexture;
        if (team == Team.Enemy) AddToGroup("enemy_projectiles");
        // 炮弹颜色由独立像素贴图承担，避免二次染色破坏玩家黄弹与敌军红弹的识别。
        Modulate = Colors.White;
    }

    public override void _PhysicsProcess(double delta)
    {
        // 不依赖 Node2D 的逐帧位移碰撞：高速炮弹可能跨过薄墙，故每帧对“当前位置到目标位置”做线段查询。
        float remaining = _spec.Speed * (float)delta;
        _lifetime -= (float)delta;
        if (_lifetime <= 0f) { QueueFree(); return; }
        for (int impact = 0; impact < 4 && remaining > 0f; impact++)
        {
            Vector2 target = GlobalPosition + _direction * remaining;
            using PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(GlobalPosition, target, CollisionMask);
            Godot.Collections.Dictionary hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
            if (hit.Count == 0) { GlobalPosition = target; return; }
            Vector2 point = hit["position"].AsVector2();
            Vector2 normal = hit["normal"].AsVector2();
            // 命中可受伤实体时先结算伤害；砖墙不会进入反弹分支。
            if (hit["collider"].AsGodotObject() is ITeamDamageable teamDamageable)
            {
                if (!ProjectileTargeting.CanDamage(_team, teamDamageable.DamageTeam))
                {
                    GlobalPosition = point + _direction * 0.05f;
                    continue;
                }
                DamageResult result = teamDamageable.ApplyDamage(new DamageContext(_spec.Damage));
                SpawnSplitShells(point);
                EmitSignal(SignalName.Impacted, point, result.DepletedNow, false);
                QueueFree();
                return;
            }
            if (hit["collider"].AsGodotObject() is IDamageable damageable)
            {
                DamageResult result = damageable.ApplyDamage(new DamageContext(_spec.Damage));
                SpawnSplitShells(point);
                EmitSignal(SignalName.Impacted, point, result.DepletedNow, false);
                QueueFree();
                return;
            }
            // 命中后保留本物理帧尚未走完的距离，使反弹不会因帧率不同而缩短或变慢。
            // 可破坏砖墙由 TileMapLayer 承载，不能再依赖逐块 StaticBody2D。
            // 砖块耐久归零时，适配器会清除 tile 并刷新房间的导航阻塞集合。
            if (hit["collider"].AsGodotObject() is TileMapLayer destructibleLayer
                && destructibleLayer.Name == "Destructible"
                && destructibleLayer.GetParent()?.GetNodeOrNull<TileTerrainAdapter>("TileTerrainAdapter") is TileTerrainAdapter terrain)
            {
                Vector2I cell = destructibleLayer.LocalToMap(destructibleLayer.ToLocal(point));
                terrain.DamageBrick(cell, _spec.Damage);
                SpawnSplitShells(point);
                EmitSignal(SignalName.Impacted, point, false, false);
                QueueFree();
                return;
            }
            remaining -= GlobalPosition.DistanceTo(point);
            // 轻微沿法线偏移，防止下一次射线从墙面内部开始而在墙角反复命中。
            GlobalPosition = point + normal * 0.05f;
            EmitSignal(SignalName.Impacted, point, false, true);
            // MVP 钢墙默认提供一次反弹；耗尽时立即销毁，避免在封闭空间中无限弹射。
            if (_bounces-- <= 0) { QueueFree(); return; }
            _direction = ProjectileMath.Reflect(_direction, normal);
            Rotation = _direction.Angle();
        }
    }

    /// <summary>分裂效果由数据化 SplitCount 驱动，只在母弹第一次命中时生成两枚无递归子弹。</summary>
    private void SpawnSplitShells(Vector2 point)
    {
        if (_splits <= 0) return;

        int childDamage = Mathf.Max(1, Mathf.RoundToInt(_spec.Damage * 0.65f));
        for (int index = 0; index < _splits; index++)
        {
            foreach (float angleOffset in new[] { -0.45f, 0.45f })
            {
                Projectile child = ProjectileScene.Instantiate<Projectile>();
                GetTree().CurrentScene.AddChild(child);
                Vector2 childDirection = _direction.Rotated(angleOffset);
                child.GlobalPosition = point + childDirection * 3f;
                child.Initialize(new ProjectileSpec(childDamage, _spec.Speed, _spec.LifetimeSeconds * 0.6f, 0, 0), _team, childDirection);
            }
        }

        _splits = 0;
    }
}
