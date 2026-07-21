using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证 Debug 验收菜单提供波次闭环的明确命令，且每个按钮只发出请求。</summary>
public partial class AcceptanceMenuTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            AcceptanceMenu menu = new();
            AddChild(menu);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            int clearCount = 0;
            int completeCount = 0;
            int advanceCount = 0;
            int endRunCount = 0;
            menu.ClearWaveEnemiesRequested += () => clearCount++;
            menu.CompleteWaveRequested += () => completeCount++;
            menu.AdvanceWaveRequested += () => advanceCount++;
            menu.EndRunRequested += () => endRunCount++;

            Press(menu, "敌军全灭（当前波）");
            Press(menu, "结束本轮并结算");
            Press(menu, "确认并到下一波");
            Press(menu, "结束本局（验收）");

            Assert(clearCount == 1, "敌军全灭按钮必须只发出一次清场请求。");
            Assert(completeCount == 1, "结束本轮按钮必须只发出一次结算请求。");
            Assert(advanceCount == 1, "到下一波按钮必须只发出一次推进请求。");
            Assert(endRunCount == 1, "结束本局按钮必须只发出一次结束请求。");
            GD.Print("[PASS] acceptance_menu_commands");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] acceptance_menu_commands: {exception.Message}");
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
        throw new InvalidOperationException($"未找到验收按钮：{text}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
