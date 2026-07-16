using Godot;
using NUnit.Framework;

namespace Game1.Tests.Presentation;

public sealed class EnemyVisualPaletteTests
{
    [TestCase(BehaviorId.Patrol, 0.95f, 0.75f, 0.20f)]
    [TestCase(BehaviorId.Assault, 1.00f, 0.42f, 0.25f)]
    [TestCase(BehaviorId.Siege, 0.72f, 0.30f, 0.85f)]
    public void GetRoleTint_ReturnsTheApprovedReadableRoleColor(BehaviorId behavior, float red, float green, float blue)
    {
        Color tint = EnemyVisualPalette.GetRoleTint(behavior);

        Assert.Multiple(() =>
        {
            Assert.That(tint.R, Is.EqualTo(red).Within(0.001f));
            Assert.That(tint.G, Is.EqualTo(green).Within(0.001f));
            Assert.That(tint.B, Is.EqualTo(blue).Within(0.001f));
        });
    }
}
