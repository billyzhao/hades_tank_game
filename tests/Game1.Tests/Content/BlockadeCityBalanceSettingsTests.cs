using NUnit.Framework;

namespace Game1.Tests.Content;

public sealed class BlockadeCityBalanceSettingsTests
{
    [Test]
    public void DesignBaseline_DoesNotChangeApprovedValues()
    {
        BlockadeCityBalanceSettings baseline = BlockadeCityBalanceSettings.DesignBaseline;

        baseline.Validate();
        Assert.That(baseline.SpawnRateMultiplier, Is.EqualTo(1f));
        Assert.That(baseline.MaximumAliveAdjustment, Is.Zero);
        Assert.That(baseline.EnemyMoveSpeedMultiplier, Is.EqualTo(1f));
        Assert.That(baseline.EnemyAttackRateMultiplier, Is.EqualTo(1f));
        Assert.That(baseline.EnemyArmorMultiplier, Is.EqualTo(1f));
        Assert.That(baseline.PlayerMoveSpeedMultiplier, Is.EqualTo(1f));
        Assert.That(baseline.PlayerFireRateMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void ClampToApprovedRange_ClampsEveryDesignerControl()
    {
        BlockadeCityBalanceSettings result = new BlockadeCityBalanceSettings(
            99f, -99, 0f, 99f, 0f, 99f, 0f).ClampToApprovedRange();

        Assert.That(result, Is.EqualTo(new BlockadeCityBalanceSettings(
            2.5f, -3, 0.75f, 1.75f, 0.5f, 1.3f, 0.75f)));
    }

    [Test]
    public void Presets_AreDeterministicAndValid()
    {
        BlockadeCityBalanceSettings.DensePreset.Validate();
        BlockadeCityBalanceSettings.HighPressurePreset.Validate();

        Assert.That(BlockadeCityBalanceSettings.DensePreset.SpawnRateMultiplier, Is.EqualTo(1.5f));
        Assert.That(BlockadeCityBalanceSettings.DensePreset.MaximumAliveAdjustment, Is.EqualTo(2));
        Assert.That(BlockadeCityBalanceSettings.HighPressurePreset.SpawnRateMultiplier, Is.EqualTo(2f));
        Assert.That(BlockadeCityBalanceSettings.HighPressurePreset.MaximumAliveAdjustment, Is.EqualTo(4));
    }

}
