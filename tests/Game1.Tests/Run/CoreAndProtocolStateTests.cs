using System;
using System.Linq;
using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class CoreAndProtocolStateTests
{
    [Test]
    public void DefaultCatalog_ContainsThreeUniqueCoresWithDistinctBuildTags()
    {
        CoreCatalog catalog = CoreCatalog.CreateDefault();

        Assert.That(catalog.Definitions.Select(definition => definition.Id).Distinct().Count(), Is.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(catalog.Get(CoreId.BreakthroughCannon).BuildTags, Does.Contain("artillery"));
            Assert.That(catalog.Get(CoreId.OverdriveAutocannon).BuildTags, Does.Contain("rapid_fire"));
            Assert.That(catalog.Get(CoreId.ElectricRider).BuildTags, Does.Contain("dash"));
        });
    }

    [Test]
    public void RunState_SelectsExactlyOneCoreForTheRun()
    {
        RunState state = RunState.CreateNew(seed: 14);

        state.SelectCore(CoreId.ElectricRider);

        Assert.That(state.SelectedCore, Is.EqualTo(CoreId.ElectricRider));
        Assert.That(() => state.SelectCore(CoreId.OverdriveAutocannon), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void RunState_UpgradesProtocolFromMkIToMkIIIThenRejectsFurtherUpgrade()
    {
        RunState state = RunState.CreateNew(seed: 14);

        Assert.That(state.UpgradeProtocol("frontline_piercing"), Is.EqualTo(ProtocolRank.MkI));
        Assert.That(state.UpgradeProtocol("frontline_piercing"), Is.EqualTo(ProtocolRank.MkII));
        Assert.That(state.UpgradeProtocol("frontline_piercing"), Is.EqualTo(ProtocolRank.MkIII));
        Assert.That(state.GetProtocolRank("frontline_piercing"), Is.EqualTo(ProtocolRank.MkIII));
        Assert.That(() => state.UpgradeProtocol("frontline_piercing"), Throws.TypeOf<InvalidOperationException>());
    }

}
