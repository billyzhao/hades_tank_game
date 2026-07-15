using Godot;

namespace Game1;

public enum DashAdvanceResult
{
    None,
    Ended
}

public sealed class DashState
{
    private readonly float _durationSeconds;
    private readonly float _cooldownSeconds;
    private float _dashRemaining;
    private float _cooldownRemaining;

    public DashState(float durationSeconds, float cooldownSeconds)
    {
        _durationSeconds = durationSeconds;
        _cooldownSeconds = cooldownSeconds;
    }

    public Vector2 Direction { get; private set; } = Vector2.Right;

    public bool IsDashing => _dashRemaining > 0f;

    public bool IsCoolingDown => _cooldownRemaining > 0f;

    public bool TryStart(Vector2 direction)
    {
        if (direction.IsZeroApprox() || IsDashing || IsCoolingDown)
        {
            return false;
        }

        Direction = direction.Normalized();
        _dashRemaining = _durationSeconds;
        return true;
    }

    public DashAdvanceResult Advance(float deltaSeconds)
    {
        if (IsDashing)
        {
            _dashRemaining = Mathf.Max(0f, _dashRemaining - deltaSeconds);
            if (!IsDashing)
            {
                _cooldownRemaining = _cooldownSeconds;
                return DashAdvanceResult.Ended;
            }
        }
        else if (IsCoolingDown)
        {
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaSeconds);
        }

        return DashAdvanceResult.None;
    }
}
