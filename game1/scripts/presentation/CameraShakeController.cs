using Godot;

namespace Game1;

/// <summary>使用 Camera2D.Offset 播放渲染震动；不修改房间或角色的物理坐标。</summary>
public partial class CameraShakeController : Node
{
    private readonly CameraShakeState _state = new();
    private Camera2D _camera = null!;
    private HealthComponent _health = null!;
    private int _lastArmor;
    private WaveDirector _waveDirector;
    private int _hitStopGeneration;

    public override void _Ready()
    {
        _camera = GetParent().GetNode<Camera2D>("Camera2D");
        PlayerTank player = GetParent().GetNode<PlayerTank>("PlayerTank");
        WeaponController weapon = player.GetNode<WeaponController>("WeaponController");
        _health = player.GetNode<HealthComponent>("HealthComponent");
        _lastArmor = _health.Armor;
        weapon.Fired += (_, _, _) => Trigger(FeedbackTier.Small);
        weapon.ProjectileImpacted += (_, destroyed, reflected) =>
            Trigger(destroyed ? FeedbackTier.Medium : FeedbackTier.Small, destroyed);
        _health.ValueChanged += (armor, _) =>
        {
            if (armor < _lastArmor) Trigger(FeedbackTier.Medium);
            _lastArmor = armor;
        };
        RebootController reboot = GetParent().GetNode<RebootController>("RebootController");
        reboot.RebootStarted += _ => Trigger(FeedbackTier.Large);
        reboot.Rebooted += () => Trigger(FeedbackTier.Medium);
    }

    public override void _Process(double delta) => _camera.Offset = _state.Advance((float)delta);

    public void BindWaveDirector(WaveDirector director)
    {
        if (_waveDirector is not null) _waveDirector.EnemyDefeated -= OnEnemyDefeated;
        _waveDirector = director ?? throw new System.ArgumentNullException(nameof(director));
        _waveDirector.EnemyDefeated += OnEnemyDefeated;
    }

    public void BindBoss(RoadblockCommander boss)
    {
        System.ArgumentNullException.ThrowIfNull(boss);
        boss.PhaseChanged += _ => Trigger(FeedbackTier.Large, true);
        boss.ChargeStarted += () => Trigger(FeedbackTier.Medium);
        boss.WeakpointExposed += () => Trigger(FeedbackTier.Medium);
        boss.Defeated += () => Trigger(FeedbackTier.Large, true);
    }

    public void Trigger(FeedbackTier tier, bool requestHitStop = false)
    {
        (float strength, float seconds) = tier switch
        {
            FeedbackTier.Small => (1.0f, .075f),
            FeedbackTier.Medium => (2.6f, .14f),
            FeedbackTier.Large => (5.2f, .28f),
            _ => (1f, .075f)
        };
        _state.Start(strength, seconds);
        if (requestHitStop)
            RunHitStop(tier == FeedbackTier.Large ? .06f : .028f, tier == FeedbackTier.Large ? .08f : .18f);
    }

    private void OnEnemyDefeated(Vector2 _, bool elite)
    {
        if (elite) Trigger(FeedbackTier.Large, true);
    }

    private async void RunHitStop(float realSeconds, float timeScale)
    {
        int generation = ++_hitStopGeneration;
        Engine.TimeScale = Mathf.Min(Engine.TimeScale, timeScale);
        await ToSignal(GetTree().CreateTimer(realSeconds, processAlways: true, processInPhysics: false, ignoreTimeScale: true),
            SceneTreeTimer.SignalName.Timeout);
        if (generation == _hitStopGeneration) Engine.TimeScale = 1d;
    }

    public override void _ExitTree()
    {
        _hitStopGeneration++;
        Engine.TimeScale = 1d;
        if (_waveDirector is not null) _waveDirector.EnemyDefeated -= OnEnemyDefeated;
        if (IsInstanceValid(_camera))
        {
            _camera.Offset = Vector2.Zero;
        }
    }
}
