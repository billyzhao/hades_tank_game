using Godot;
using NUnit.Framework;

namespace Game1.Tests.Presentation;

public sealed class CameraShakeStateTests
{
    [Test]
    public void Advance_DecaysWithinRequestedStrengthAndStopsAtDuration()
    {
        CameraShakeState shake = new();
        shake.Start(strength: 4f, seconds: 0.1f);

        Vector2 firstOffset = shake.Advance(0.02f);
        shake.Advance(0.08f);
        Vector2 finalOffset = shake.Advance(0.01f);

        Assert.Multiple(() =>
        {
            Assert.That(firstOffset.Length(), Is.LessThanOrEqualTo(4f));
            Assert.That(firstOffset.Length(), Is.GreaterThan(0f));
            Assert.That(finalOffset, Is.EqualTo(Vector2.Zero));
        });
    }

    [Test]
    public void Start_WithNonPositiveValues_DisablesShake()
    {
        CameraShakeState shake = new();
        shake.Start(strength: 0f, seconds: 0.2f);

        Assert.That(shake.Advance(0.01f), Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void Start_WhileActive_AddsTraumaWithoutExceedingCombinedMaximum()
    {
        CameraShakeState shake = new();
        shake.Start(1f, .1f);
        Vector2 first = shake.Advance(.01f);
        shake.Start(2f, .2f);
        Vector2 combined = shake.Advance(.01f);

        Assert.Multiple(() =>
        {
            Assert.That(first.Length(), Is.GreaterThan(0f));
            Assert.That(combined.Length(), Is.GreaterThan(0f));
            Assert.That(combined.Length(), Is.LessThanOrEqualTo(3f));
        });
    }
}
