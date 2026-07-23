using NUnit.Framework;

namespace Game1.Tests.Enemies;

public sealed class ThreatWavePlanTests
{
    [Test]
    public void CreateMvp_ReturnsThreeWavesMatchingApprovedThreatBudgets()
    {
        IReadOnlyList<IReadOnlyList<BehaviorId>> waves = ThreatWavePlan.CreateMvp();

        Assert.Multiple(() =>
        {
            Assert.That(waves, Has.Count.EqualTo(3));
            Assert.That(ThreatWavePlan.GetThreatCost(waves[0]), Is.EqualTo(4));
            Assert.That(ThreatWavePlan.GetThreatCost(waves[1]), Is.EqualTo(6));
            Assert.That(ThreatWavePlan.GetThreatCost(waves[2]), Is.EqualTo(8));
        });
    }

    [Test]
    public void CreateMvp_NeverPlacesMoreThanOneMortarInASingleWave()
    {
        foreach (IReadOnlyList<BehaviorId> wave in ThreatWavePlan.CreateMvp())
        {
            Assert.That(wave.Count(behavior => behavior == BehaviorId.Mortar), Is.LessThanOrEqualTo(1));
        }
    }
}
