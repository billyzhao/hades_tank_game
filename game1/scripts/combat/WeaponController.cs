using Godot;

namespace Game1;

public partial class WeaponController : Node
{
    private static readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");
    [Export] public WeaponDefinition Definition { get; set; } = new();
    private float _cooldown;

    public override void _PhysicsProcess(double delta) => _cooldown = Mathf.Max(0f, _cooldown - (float)delta);
    public bool TryFire(Vector2 origin, Vector2 direction, Team team)
    {
        // 冷却由武器控制器集中管理，玩家输入可持续按住，避免把射速判断散落在角色脚本中。
        if (_cooldown > 0f || direction.IsZeroApprox()) return false;
        Projectile projectile = ProjectileScene.Instantiate<Projectile>();
        // 炮弹加入当前主场景，而非挂在坦克下：坦克移动或销毁时不会拖拽、误删已发射炮弹。
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = origin;
        projectile.Initialize(Definition.CreateSpec(), team, direction);
        _cooldown = Definition.CooldownSeconds;
        return true;
    }
}
