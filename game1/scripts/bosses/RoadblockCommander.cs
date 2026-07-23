using Godot;

namespace Game1;

/// <summary>路障指挥车的受击实体；07A 仅负责生命和阶段，攻击机制留给 07B 组合。</summary>
public partial class RoadblockCommander : CharacterBody2D, ITeamDamageable
{
    private static readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");
    [Export] public BossDefinition Definition { get; set; }
    [Export] public Godot.Collections.Array<Vector2> PhaseOneAnchors { get; set; } = new();
    [Export] public float PhaseOneMoveSpeed { get; set; } = 30f;
    [Export] public float FanCooldownSeconds { get; set; } = 2.6f;
    [Export] public float ChargeTelegraphSeconds { get; set; } = .8f;
    [Export] public float VulnerableSeconds { get; set; } = 1.5f;
    [Signal] public delegate void HealthChangedEventHandler(int current, int maximum);
    [Signal] public delegate void PhaseChangedEventHandler(int phase);
    [Signal] public delegate void DefeatedEventHandler();

    private BossPhaseController _phaseController = null!;
    private Sprite2D _visual = null!;
    private Sprite2D _weakpoint = null!;
    private int _currentHealth;
    private bool _initialized;
    private int _anchorIndex;
    private float _fanCooldown;
    private float _chargeTimer;
    private float _vulnerableTimer;
    private bool _charging;
    private bool _chargeTelegraph;
    private Line2D _chargeWarning = null!;
    private Vector2 _chargeTarget;

    public int CurrentHealth => _currentHealth;
    public int MaximumHealth => Definition is null ? 0 : Definition.MaximumHealth;
    public Team DamageTeam => Team.Enemy;

    public override void _Ready()
    {
        _visual = GetNode<Sprite2D>("Visual");
        _weakpoint = GetNode<Sprite2D>("Weakpoint");
        _chargeWarning = new Line2D { Name = "ChargeWarning", Width = 3f, DefaultColor = new Color(1f, .12f, .06f, .8f), ZIndex = 5, Visible = false };
        AddChild(_chargeWarning);
        if (Definition is not null) Initialize(Definition);
    }

    public void Initialize(BossDefinition definition)
    {
        definition.Validate();
        Definition = definition;
        _currentHealth = definition.MaximumHealth;
        _phaseController = new BossPhaseController();
        _phaseController.PhaseChanged += OnPhaseChanged;
        _phaseController.Defeated += OnDefeated;
        _initialized = true;
        EmitSignal(SignalName.HealthChanged, _currentHealth, definition.MaximumHealth);
    }

