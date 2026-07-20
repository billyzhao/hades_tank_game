using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>
/// Alpha 02B 的真实 Godot 运行时验收：验证中继站已退出场景，以及战场重启的可观察规则。
/// </summary>
public partial class MobileCoreSurvivalTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            AssertFocusPauseReleasesOnlyItsOwnReason();
            AssertProductionRoomsHaveNoRelayStation();
            await AssertRebootRestoresInPlaceAndPreservesCombatActors();
            GD.Print("[PASS] alpha_02b_mobile_core_survival");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] alpha_02b_mobile_core_survival: {exception.GetType().Name}: {exception.Message}");
            GD.PrintErr(exception.StackTrace ?? "<no stack trace>");
            GetTree().Quit(1);
        }
    }

    private void AssertFocusPauseReleasesOnlyItsOwnReason()
    {
        PauseCoordinator coordinator = new(GetTree());
        PauseController controller = new();
        controller.Configure(coordinator);
        AddChild(controller);
        try
        {
            controller.Notification((int)Node.NotificationApplicationFocusOut);
            Assert(GetTree().Paused && coordinator.Contains(PauseReason.FocusLost),
                "窗口失焦必须获取 FocusLost 暂停原因。");
            controller.Notification((int)Node.NotificationApplicationFocusIn);
            Assert(!GetTree().Paused && !coordinator.Contains(PauseReason.FocusLost),
                "重新聚焦必须自动释放 FocusLost 并恢复战斗。");

            coordinator.Acquire(PauseReason.Manual);
            controller.Notification((int)Node.NotificationApplicationFocusOut);
            controller.Notification((int)Node.NotificationApplicationFocusIn);
            Assert(GetTree().Paused && coordinator.Contains(PauseReason.Manual),
                "重新聚焦不得释放玩家主动获取的 Manual 暂停。");
            coordinator.Release(PauseReason.Manual);
        }
        finally
        {
            controller.QueueFree();
            coordinator.Release(PauseReason.FocusLost);
            coordinator.Release(PauseReason.Manual);
        }
    }

    private static void AssertProductionRoomsHaveNoRelayStation()
    {
        string[] roomPaths =
        {
            "res://scenes/rooms/mvp_combat_room.tscn",
            "res://scenes/rooms/industrial_flank_room.tscn",
            "res://scenes/rooms/mvp_boss_room.tscn"
        };

        foreach (string path in roomPaths)
        {
            Node room = GD.Load<PackedScene>(path).Instantiate();
            try
            {
                Assert(room.GetNodeOrNull("RelayStation") is null, $"{path} 不得保留 RelayStation 节点。");
            }
            finally
            {
                room.Free();
            }
        }
    }

    private async System.Threading.Tasks.Task AssertRebootRestoresInPlaceAndPreservesCombatActors()
    {
        Node2D room = GD.Load<PackedScene>("res://scenes/rooms/mvp_combat_room.tscn").Instantiate<Node2D>();
        AddChild(room);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        PlayerTank player = room.GetNode<PlayerTank>("PlayerTank");
        HealthComponent health = player.GetNode<HealthComponent>("HealthComponent");
        RebootController reboot = room.GetNode<RebootController>("RebootController");
        RunState state = RunState.CreateNew(seed: 20260720, maximumArmor: 100, reboots: 1);
        ContentCatalog catalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
        RunController run = new(state, new BuildController(state, catalog), new RewardGenerator());
        run.BeginRoom();
        run.Advance(0.6d);
        reboot.RebootDelaySeconds = 0.05f;
        reboot.ProtectionSeconds = 0.2f;
        reboot.KnockbackRadius = 96f;
        reboot.KnockbackDistance = 24f;
        reboot.Configure(run, state);
        health.InitializeArmor(state.PlayerArmor, state.MaximumArmor);
        health.ValueChanged += (armor, _) => state.SynchronizeArmor(armor, health.MaximumArmor);

        EnemyTank enemy = GD.Load<PackedScene>("res://scenes/actors/enemy_tank.tscn").Instantiate<EnemyTank>();
        enemy.GlobalPosition = player.GlobalPosition + new Vector2(30f, 0f);
        room.AddChild(enemy);
        enemy.SetPhysicsProcess(false);
        Vector2 enemyPosition = enemy.GlobalPosition;
        int enemyArmor = enemy.Armor;

        Node2D projectileSentinel = new() { Name = "ProjectileSentinel" };
        projectileSentinel.AddToGroup("projectiles");
        room.AddChild(projectileSentinel);

        Vector2 rebootPosition = player.GlobalPosition;
        health.ApplyDamage(new DamageContext(100));
        await ToSignal(GetTree().CreateTimer(0.1d), SceneTreeTimer.SignalName.Timeout);

        Assert(player.GlobalPosition.IsEqualApprox(rebootPosition), "重启后玩家必须保持报废坐标不变。");
        Assert(health.Armor == 50 && state.PlayerArmor == 50, "重启必须恢复向上取整的 50% 最大装甲并同步 RunState。");
        Assert(state.RebootsRemaining == 0, "重启只能消耗一次且不得恢复。");
        Assert(reboot.Phase == RebootPhase.Protected && health.InvulnerabilityRemaining > 0d,
            "重启完成后必须进入 2 秒保护阶段。");
        Assert(!enemy.GlobalPosition.IsEqualApprox(enemyPosition), "重启脉冲必须推动范围内普通敌军。");
        Assert(enemy.Armor == enemyArmor, "重启脉冲不得伤害敌军。");
        Assert(IsInstanceValid(projectileSentinel) && projectileSentinel.IsInsideTree(), "重启不得清除在场炮弹。");

        room.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
