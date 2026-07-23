using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>Alpha 02F 的槽位与内容合同；运行时行为仍由可见验收确认。</summary>
public partial class AuxiliarySystemTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            ContentCatalog catalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
            catalog.Validate();
            Assert(catalog.Auxiliaries.Count == 4, "内容目录必须恰好有四种辅助系统。");

            RunState state = RunState.CreateNew(20260722);
            BuildController build = new(state, catalog);
            Assert(build.AddOrUpgradeAuxiliary("aux_side_cannon") == 1, "首个辅助必须占用第一槽。");
            Assert(build.AddOrUpgradeAuxiliary("aux_orbit_drone") == 1, "第二个不同辅助必须占用第二槽。");
            Assert(state.AuxiliarySlots.Count == 2, "辅助槽上限必须是两个。");
            Assert(build.AddOrUpgradeAuxiliary("aux_side_cannon") == 2, "重复辅助必须升级现有槽位。");
            AssertThrows(() => build.AddOrUpgradeAuxiliary("aux_mine_layer"), "满槽后不得加入第三种辅助。");
            Assert(state.GetAuxiliaryRank("aux_side_cannon") == 2, "升级后的辅助等级必须写入单局状态。");
            GD.Print("[PASS] auxiliary_slots_and_catalog");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] auxiliary_slots_and_catalog: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException(message);
    }
}
