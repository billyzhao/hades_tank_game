using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class RunStateTests
{
    [Test]
    public void CreateNew_UsesApprovedMvpDefaults()
    {
        RunState state = RunState.CreateNew(seed: 42);

        Assert.That(state.Seed, Is.EqualTo(42));
        Assert.That(state.RelayIntegrity, Is.EqualTo(100));
        Assert.That(state.PlayerArmor, Is.EqualTo(100));
        Assert.That(state.RebootsRemaining, Is.EqualTo(1));
        Assert.That(state.RoomIndex, Is.Zero);
    }
}
