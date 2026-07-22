using Godot;

namespace Game1;

public partial class PlayerTank : CharacterBody2D, ITeamDamageable
{
    [Export] public float MoveSpeed { get; set; } = 120f;

    private DashComponent _dashComponent = null!;
    private Node2D _turret = null!;
    private WeaponController _weaponController = null!;
    private TankVisualAnimator _visualAnimator = null!;
    private BuildController _buildController;

    public Vector2 AimDirection { get; private set; } = Vector2.Up;
    public Team DamageTeam => Team.Player;

    public override void _Ready()
    {
        MotionMode = MotionModeEnum.Floating;
        _dashComponent = GetNode<DashComponent>("DashComponent");
        _turret = GetNode<Node2D>("Turret");
        _weaponController = GetNode<WeaponController>("WeaponController");
        _visualAnimator = GetNode<TankVisualAnimator>("TankVisualAnimator");
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaSeconds = (float)delta;
        Vector2 movementInput = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        UpdateAimDirection();

        _dashComponent.Advance(deltaSeconds);
        if (Input.IsActionJustPressed("dash"))
        {
            Vector2 dashDirection = movementInput.IsZeroApprox() ? AimDirection : movementInput;
            _dashComponent.TryStart(dashDirection);
        }

        float movementSpeed = _buildController is null ? MoveSpeed : _buildController.EvaluateStat(StatId.MoveSpeed, MoveSpeed);
        if (_dashComponent.IsDashing)
        {
            Velocity = _dashComponent.Direction * movementSpeed * _dashComponent.SpeedMultiplier;
        }
        else
        {
            Velocity = TankMotion.CalculateVelocity(movementInput, movementSpeed);
        }

        if (!Velocity.IsZeroApprox())
        {
            Rotation = Velocity.Angle();
        }

        _turret.GlobalRotation = AimDirection.Angle();
        if (Input.IsActionPressed("fire_primary"))
        {
            // 持续按住开火键即可连射，实际是否能发射由 WeaponController 的冷却决定。
            _weaponController.TryFire(GetNode<Marker2D>("Turret/Muzzle").GlobalPosition, AimDirection, Team.Player);
        }
        MoveAndSlide();
        _visualAnimator.SetMotion(Velocity, _dashComponent.IsDashing);
    }

    public void AttachBuild(BuildController buildController) =>
        _buildController = buildController ?? throw new System.ArgumentNullException(nameof(buildController));

    /// <summary>玩家碰撞体是敌方弹丸的命中实体；伤害统一交由生命组件结算。</summary>
    public DamageResult ApplyDamage(DamageContext context) =>
        GetNode<HealthComponent>("HealthComponent").ApplyDamage(context);

    private void UpdateAimDirection()
    {
        Vector2 stickAim = Input.GetVector("aim_left", "aim_right", "aim_up", "aim_down");
        if (!stickAim.IsZeroApprox())
        {
            AimDirection = stickAim.Normalized();
            return;
        }

        Vector2 mouseAim = GetGlobalMousePosition() - GlobalPosition;
        if (!mouseAim.IsZeroApprox())
        {
            AimDirection = mouseAim.Normalized();
        }
    }
}
