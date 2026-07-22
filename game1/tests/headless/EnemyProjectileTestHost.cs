using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证普通敌军攻击必须产生可命中玩家的敌方弹丸。</summary>
public partial class EnemyProjectileTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            Node2D arena = new() { Name = "EnemyProjectileArena" };
            AddChild(arena);

            PlayerTank player = GD.Load<PackedScene>("res://scenes/actors/player_tank.tscn").Instantiate<PlayerTank>();
            player.GlobalPosition = new Vector2(160f, 120f);
            player.AddToGroup("player");
            arena.AddChild(player);
            HealthComponent playerHealth = player.GetNode<HealthComponent>("HealthComponent");

            EnemyTank enemy = GD.Load<PackedScene>("res://scenes/actors/enemy_tank.tscn").Instantiate<EnemyTank>();
            enemy.GlobalPosition = new Vector2(220f, 120f);
            int firedProjectileCount = 0;
            enemy.ProjectileFired += () => firedProjectileCount++;
            arena.AddChild(enemy);

            await ToSignal(GetTree().CreateTimer(1.1d), SceneTreeTimer.SignalName.Timeout);
            Assert(firedProjectileCount > 0,
                "普通敌军完成攻击预警后必须发射可见的敌方炮弹，而不是直接扣除玩家装甲。");
            Assert(playerHealth.Armor < playerHealth.MaximumArmor,
                "敌方炮弹命中玩家碰撞体后必须通过统一生命组件扣除装甲。");

            foreach (Node projectile in GetTree().GetNodesInGroup("enemy_projectiles")) projectile.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            arena.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("[PASS] enemy_projectile_attack");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] enemy_projectile_attack: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
