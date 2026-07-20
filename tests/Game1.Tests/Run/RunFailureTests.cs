using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class RunFailureTests
{
    [Test]
    public void TryConsumeReboot_ConsumesOnceWithoutGoingNegative()
    {
        RunState state = RunState.CreateNew(seed: 1, reboots: 1);

        Assert.That(state.TryConsumeReboot(), Is.True);
        Assert.That(state.TryConsumeReboot(), Is.False);
        Assert.That(state.RebootsRemaining, Is.Zero);
    }
}
