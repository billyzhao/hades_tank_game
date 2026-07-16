using Godot;
using NUnit.Framework;

namespace Game1.Tests.Enemies;

public sealed class DefenseRoutePlannerTests
{
    [Test]
    public void GetNextPoint_WhenCrossingCentralDefenses_UsesUpperOrLowerLane()
    {
        Vector2 upper = DefenseRoutePlanner.GetNextPoint(new Vector2(438, 42), new Vector2(80, 135));
        Vector2 lower = DefenseRoutePlanner.GetNextPoint(new Vector2(438, 228), new Vector2(80, 135));

        Assert.Multiple(() =>
        {
            Assert.That(upper, Is.EqualTo(new Vector2(230, 72)));
            Assert.That(lower, Is.EqualTo(new Vector2(230, 198)));
        });
    }

    [Test]
    public void GetNextPoint_WhenAlreadyPastTheRouteGate_GoesDirectlyToTarget()
    {
        Vector2 target = new(80, 135);

        Assert.That(DefenseRoutePlanner.GetNextPoint(new Vector2(225, 72), target), Is.EqualTo(target));
    }

    [Test]
    public void GetNextPoint_WhenPlayerIsNearCenter_StillRoutesAroundCentralSteelWall()
    {
        Vector2 nextPoint = DefenseRoutePlanner.GetNextPoint(new Vector2(438, 42), new Vector2(240, 135));

        Assert.That(nextPoint, Is.EqualTo(new Vector2(230, 72)));
    }
}
