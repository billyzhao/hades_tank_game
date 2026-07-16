using Godot;
using NUnit.Framework;

namespace Game1.Tests.Presentation;

public sealed class EnemyMotionVisualTests
{
    [Test]
    public void Calculate_WhenMoving_ReturnsVisibleTreadPulseWithinSafeBounds()
    {
        EnemyMotionPose pose = EnemyMotionVisual.Calculate(time: 0.25f, speed: 42f, baseScale: 0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(Mathf.Abs(pose.LateralOffset), Is.GreaterThan(0f).And.LessThanOrEqualTo(0.5f));
            Assert.That(pose.Scale.X, Is.InRange(0.48f, 0.52f));
            Assert.That(pose.Scale.Y, Is.InRange(0.48f, 0.52f));
        });
    }

    [Test]
    public void Calculate_WhenStopped_ReturnsNeutralPose()
    {
        EnemyMotionPose pose = EnemyMotionVisual.Calculate(time: 0.25f, speed: 0f, baseScale: 0.62f);

        Assert.That(pose, Is.EqualTo(new EnemyMotionPose(0f, Vector2.One * 0.62f)));
    }
}
