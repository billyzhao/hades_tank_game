using Godot;

namespace Game1;

/// <summary>路障指挥车的受击实体；07A 仅负责生命和阶段，攻击机制留给 07B 组合。</summary>
public partial class RoadblockCommander : CharacterBody2D, IDamageable
{
    [Export] public BossDefinition Definition { get; set; }
    [Signal] public delegate void HealthChangedEventHandler(int current, int maximum);
    [Signal] public delegate void PhaseChangedEventHandler(int phase);
    [Signal] public delegate void DefeatedEventHandler();

    private BossPhaseController _phaseController = null!;
    private Polygon2D _visual = null!;
    private int _currentHealth;
    private bool _initialized;

    public int CurrentHealth => _currentHealth;
    public int MaximumHealth => Definition is null ? 0 : Definition.MaximumHealth;

    public override void _Ready()
    {
        _visual = GetNode<Polygon2D>("Visual");
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
        int applied = Mathf.Min(_currentHealth, Mathf.Max(0, context.Amount));
        if (applied == 0) return new DamageResult(0, false);

        _currentHealth -= applied;
        EmitSignal(SignalName.HealthChanged, _currentHealth, Definition.MaximumHealth);
        _phaseController.ReportHealth(_currentHealth, Definition.MaximumHealth);
        return new DamageResult(applied, _phaseController.CurrentPhase == BossPhase.Defeated);
    }

    private async void OnPhaseChanged(BossPhase phase)
    {
        EmitSignal(SignalName.PhaseChanged, (int)phase);
        if (!IsInsideTree()) return;
        _visual.Color = Colors.White;
        await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree() && _phaseController.CurrentPhase == BossPhase.PhaseTwo)
            _visual.Color = new Color(0.92f, 0.2f, 0.12f);
    }

    private void OnDefeated()
    {
        Velocity = Vector2.Zero;
        GetNode<CollisionShape2D>("Collision").Disabled = true;
        EmitSignal(SignalName.Defeated);
    }
}
