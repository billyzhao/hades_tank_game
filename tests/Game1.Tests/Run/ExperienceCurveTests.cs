using System;
using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class ExperienceCurveTests
{
    [Test]
    public void GetRequiredExperience_UsesStrictlyIncreasingEarlyRunCurve()
    {
        ExperienceCurve curve = new();

        Assert.That(new[]
        {
            curve.GetRequiredExperience(1),
            curve.GetRequiredExperience(2),
            curve.GetRequiredExperience(3)
        }, Is.EqualTo(new[] { 20, 30, 40 }));
    }

    [Test]
    public void GetRequiredExperience_RejectsNonPositiveLevel()
    {
        ExperienceCurve curve = new();

        Assert.That(() => curve.GetRequiredExperience(0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
