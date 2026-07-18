using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class StatPipelineTests
{
    [Test]
    public void Evaluate_AppliesFlatThenAdditiveThenMultiplicative()
    {
        float result = new StatPipeline().Evaluate(StatId.Damage, 10f, new[]
        {
            new StatModifier(StatId.Damage, 2f, 0f, 0f, "flat"),
            new StatModifier(StatId.Damage, 0f, .5f, 0f, "additive"),
            new StatModifier(StatId.Damage, 0f, 0f, 1f, "multiplicative")
        });

        Assert.That(result, Is.EqualTo(36f));
    }

    [Test]
    public void Evaluate_IgnoresModifiersForOtherStats()
    {
        float result = new StatPipeline().Evaluate(StatId.Damage, 10f, new[]
        {
            new StatModifier(StatId.DashCooldown, 999f, 1f, 1f, "wrong-stat")
        });

        Assert.That(result, Is.EqualTo(10f));
    }

    [Test]
    public void Evaluate_AppliesRoundingOnlyAfterAllModifiers()
    {
        float result = new StatPipeline().Evaluate(StatId.Damage, 9.5f, new[]
        {
            new StatModifier(StatId.Damage, .5f, .25f, 0f, "fractional")
        });

        Assert.That(result, Is.EqualTo(12.5f));
    }

    [Test]
    public void Evaluate_ClampsNegativeFinalValuesToZero()
    {
        float result = new StatPipeline().Evaluate(StatId.Damage, 10f, new[]
        {
            new StatModifier(StatId.Damage, -20f, 0f, 0f, "negative-final")
        });

        Assert.That(result, Is.EqualTo(0f));
    }
}
