using NUnit.Framework;

namespace Game1.Tests.Enemies;

public sealed class ThreatBudgetTests
{
    [Test]
    public void CanSpawn_RejectsSecondMortarAndOverBudgetSpawn()
    {
        ThreatBudget budget = new(total: 4);

        Assert.That(budget.TrySpend(BehaviorId.Mortar), Is.True);
        Assert.That(budget.TrySpend(BehaviorId.Mortar), Is.False);
        Assert.That(budget.TrySpend(BehaviorId.Assault), Is.False);
    }
}
