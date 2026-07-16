using NUnit.Framework;

namespace Game1.Tests.Enemies;

public sealed class TargetPolicyTests
{
    [TestCase(BehaviorId.Patrol, TargetId.Player)]
    [TestCase(BehaviorId.Assault, TargetId.Player)]
    [TestCase(BehaviorId.Siege, TargetId.Relay)]
    public void SelectTarget_UsesRolePreferredTarget(BehaviorId behavior, TargetId expected)
    {
        TargetId target = TargetPolicy.SelectTarget(behavior, new TargetSnapshot(PlayerAvailable: true, RelayAvailable: true));
        Assert.That(target, Is.EqualTo(expected));
    }

    [Test]
    public void SelectTarget_FallsBackWhenPreferredTargetIsUnavailable()
    {
        TargetId target = TargetPolicy.SelectTarget(BehaviorId.Siege, new TargetSnapshot(PlayerAvailable: true, RelayAvailable: false));
        Assert.That(target, Is.EqualTo(TargetId.Player));
    }
}
