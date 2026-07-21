using System;
using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class ExperienceRunStateTests
{
    [Test]
    public void AddExperience_QueuesEveryLevelFromSingleLargeCollectionInFifoOrder()
    {
        RunState state = RunState.CreateNew(seed: 8);
        ExperienceCurve curve = new();

        state.AddExperience(95, curve);

        Assert.Multiple(() =>
        {
            Assert.That(state.Level, Is.EqualTo(4));
            Assert.That(state.Experience, Is.EqualTo(5));
            Assert.That(state.PendingLevelUps, Is.EqualTo(3));
        });
        Assert.That(state.TryConsumePendingLevel(out int first), Is.True);
        Assert.That(state.TryConsumePendingLevel(out int second), Is.True);
        Assert.That(state.TryConsumePendingLevel(out int third), Is.True);
        Assert.That(state.TryConsumePendingLevel(out _), Is.False);
        Assert.That(new[] { first, second, third }, Is.EqualTo(new[] { 2, 3, 4 }));
    }

    [Test]
    public void AddExperience_RejectsNegativeAmount()
    {
        RunState state = RunState.CreateNew(seed: 8);

        Assert.That(() => state.AddExperience(-1, new ExperienceCurve()), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
