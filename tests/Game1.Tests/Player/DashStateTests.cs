using Godot;
using NUnit.Framework;

namespace Game1.Tests.Player;

public sealed class DashStateTests
{
    [Test]
    public void TryStart_RejectsZeroDirection()
    {
        DashState state = new(durationSeconds: 0.14f, cooldownSeconds: 0.8f);

        bool started = state.TryStart(Vector2.Zero);

        Assert.That(started, Is.False);
        Assert.That(state.IsDashing, Is.False);
        Assert.That(state.IsCoolingDown, Is.False);
    }

    [Test]
    public void Advance_EndsDashBeforeCooldownExpires()
    {
        DashState state = new(durationSeconds: 0.14f, cooldownSeconds: 0.8f);
        Assert.That(state.TryStart(Vector2.Right), Is.True);

        DashAdvanceResult result = state.Advance(0.14f);

        Assert.That(result, Is.EqualTo(DashAdvanceResult.Ended));
        Assert.That(state.IsDashing, Is.False);
        Assert.That(state.IsCoolingDown, Is.True);
    }
}
