using Godot;

namespace Game1;

public partial class AppRoot : Node
{
    private static readonly PackedScene MvpCombatRoomScene = GD.Load<PackedScene>("res://scenes/rooms/mvp_combat_room.tscn");

    public RunState CurrentRun { get; private set; } = null!;

    public override void _Ready()
    {
        CurrentRun = RunState.CreateNew(System.Environment.TickCount);
        Node roomHost = GetNode<Node>("RoomHost");
        roomHost.AddChild(MvpCombatRoomScene.Instantiate<Node2D>());
    }
}
