using Godot;

namespace Game1;

public partial class AppRoot : Node
{
    public RunState CurrentRun { get; private set; } = null!;

    public override void _Ready()
    {
        CurrentRun = RunState.CreateNew(System.Environment.TickCount);
    }
}
