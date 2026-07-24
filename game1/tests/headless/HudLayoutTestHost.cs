using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>保护竞技场主视野：常驻 HUD 只能占用左上角的紧凑边缘区域。</summary>
public partial class HudLayoutTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            PackedScene scene = GD.Load<PackedScene>("res://scenes/app/main.tscn");
            Node root = scene.Instantiate();
            Panel hudPanel = root.GetNode<Panel>("UI/HudPanel");
            Label buildLabel = root.GetNode<Label>("UI/BuildLabel");
            Label arenaLabel = root.GetNode<Label>("UI/Hud/ArenaLabel");

            Assert(hudPanel.Size.X <= 176f, "常驻 HUD 不能覆盖超过 176px 的左上战场宽度。");
            Assert(hudPanel.Size.Y <= 48f, "常驻 HUD 不能覆盖超过 48px 的左上战场高度。");
            Assert(buildLabel.Visible, "BC-02 必须常驻显示当前构筑路线，供玩家识别成长方向。");
            Assert(buildLabel.OffsetBottom - buildLabel.OffsetTop <= 11f,
                "构筑路线提示的场景布局必须保持 11px 单行高度，不得遮挡竞技场。");
            Assert(buildLabel.Text.Contains("构筑路线", StringComparison.Ordinal),
                "构筑提示必须直接使用“构筑路线”语义。");
            Assert(arenaLabel.Text == "封锁城区", "单区试玩不得继续显示竞技场 1/5。");
            Assert(!arenaLabel.Text.Contains("/5", StringComparison.Ordinal),
                "正式 HUD 不得暗示四张尚未交付的地图。");
            root.QueueFree();
            GD.Print("[PASS] hud_compact_layout");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] hud_compact_layout: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
