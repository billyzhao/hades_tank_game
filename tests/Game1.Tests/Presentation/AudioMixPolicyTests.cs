using NUnit.Framework;

namespace Game1.Tests.Presentation;

[TestFixture]
public sealed class AudioMixPolicyTests
{
    [Test]
    public void LinearToDecibels_UsesPerceptualMappingAndFiniteSilence()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AudioMixPolicy.LinearToDecibels(1f), Is.EqualTo(0f).Within(.001f));
            Assert.That(AudioMixPolicy.LinearToDecibels(.5f), Is.EqualTo(-6.0206f).Within(.01f));
            Assert.That(AudioMixPolicy.LinearToDecibels(0f), Is.EqualTo(AudioMixPolicy.SilenceDb));
        });
    }

    [Test]
    public void CombatIntensity_RisesMonotonicallyAcrossFiveWaves()
    {
        float previous = AudioMixPolicy.CombatIntensityDb(1);
        for (int wave = 2; wave <= 5; wave++)
        {
            float current = AudioMixPolicy.CombatIntensityDb(wave);
            Assert.That(current, Is.GreaterThan(previous));
            previous = current;
        }
    }

    [TestCase(30, 100, true)]
    [TestCase(31, 100, false)]
    [TestCase(0, 100, false)]
    [TestCase(10, 0, false)]
    public void IsLowArmor_UsesThirtyPercentThreshold(int armor, int maximum, bool expected) =>
        Assert.That(AudioMixPolicy.IsLowArmor(armor, maximum), Is.EqualTo(expected));

    [TestCase(BehaviorId.Scout, AudioCue.EnemyScoutFire)]
    [TestCase(BehaviorId.Patrol, AudioCue.EnemyPatrolFire)]
    [TestCase(BehaviorId.Assault, AudioCue.EnemyAssaultFire)]
    [TestCase(BehaviorId.Mortar, AudioCue.EnemyMortarFire)]
    public void EnemyFireCue_PreservesRoleIdentity(BehaviorId behavior, AudioCue expected) =>
        Assert.That(AudioMixPolicy.EnemyFireCue(behavior), Is.EqualTo(expected));
}
