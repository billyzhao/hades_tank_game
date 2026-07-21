using NUnit.Framework;

namespace Game1.Tests.Content;

public sealed class WaveScheduleTests
{
    [Test]
    public void CreateApproved_ReturnsConfirmedDurationsRewardsAndEliteSlot()
    {
        IReadOnlyList<WaveSchedule> schedules = WaveSchedule.CreateApproved();

        Assert.Multiple(() =>
        {
            Assert.That(schedules.Select(schedule => schedule.WaveNumber), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(schedules.Select(schedule => schedule.SpawnDurationSeconds), Is.EqualTo(new[] { 45d, 50d, 55d, 60d, 70d }));
            Assert.That(schedules.Select(schedule => schedule.RewardKind), Is.EqualTo(new[]
            {
                RewardKind.NormalProtocol,
                RewardKind.Maintenance,
                RewardKind.NormalProtocol,
                RewardKind.Maintenance,
                RewardKind.RareProtocol
            }));
            Assert.That(schedules.Take(4).Any(schedule => schedule.IncludesElite), Is.False);
            Assert.That(schedules[4].IncludesElite, Is.True);
        });
    }
}
