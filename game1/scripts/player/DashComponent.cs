using Godot;

namespace Game1;

public partial class DashComponent : Node
{
    [Export] public float SpeedMultiplier { get; set; } = 3f;

    [Export] public float DurationSeconds { get; set; } = 0.14f;

    [Export] public float CooldownSeconds { get; set; } = 0.8f;

    private DashState _state = null!;
    private BuildController _buildController;

    [Signal] public delegate void DashStartedEventHandler();

    [Signal] public delegate void DashEndedEventHandler();

    public Vector2 Direction => _state.Direction;

    public bool IsDashing => _state.IsDashing;

    public bool IsCoolingDown => _state.IsCoolingDown;

    public override void _Ready()
    {
        RefreshSnapshot();
    }

    /// <summary>冲刺只订阅本局构筑快照，BuildController 结束本局时会统一清除此订阅。</summary>
    public void AttachBuild(BuildController buildController)
    {
        _buildController = buildController ?? throw new System.ArgumentNullException(nameof(buildController));
        _buildController.SnapshotChanged += RefreshSnapshot;
        RefreshSnapshot();
    }

    public bool TryStart(Vector2 direction)
    {
        if (!_state.TryStart(direction))
        {
            return false;
        }

        _buildController?.OnDashStarted();
        SpawnTrail();
        EmitSignal(SignalName.DashStarted);
        return true;
    }

    public void Advance(float deltaSeconds)
    {
        if (_state.Advance(deltaSeconds) == DashAdvanceResult.Ended)
        {
            EmitSignal(SignalName.DashEnded);
        }
    }

    private void RefreshSnapshot()
    {
        // 正在冲刺时不替换状态，避免协议选择瞬间取消本次已开始的冲刺。
        if (_state is not null && _state.IsDashing)
        {
            return;
        }

        float cooldown = _buildController is null
            ? CooldownSeconds
            : _buildController.EvaluateStat(StatId.DashCooldown, CooldownSeconds);
        _state = new DashState(DurationSeconds, cooldown);
    }

    private void SpawnTrail()
    {
        int damage = _buildController is null
            ? 0
            : Mathf.RoundToInt(_buildController.EvaluateStat(StatId.DashTrailDamage, 0f));
        if (damage <= 0) return;

        DashTrail trail = new();
        GetTree().CurrentScene.AddChild(trail);
        trail.Initialize(GetParent<Node2D>().GlobalPosition, damage);
    }
}
