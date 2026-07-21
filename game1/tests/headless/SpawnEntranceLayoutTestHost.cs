using System;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>四向入口必须落在可见墙体内侧的开放出生带，不得埋入边缘装饰或碰撞边界。</summary>
public partial class SpawnEntranceLayoutTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            Node2D room = GD.Load<PackedScene>("res://scenes/rooms/mvp_combat_room.tscn").Instantiate<Node2D>();
            Marker2D[] markers = room.GetNode<Node>("SpawnEntrances").GetChildren().OfType<Marker2D>().ToArray();
            Assert(markers.Length == 4, "竞技场必须保留四个入口。 ");
            foreach (Marker2D marker in markers)
            {
                Vector2 position = marker.Position;
                Assert(position.X >= 60f && position.X <= 420f && position.Y >= 60f && position.Y <= 210f,
                    $"入口 {marker.Name} 必须位于距场地边缘至少 60px 的开放出生带内。 ");
            }
            room.QueueFree();
            GD.Print("[PASS] spawn_entrance_layout");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] spawn_entrance_layout: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
