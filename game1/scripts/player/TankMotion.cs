using Godot;

namespace Game1;

public static class TankMotion
{
    public static Vector2 CalculateVelocity(Vector2 input, float speed)
    {
        return input.IsZeroApprox() ? Vector2.Zero : input.Normalized() * speed;
    }
}
