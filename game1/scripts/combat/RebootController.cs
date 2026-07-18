using Godot;

namespace Game1;

/// <summary>玩家报废后的房间级恢复流程；重启次数属于 RunState，切换房间后不会重置。</summary>
public partial class RebootController : Node
{
    [Export] public float RebootDelaySeconds { get; set; } = 1.2f;
    [Export] public float RespawnInvulnerabilitySeconds { get; set; } = 1.2f;
    [Export] public float ProjectileClearRadius { get; set; } = 160f;

    [Signal] public delegate void RebootedEventHandler();
    [Signal] public delegate void RunFailedEventHandler();

    private PlayerTank _player = null!;
    private HealthComponent _health = null!;
    private RelayStation _relay = null!;
    private AppRoot _appRoot = null!;

    public override void _Ready()
    {
        _player = GetParent().GetNode<PlayerTank>("PlayerTank");
        _health = _player.GetNode<HealthComponent>("HealthComponent");
        _relay = GetParent().GetNode<RelayStation>("RelayStation");
        _appRoot = GetTree().CurrentScene.GetNode<AppRoot>(".");
        _health.Depleted += OnPlayerDepleted;
    }

    private async void OnPlayerDepleted()
    {
        _player.SetPhysicsProcess(false);
        _player.GetNode<CollisionShape2D>("BodyCollision")
            .SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        if (!_appRoot.TryHandleTankDefeat())
        {
            GD.PrintErr("本局失败：坦克报废且没有剩余战场重启次数。");
            EmitSignal(SignalName.RunFailed);
            return;
        }

        ClearNearbyEnemyProjectiles();
        await ToSignal(GetTree().CreateTimer(RebootDelaySeconds), SceneTreeTimer.SignalName.Timeout);

        _player.GlobalPosition = _relay.GlobalPosition + Vector2.Right * 40f;
        _health.RestoreArmor(_health.MaximumArmor / 2);
        _health.GrantInvulnerability(RespawnInvulnerabilitySeconds);
        _player.Modulate = new Color(0.45f, 0.95f, 1f, 0.65f);
        _player.CreateTween()
            .TweenProperty(_player, "modulate", Colors.White, RespawnInvulnerabilitySeconds);
        _player.GetNode<CollisionShape2D>("BodyCollision")
            .SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _player.SetPhysicsProcess(true);
        EmitSignal(SignalName.Rebooted);
    }

    /// <summary>只清理复位点附近敌弹，防止复活即死，同时保留远处战场压力。</summary>
    private void ClearNearbyEnemyProjectiles()
    {
        foreach (Node node in GetTree().GetNodesInGroup("enemy_projectiles"))
        {
            if (node is Node2D projectile
                && projectile.GlobalPosition.DistanceTo(_relay.GlobalPosition) <= ProjectileClearRadius)
            {
                projectile.QueueFree();
            }
        }
    }
}
