using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证靠近吸收一次、波末回收全部剩余数据，避免经验丢失或重复。</summary>
public partial class CombatDataTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            Node2D arena = new();
            AddChild(arena);
            Node2D player = new() { Position = new Vector2(120f, 120f) };
            player.AddToGroup("player");
            arena.AddChild(player);
            CombatDataCollector collector = new();
            arena.AddChild(collector);
            int collected = 0;
            collector.DataCollected += amount => collected += amount;

            collector.Spawn(arena, new Vector2(130f, 120f), 5);
            await ToSignal(GetTree().CreateTimer(0.05d), SceneTreeTimer.SignalName.Timeout);
            Assert(collected == 5 && collector.PendingPickupCount == 0, "靠近玩家的数据必须只收集一次。 ");

            collector.Spawn(arena, new Vector2(300f, 120f), 5);
            collector.Spawn(arena, new Vector2(320f, 120f), 5);
            collector.CollectAllAtWaveEnd();
            Assert(collected == 15 && collector.PendingPickupCount == 0, "波末回收必须收集全部剩余数据且不重复。 ");
            GD.Print("[PASS] combat_data_collection");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] combat_data_collection: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
