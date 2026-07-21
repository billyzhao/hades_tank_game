using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class ArenaControllerTests
{
    [Test]
    public void FiveWaveFlow_RequiresSpawnEndCleanupAndRewardConfirmation()
    {
        RunState runState = RunState.CreateNew(seed: 20260721);
        ArenaController controller = new(runState);
        List<RewardKind> requestedRewards = new();
        controller.RewardRequested += requestedRewards.Add;

        controller.BeginArena(WaveSchedule.CreateApproved(), arenaIndex: 0);

        Assert.Multiple(() =>
        {
            Assert.That(controller.State, Is.EqualTo(ArenaState.Intro));
            Assert.That(controller.CurrentWave, Is.EqualTo(1));
            Assert.That(runState.WaveIndex, Is.EqualTo(0));
        });

        controller.OnIntroFinished();

        for (int waveNumber = 1; waveNumber <= 5; waveNumber++)
        {
            Assert.That(controller.State, Is.EqualTo(ArenaState.WaveCombat));
            controller.OnWaveSpawnWindowEnded();
            Assert.That(controller.State, Is.EqualTo(ArenaState.Cleanup));
            controller.OnAllEnemiesCleared();
            Assert.That(controller.State, Is.EqualTo(ArenaState.Reward));
            Assert.That(controller.CurrentRewardKind, Is.EqualTo(ExpectedReward(waveNumber)));

            controller.ConfirmReward($"wave_{waveNumber}_accepted");
            if (waveNumber < 5)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(controller.State, Is.EqualTo(ArenaState.WaveCombat));
                    Assert.That(controller.CurrentWave, Is.EqualTo(waveNumber + 1));
                    Assert.That(runState.WaveIndex, Is.EqualTo(waveNumber));
                });
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(controller.State, Is.EqualTo(ArenaState.BossIntro));
            Assert.That(requestedRewards, Is.EqualTo(new[]
            {
                RewardKind.NormalProtocol,
                RewardKind.Maintenance,
                RewardKind.NormalProtocol,
                RewardKind.Maintenance,
                RewardKind.RareProtocol
            }));
        });
    }

    [Test]
    public void CleanupAndRewardTransitions_RejectOutOfOrderCalls()
    {
        ArenaController controller = new(RunState.CreateNew(seed: 9));
        controller.BeginArena(WaveSchedule.CreateApproved(), arenaIndex: 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(controller.OnAllEnemiesCleared);
            Assert.Throws<InvalidOperationException>(() => controller.ConfirmReward("early"));
        });

        controller.OnIntroFinished();
        Assert.Throws<InvalidOperationException>(controller.OnAllEnemiesCleared);
        controller.OnWaveSpawnWindowEnded();
        controller.OnAllEnemiesCleared();
        Assert.Throws<ArgumentException>(() => controller.ConfirmReward(" "));
    }

    [Test]
    public void PlayerFailure_StopsFurtherWaveProgress()
    {
        ArenaController controller = new(RunState.CreateNew(seed: 17));
        controller.BeginArena(WaveSchedule.CreateApproved(), arenaIndex: 0);
        controller.OnIntroFinished();

        controller.OnPlayerRunFailed();

        Assert.That(controller.State, Is.EqualTo(ArenaState.Failed));
        Assert.Throws<InvalidOperationException>(controller.OnWaveSpawnWindowEnded);
    }

    private static RewardKind ExpectedReward(int waveNumber) => waveNumber switch
    {
        1 or 3 => RewardKind.NormalProtocol,
        2 or 4 => RewardKind.Maintenance,
        5 => RewardKind.RareProtocol,
        _ => throw new ArgumentOutOfRangeException(nameof(waveNumber))
    };
}
