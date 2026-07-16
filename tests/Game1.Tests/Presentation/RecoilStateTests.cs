using NUnit.Framework;

namespace Game1.Tests.Presentation;

public sealed class RecoilStateTests
{
    [Test]
    public void Advance_AfterKickReturnsDecayingOffsetThenZero()
    {
        RecoilState recoil = new();
        recoil.Kick(pixels: 2f, seconds: 0.1f);

        float firstOffset = recoil.Advance(0.02f);
        recoil.Advance(0.08f);
        float finalOffset = recoil.Advance(0.01f);

        Assert.Multiple(() =>
        {
            Assert.That(firstOffset, Is.GreaterThan(0f).And.LessThanOrEqualTo(2f));
            Assert.That(finalOffset, Is.Zero);
        });
    }
}
