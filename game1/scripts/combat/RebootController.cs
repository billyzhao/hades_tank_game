using Godot;

namespace Game1;

public enum RebootPhase { Ready, Reconstructing, Protected, Failed }

/// <summary>玩家报废后的房间级恢复流程；重启次数属于 RunState，切换房间后不会重置。</summary>
public partial class RebootController : Node
{
    [Export] public float RebootDelaySeconds { get; set; } = 1.2f;
    [Export] public float ProtectionSeconds { get; set; } = 2f;
    [Export] public float KnockbackRadius { get; set; } = 72f;
    [Export] public float KnockbackDistance { get; set; } = 36f;

    [Signal] public delegate void RebootStartedEventHandler(double durationSeconds);
    [Signal] public delegate void RebootedEventHandler();
    [Signal] public delegate void RunFailedEventHandler();

    private PlayerTank _player = null!;
    private HealthComponent _health = null!;
    private RunController _runController = null!;
    private RunState _runState = null!;
    private bool _busy;

    public RebootPhase Phase { get; private set; } = RebootPhase.Ready;
    public double PhaseSecondsRemaining { get; private set; }

    public override void _Ready()
    {
        _player = GetParent().GetNode<PlayerTank>("PlayerTank");
        _health = _player.GetNode<HealthComponent>("HealthComponent");
        _health.Depleted += OnPlayerDepleted;
    }

    public override void _Process(double delta)
    {
        PhaseSecondsRemaining = System.Math.Max(0d, PhaseSecondsRemaining - delta);
    }

    public void Configure(RunController runController, RunState runState)
    {
        _runController = runController ?? throw new System.ArgumentNullException(nameof(runController));
        _runState = runState ?? throw new System.ArgumentNullException(nameof(runState));
    }

    private async void OnPlayerDepleted()
    {
        if (_busy) return;
        if (_runController is null || _runState is null)
        {
            GD.PushError("RebootController 必须先由 AppRoot 注入 RunController 与 RunState。");
            return;
        }

        _busy = true;
        _player.SetPhysicsProcess(false);
        _player.Velocity = Vector2.Zero;
        _player.GetNode<CollisionShape2D>("BodyCollision")
            .SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        if (!_runController.OnTankDefeated())
        {
            Phase = RebootPhase.Failed;
            GD.PrintErr("本局失败：坦克报废且没有剩余战场重启次数。");
            EmitSignal(SignalName.RunFailed);
            return;
        }

        Vector2 rebootPosition = _player.GlobalPosition;
        Phase = RebootPhase.Reconstructing;
        PhaseSecondsRemaining = RebootDelaySeconds;
        _health.GrantInvulnerability(RebootDelaySeconds);
        EmitSignal(SignalName.RebootStarted, (double)RebootDelaySeconds);
        await ToSignal(GetTree().CreateTimer(RebootDelaySeconds), SceneTreeTimer.SignalName.Timeout);
        if (!IsInsideTree()) return;

        _player.GlobalPosition = rebootPosition;
        _runState.RestoreAfterReboot();
        _health.SetArmor(_runState.PlayerArmor);
        ApplyKnockbackPulse(rebootPosition);
        _health.GrantInvulnerability(ProtectionSeconds);
        _player.Modulate = new Color(0.45f, 0.95f, 1f, 0.65f);
        _player.CreateTween()
            .TweenProperty(_player, "modulate", Colors.White, ProtectionSeconds);
        _player.GetNode<CollisionShape2D>("BodyCollision")
            .SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _player.SetPhysicsProcess(true);
        Phase = RebootPhase.Protected;
        PhaseSecondsRemaining = ProtectionSeconds;
        EmitSignal(SignalName.Rebooted);
        await ToSignal(GetTree().CreateTimer(ProtectionSeconds), SceneTreeTimer.SignalName.Timeout);
        if (!IsInsideTree()) return;
        Phase = RebootPhase.Ready;
        PhaseSecondsRemaining = 0d;
        _busy = false;
    }

    /// <summary>脉冲只推动普通敌军，不造成伤害、不影响 Boss、炮弹或地形。</summary>
    private void ApplyKnockbackPulse(Vector2 origin)
    {
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is EnemyTank enemy && enemy.GlobalPosition.DistanceTo(origin) <= KnockbackRadius)
            {
                enemy.ApplyRebootKnockback(origin, KnockbackDistance);
            }
        }
    }
}
