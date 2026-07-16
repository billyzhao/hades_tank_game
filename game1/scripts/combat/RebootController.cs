using Godot;

namespace Game1;

/// <summary>玩家报废后的房间级恢复流程；重启次数属于 RunState，避免切换房间后被重置。</summary>
public partial class RebootController : Node
{
    [Signal] public delegate void RebootedEventHandler();
    [Signal] public delegate void RunFailedEventHandler();
    private PlayerTank _player = null!;
    private HealthComponent _health = null!;
    private RelayStation _relay = null!;
    private RunState _run = null!;

    public override void _Ready()
    {
        _player = GetParent().GetNode<PlayerTank>("PlayerTank");
        _health = _player.GetNode<HealthComponent>("HealthComponent");
        _relay = GetParent().GetNode<RelayStation>("RelayStation");
        _run = GetTree().CurrentScene.GetNode<AppRoot>(".").CurrentRun;
        _health.Depleted += OnPlayerDepleted;
    }

    private async void OnPlayerDepleted()
    {
        _player.SetPhysicsProcess(false);
        _player.GetNode<CollisionShape2D>("BodyCollision").SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        if (!_run.TryConsumeReboot())
        {
            GD.PrintErr("本局失败：坦克报废且没有剩余战场重启次数。");
            EmitSignal(SignalName.RunFailed);
            return;
        }

        // 预留 1.2 秒报废反馈窗口；当前灰盒阶段不清除不存在的敌方炮弹。
        await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
        _player.GlobalPosition = _relay.GlobalPosition + Vector2.Right * 40f;
        _health.RestoreArmor(_health.MaximumArmor / 2);
        _player.GetNode<CollisionShape2D>("BodyCollision").SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _player.SetPhysicsProcess(true);
        EmitSignal(SignalName.Rebooted);
    }
}
