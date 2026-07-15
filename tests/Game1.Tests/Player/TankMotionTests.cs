using Godot;
using NUnit.Framework;

namespace Game1.Tests.Player;

public sealed class TankMotionTests
{
    [Test]
    public void CalculateVelocity_NormalizesDiagonalInput()
    {
        Vector2 velocity = TankMotion.CalculateVelocity(Vector2.One, 120f);

        Assert.That(velocity.Length(), Is.EqualTo(120f).Within(0.001f));
    }
}
