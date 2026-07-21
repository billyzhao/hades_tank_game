using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class ControlledStatOfferGeneratorTests
{
    [Test]
    public void Generate_ReturnsThreeUniqueOffersInStableOrder()
    {
        ControlledStatOfferGenerator generator = new();

        IReadOnlyList<StatUpgradeOffer> first = generator.Generate(123, 2, new Dictionary<StatUpgradeId, int>());
        IReadOnlyList<StatUpgradeOffer> second = generator.Generate(123, 2, new Dictionary<StatUpgradeId, int>());

        Assert.That(first.Select(offer => offer.Id), Is.EqualTo(second.Select(offer => offer.Id)));
        Assert.That(first.Select(offer => offer.Id).Distinct().Count(), Is.EqualTo(3));
    }

    [Test]
    public void Generate_ExcludesFullyStackedStat()
    {
        ControlledStatOfferGenerator generator = new();
        Dictionary<StatUpgradeId, int> stacks = new() { [StatUpgradeId.ArmorMax] = 3 };

        IReadOnlyList<StatUpgradeOffer> offers = generator.Generate(123, 2, stacks);

        Assert.That(offers.Any(offer => offer.Id == StatUpgradeId.ArmorMax), Is.False);
    }
}
