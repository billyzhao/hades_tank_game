using NUnit.Framework;

namespace Game1.Tests.Enemies;

public sealed class TargetPolicyTests
{
    [TestCase(BehaviorId.Patrol)]
    [TestCase(BehaviorId.Assault)]
    [TestCase(BehaviorId.Mortar)]
    public void SelectTarget_AllCurrentRolesTargetPlayer(BehaviorId behavior)
    {
        TargetId target = TargetPolicy.SelectTarget(behavior, new TargetSnapshot(PlayerAvailable: true));
        Assert.That(target, Is.EqualTo(TargetId.Player));
    }

    [Test]
    public void SelectTarget_ReturnsNoneWhenPlayerIsUnavailable()
    {
        TargetId target = TargetPolicy.SelectTarget(BehaviorId.Mortar, new TargetSnapshot(PlayerAvailable: false));
        Assert.That(target, Is.EqualTo(TargetId.None));
    }
}
