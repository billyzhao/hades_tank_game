using System.Reflection;
using NUnit.Framework;

namespace Game1.Tests.Run;

public sealed class RunStateTests
{
    [Test]
    public void CreateNew_UsesMobileCoreSurvivalDefaultsWithoutRelayState()
    {
        RunState state = RunState.CreateNew(seed: 42);
        PropertyInfo? maximumArmor = typeof(RunState).GetProperty("MaximumArmor");

        Assert.Multiple(() =>
        {
            Assert.That(state.Seed, Is.EqualTo(42));
            Assert.That(state.PlayerArmor, Is.EqualTo(100));
            Assert.That(state.RebootsRemaining, Is.EqualTo(1));
            Assert.That(maximumArmor, Is.Not.Null, "RunState 必须显式持有最大装甲。");
            Assert.That(maximumArmor?.GetValue(state), Is.EqualTo(100));
            Assert.That(typeof(RunState).GetProperty("RelayIntegrity"), Is.Null,
                "移动核心方案不得保留第二条中继生命线。");
        });
    }

    [Test]
    public void RestoreAfterReboot_UsesCeilingHalfMaximumArmor()
    {
        RunState state = RunState.CreateNew(seed: 42, maximumArmor: 99);
        MethodInfo? restore = typeof(RunState).GetMethod("RestoreAfterReboot");

        Assert.That(restore, Is.Not.Null, "RunState 缺少重启恢复接口。");
        restore!.Invoke(state, null);

        Assert.That(state.PlayerArmor, Is.EqualTo(50));
    }

    [Test]
    public void RestoreArmorForNextArena_FillsArmorWithoutRestoringReboot()
    {
        RunState state = RunState.CreateNew(seed: 42, maximumArmor: 100, reboots: 1);
        state.SynchronizeArmor(1, 100);
        Assert.That(state.TryConsumeReboot(), Is.True);
        MethodInfo? restore = typeof(RunState).GetMethod("RestoreArmorForNextArena");

        Assert.That(restore, Is.Not.Null, "RunState 缺少 Boss 后全修接口。");
        restore!.Invoke(state, null);

        Assert.Multiple(() =>
        {
            Assert.That(state.PlayerArmor, Is.EqualTo(100));
            Assert.That(state.RebootsRemaining, Is.Zero);
        });
    }
}
