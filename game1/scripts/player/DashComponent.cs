using Godot;

namespace Game1;

public partial class DashComponent : Node
{
    [Export] public float SpeedMultiplier { get; set; } = 3f;

    [Export] public float DurationSeconds { get; set; } = 0.14f;

    [Export] public float CooldownSeconds { get; set; } = 0.8f;

    private DashState _state = null!;

    [Signal] public delegate void DashStartedEventHandler();

    [Signal] public delegate void DashEndedEventHandler();

    public Vector2 Direction => _state.Direction;

    public bool IsDashing => _state.IsDashing;

    public bool IsCoolingDown => _state.IsCoolingDown;

    public override void _Ready()
    {
        _state = new DashState(DurationSeconds, CooldownSeconds);
    }

    public bool TryStart(Vector2 direction)
    {
        if (!_state.TryStart(direction))
        {
            return false;
        }

        EmitSignal(SignalName.DashStarted);
        return true;
    }

    public void Advance(float deltaSeconds)
    {
        if (_state.Advance(deltaSeconds) == DashAdvanceResult.Ended)
        {
            EmitSignal(SignalName.DashEnded);
        }
    }
}
