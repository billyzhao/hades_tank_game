using Godot;

namespace Game1;

public partial class WeaponController : Node
{
    private static readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");
    [Signal] public delegate void FiredEventHandler(Vector2 origin, Vector2 direction, int team);
    [Signal] public delegate void ProjectileImpactedEventHandler(Vector2 position, bool destroyedTarget, bool reflected);
    [Export] public WeaponDefinition Definition { get; set; } = new();
    private float _cooldown;
    private BuildController _buildController;
    private float _fireRateMultiplier = 1f;
    public float FireRateMultiplier => _fireRateMultiplier;

    /// <summary>由房间编排器在本局开始时注入；武器不读取协议 Id，只读取统一数值快照。</summary>
    public void AttachBuild(BuildController buildController)
    {
        _buildController = buildController ?? throw new System.ArgumentNullException(nameof(buildController));
    }

    public override void _PhysicsProcess(double delta) => _cooldown = Mathf.Max(0f, _cooldown - (float)delta);

    public void SetFireRateMultiplier(float multiplier)
    {
        if (!float.IsFinite(multiplier) || multiplier is < 0.75f or > 2f)
            throw new System.ArgumentOutOfRangeException(nameof(multiplier));
        _fireRateMultiplier = multiplier;
        _cooldown = Mathf.Min(_cooldown, EvaluateStat(StatId.FireCooldown, Definition.CooldownSeconds) / multiplier);
    }

    public bool TryFire(Vector2 origin, Vector2 direction, Team team)
    {
        // 冷却由武器控制器集中管理，玩家输入可持续按住，避免把射速判断散落在角色脚本中。
        if (_cooldown > 0f || direction.IsZeroApprox()) return false;
        Projectile projectile = ProjectileScene.Instantiate<Projectile>();
        // 炮弹加入当前主场景，而非挂在坦克下：坦克移动或销毁时不会拖拽、误删已发射炮弹。
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = origin;
        int damage = Mathf.RoundToInt(EvaluateStat(StatId.Damage, Definition.Damage));
        int bounces = Mathf.Max(0, Mathf.RoundToInt(EvaluateStat(StatId.ProjectileBounces, Definition.Bounces)));
        int splits = Mathf.Max(0, Mathf.RoundToInt(EvaluateStat(StatId.ProjectileSplitCount, 0f)));
        projectile.Initialize(new ProjectileSpec(damage, Definition.ProjectileSpeed, Definition.LifetimeSeconds, bounces, splits), team, direction);
        projectile.Impacted += (position, destroyedTarget, reflected) =>
        {
            _buildController?.OnProjectileHit();
            EmitSignal(SignalName.ProjectileImpacted, position, destroyedTarget, reflected);
        };
        _cooldown = EvaluateStat(StatId.FireCooldown, Definition.CooldownSeconds) / _fireRateMultiplier;
        _buildController?.OnShotFired();
        EmitSignal(SignalName.Fired, origin, direction, (int)team);
        return true;
    }

    private float EvaluateStat(StatId stat, float baseValue) =>
        _buildController is null ? baseValue : _buildController.EvaluateStat(stat, baseValue);
}
