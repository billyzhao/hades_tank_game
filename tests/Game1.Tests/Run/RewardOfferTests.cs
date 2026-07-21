using System;
using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class RewardOfferTests
{
    [Test]
    public void Constructor_AcceptsExactlyThreeUniqueChoices()
    {
        RewardOffer offer = new(
            RewardKind.NormalProtocol,
            new[]
            {
                new RewardChoice("a", "A", "说明 A", new[] { "artillery" }),
                new RewardChoice("b", "B", "说明 B", new[] { "mobility" }),
                new RewardChoice("c", "C", "说明 C", new[] { "survival" })
            });

        Assert.That(offer.Choices, Has.Count.EqualTo(3));
        Assert.That(offer.Choices[0].Id, Is.EqualTo("a"));
    }

    [Test]
    public void Constructor_RejectsDuplicateOrBlankChoiceIds()
    {
        Assert.That(() => new RewardOffer(
            RewardKind.Maintenance,
            new[]
            {
                new RewardChoice("repair", "修复", "修复装甲", new[] { "repair" }),
                new RewardChoice("repair", "修复", "修复装甲", new[] { "repair" }),
                new RewardChoice("", "准备", "临时护盾", new[] { "guard" })
            }), Throws.TypeOf<ArgumentException>());
    }
}
