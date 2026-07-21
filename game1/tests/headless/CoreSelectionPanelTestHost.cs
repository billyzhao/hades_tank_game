using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证开局核心卡在暂停状态仍可选择，并且只提交一次核心 Id。</summary>
public partial class CoreSelectionPanelTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            CoreSelectionPanel panel = new();
            AddChild(panel);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            int received = 0;
            CoreId chosen = default;
            panel.CoreChosen += core =>
            {
                received++;
                chosen = core;
            };
            panel.ShowChoices(CoreCatalog.CreateDefault());
            Press(panel, "突破重炮核心");

            Assert(received == 1 && chosen == CoreId.BreakthroughCannon, "核心卡必须只提交一次所选核心。\n");
            Assert(!panel.Visible, "选择后核心面板必须关闭。\n");
            GD.Print("[PASS] core_selection_panel");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] core_selection_panel: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Press(Node root, string text)
    {
        foreach (Node node in root.FindChildren("*", "Button", true, false))
        {
            if (node is Button button && button.Text == text)
            {
                button.EmitSignal(Button.SignalName.Pressed);
                return;
            }
        }
        throw new InvalidOperationException($"找不到核心按钮：{text}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
