using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证每次获得战斗数据后，右上经验 HUD 必须在同一帧刷新。</summary>
public partial class ExperienceHudRefreshTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            PackedScene scene = GD.Load<PackedScene>("res://scenes/app/main.tscn");
            Node root = scene.Instantiate();
            AddChild(root);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            AcceptanceMenu menu = root.GetNode<AcceptanceMenu>("UI/AcceptanceMenu");
            Label experience = root.GetNode<Label>("UI/ExperienceLabel");
            menu.EmitSignal(AcceptanceMenu.SignalName.ExperienceRequested, 5);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            Assert(experience.Text == "数据 5/20", "获得战斗数据后，经验 HUD 必须立即显示最新数值。");
            root.QueueFree();
            GD.Print("[PASS] experience_hud_refresh");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] experience_hud_refresh: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
