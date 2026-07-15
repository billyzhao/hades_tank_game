using Godot;
using NUnit.Framework;

namespace Game1.Tests.Combat;

public sealed class ProjectileMathTests
{
    [Test]
    public void Reflect_HorizontalWall_ReversesVerticalTravel()
    {
        Vector2 reflected = ProjectileMath.Reflect(new Vector2(1f, 1f), Vector2.Up);

        Assert.That(reflected.X, Is.EqualTo(new Vector2(1f, -1f).Normalized().X).Within(0.001f));
        Assert.That(reflected.Y, Is.EqualTo(new Vector2(1f, -1f).Normalized().Y).Within(0.001f));
    }

    [Test]
    public void Reflect_FortyFiveDegreeImpact_ProducesSymmetricExit()
    {
        Vector2 reflected = ProjectileMath.Reflect(new Vector2(1f, -1f), Vector2.Left);

        Assert.That(reflected.X, Is.EqualTo(new Vector2(-1f, -1f).Normalized().X).Within(0.001f));
        Assert.That(reflected.Y, Is.EqualTo(new Vector2(-1f, -1f).Normalized().Y).Within(0.001f));
    }
}
