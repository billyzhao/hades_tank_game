using NUnit.Framework;

namespace Game1.Tests.Combat;

public sealed class DamageTests
{
    [Test]
    public void ApplyDamage_ConsumesShieldBeforeArmor()
    {
        HealthState state = new(armor: 10, shield: 5);

        DamageResult result = state.ApplyDamage(new DamageContext(Amount: 8));

        Assert.That(result.AppliedDamage, Is.EqualTo(3));
        Assert.That(state.Shield, Is.Zero);
        Assert.That(state.Armor, Is.EqualTo(7));
    }

    [Test]
    public void ApplyDamage_NegativeDamageCannotHealOrDepleteTwice()
    {
        HealthState state = new(armor: 5);
        state.ApplyDamage(new DamageContext(Amount: -3));
        DamageResult first = state.ApplyDamage(new DamageContext(Amount: 5));
        DamageResult second = state.ApplyDamage(new DamageContext(Amount: 1));

        Assert.That(state.Armor, Is.Zero);
        Assert.That(first.DepletedNow, Is.True);
        Assert.That(second.DepletedNow, Is.False);
    }
}
