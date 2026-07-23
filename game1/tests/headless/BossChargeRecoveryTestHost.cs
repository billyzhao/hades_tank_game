using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>回归：Boss 二阶段在开阔地到达预警目标后也必须结束冲锋并暴露弱点。</summary>
public partial class BossChargeRecoveryTestHost : Node2D
{
    public override void _Ready()
    {
        try
        {
            PackedScene scene = GD.Load<PackedScene>("res://scenes/actors/roadblock_commander.tscn");
            BossDefinition definition = GD.Load<BossDefinition>("res://resources/bosses/roadblock_commander.tres");
            RoadblockCommander boss = scene.Instantiate<RoadblockCommander>();
            AddChild(boss);
            boss.GlobalPosition = new Vector2(120f, 120f);
            boss.ChargeTelegraphSeconds = 0.1f;
            boss.VulnerableSeconds = 1.5f;
            boss.Initialize(definition);

            boss.ApplyDamage(new DamageContext(151));
            boss.BeginCharge(boss.GlobalPosition + new Vector2(24f, 0f));
            for (int step = 0; step < 10; step++) boss._PhysicsProcess(0.05d);

            DamageResult weakPointHit = boss.ApplyDamage(new DamageContext(1));
            Assert(weakPointHit.AppliedDamage == 1,
                "二阶段在开阔地到达目标点后必须结束冲锋并进入可受伤窗口。 ");
            boss.Free();
            GD.Print("[PASS] boss_charge_recovers_without_collision");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] boss_charge_recovers_without_collision: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
