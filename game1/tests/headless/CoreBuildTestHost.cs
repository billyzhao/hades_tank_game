using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>在已初始化的 Godot 运行时验证核心基础属性只能通过 BuildController 注入。</summary>
public partial class CoreBuildTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            RunState state = RunState.CreateNew(seed: 31);
            BuildController build = new(state, CreateCatalog());
            build.ApplyCore(CoreCatalog.CreateDefault().Get(CoreId.BreakthroughCannon));

            Assert(state.SelectedCore == CoreId.BreakthroughCannon, "核心选择必须写入单局状态。");
            Assert(Mathf.IsEqualApprox(build.EvaluateStat(StatId.Damage, 10f), 14f), "核心伤害修正必须经过统一属性管线。");
            AssertThrows(
                () => build.ApplyCore(CoreCatalog.CreateDefault().Get(CoreId.ElectricRider)),
                "核心不能在同一局中重复更换。");

            build.SelectProtocol("core_test_protocol");
            Assert(state.GetProtocolRank("core_test_protocol") == ProtocolRank.MkI, "首次获得协议必须成为 Mk.I。");
            build.SelectProtocol("core_test_protocol");
            Assert(state.GetProtocolRank("core_test_protocol") == ProtocolRank.MkII, "重复获得同一协议必须升级到 Mk.II，而不是被旧叠层规则拒绝。");

            GD.Print("[PASS] core_build_pipeline");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] core_build_pipeline: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static ContentCatalog CreateCatalog()
    {
        ProtocolEffectDefinition effect = new()
        {
            EffectId = "core_test_effect",
            Stat = StatId.Damage,
            FlatAdd = 1f
        };
        ProtocolDefinition protocol = new()
        {
            Id = "core_test_protocol",
            DisplayName = "核心测试协议",
            Description = "只用于验证核心属性管线。",
            Department = ProtocolDepartment.Arsenal,
            Rarity = 1,
            BaseWeight = 1f,
            StackLimit = 1,
            Effects = new Godot.Collections.Array<ProtocolEffectDefinition> { effect }
        };
        return new ContentCatalog
        {
            Version = "core-test-v1",
            Protocols = new Godot.Collections.Array<ProtocolDefinition> { protocol }
        };
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }
}
