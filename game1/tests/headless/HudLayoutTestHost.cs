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

            Assert(hudPanel.Size.X <= 176f, "常驻 HUD 不能覆盖超过 176px 的左上战场宽度。");
            Assert(hudPanel.Size.Y <= 48f, "常驻 HUD 不能覆盖超过 48px 的左上战场高度。");
            Assert(!buildLabel.Visible, "Alpha 02E 前的构筑占位提示不得常驻遮挡竞技场。");
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