    public DamageResult ApplyDamage(DamageContext context)
    {
        if (!_initialized || _phaseController.CurrentPhase == BossPhase.Defeated) return new DamageResult(0, false);
        if (_phaseController.CurrentPhase == BossPhase.PhaseTwo && _vulnerableTimer <= 0f) return new DamageResult(0, false);
        int applied = Mathf.Min(_currentHealth, Mathf.Max(0, context.Amount));
        if (applied == 0) return new DamageResult(0, false);

        _currentHealth -= applied;
        EmitSignal(SignalName.HealthChanged, _currentHealth, Definition.MaximumHealth);
        _phaseController.ReportHealth(_currentHealth, Definition.MaximumHealth);
        return new DamageResult(applied, _phaseController.CurrentPhase == BossPhase.Defeated);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized) return;
        if (_phaseController.CurrentPhase == BossPhase.PhaseTwo)
        {
            UpdateCharge((float)delta);
            return;
        }
        if (_phaseController.CurrentPhase != BossPhase.PhaseOne) return;
        UpdatePhaseOneMovement((float)delta);
        _fanCooldown = Mathf.Max(0f, _fanCooldown - (float)delta);
        if (_fanCooldown <= 0f) FireFanAtPlayer();
    }

    public void BeginCharge(Vector2 targetPosition)
    {
        if (!_initialized || _phaseController.CurrentPhase != BossPhase.PhaseTwo || _charging || _chargeTelegraph || _vulnerableTimer > 0f) return;
        _chargeTelegraph = true;
        _chargeTimer = ChargeTelegraphSeconds;
        _chargeTarget = targetPosition;
        _chargeWarning.Points = new Vector2[] { Vector2.Zero, ToLocal(targetPosition) };
        _chargeWarning.Visible = true;
    }

    private void UpdateCharge(float delta)
    {
        if (_vulnerableTimer > 0f)
        {
            _vulnerableTimer = Mathf.Max(0f, _vulnerableTimer - delta);
            _visual.Modulate = new Color(1f, .9f, .45f);
            _weakpoint.Visible = true;
            if (_vulnerableTimer <= 0f)
            {
                _visual.Modulate = new Color(1f, .55f, .45f);
                _weakpoint.Visible = false;
            }
            return;
        }
        if (_chargeTelegraph)
        {
            _chargeTimer -= delta;
            if (_chargeTimer <= 0f) { _chargeTelegraph = false; _charging = true; _chargeWarning.Visible = false; }
            return;
        }
        if (!_charging) return;
        Vector2 offset = _chargeTarget - GlobalPosition;
        float chargeStep = 175f * delta;
        if (offset.Length() <= chargeStep + 0.5f)
        {
            if (!offset.IsZeroApprox()) MoveAndCollide(offset);
            EnterVulnerableWindow();
            return;
        }

        KinematicCollision2D collision = MoveAndCollide(offset.Normalized() * chargeStep);
        if (collision is not null)
        {
            EnterVulnerableWindow();
        }
    }

    /// <summary>冲锋撞墙或到达预警终点都必须释放锁定并暴露弱点，避免开阔地永久无敌。</summary>
    private void EnterVulnerableWindow()
    {
        _charging = false;
        _vulnerableTimer = VulnerableSeconds;
        _weakpoint.Visible = true;
        Velocity = Vector2.Zero;
    }

    private void UpdatePhaseOneMovement(float delta)
    {
        if (PhaseOneAnchors.Count == 0) return;
        Vector2 target = PhaseOneAnchors[_anchorIndex % PhaseOneAnchors.Count];
        Vector2 velocity = GlobalPosition.DirectionTo(target) * PhaseOneMoveSpeed;
        if (GlobalPosition.DistanceTo(target) < 3f)
        {
            _anchorIndex = (_anchorIndex + 1) % PhaseOneAnchors.Count;
            return;
        }
        Velocity = velocity;
        Rotation = velocity.Angle();
        MoveAndSlide();
    }

    private void FireFanAtPlayer()
    {
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player is null) return;
        Vector2 direction = GlobalPosition.DirectionTo(player.GlobalPosition);
        if (direction.IsZeroApprox()) return;
        foreach (float offset in new[] { Mathf.DegToRad(-14f), 0f, Mathf.DegToRad(14f) })
        {
            Projectile projectile = ProjectileScene.Instantiate<Projectile>();
            GetTree().CurrentScene.AddChild(projectile);
            Vector2 shotDirection = direction.Rotated(offset);
            projectile.GlobalPosition = GlobalPosition + shotDirection * 24f;
            projectile.CollisionMask = 19;
            projectile.Initialize(new ProjectileSpec(8, 180f, 2.2f, 0), Team.Enemy, shotDirection);
        }
        _fanCooldown = FanCooldownSeconds;
    }

    private async void OnPhaseChanged(BossPhase phase)
    {
        EmitSignal(SignalName.PhaseChanged, (int)phase);
        if (!IsInsideTree()) return;
        _visual.Modulate = Colors.White;
        _weakpoint.Visible = false;
        await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree() && _phaseController.CurrentPhase == BossPhase.PhaseTwo)
            _visual.Modulate = new Color(1f, .55f, .45f);
    }

    private void OnDefeated()
    {
        Velocity = Vector2.Zero;
        GetNode<CollisionShape2D>("Collision").Disabled = true;
        EmitSignal(SignalName.Defeated);
    }
}
