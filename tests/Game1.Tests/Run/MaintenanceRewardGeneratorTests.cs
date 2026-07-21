using System.Linq;
using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class MaintenanceRewardGeneratorTests
{
    [Test]
    public void Generate_BelowThirtyPercentAlwaysIncludesArmorRepair()
    {
        MaintenanceRewardGenerator generator = new();

        RewardOffer offer = generator.Generate(seed: 77, armor: 29, maximumArmor: 100);

        Assert.That(offer.Kind, Is.EqualTo(RewardKind.Maintenance));
        Assert.That(offer.Choices.Select(choice => choice.Id), Does.Contain("maintenance_repair_25"));
    }
}
