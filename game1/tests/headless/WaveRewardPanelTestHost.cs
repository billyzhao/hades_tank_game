using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证波间奖励面板展示三卡并且只提交所选 Id。</summary>
public partial class WaveRewardPanelTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            WaveRewardPanel panel = new();
            AddChild(panel);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            string chosen = string.Empty;
            panel.RewardConfirmed += id => chosen = id;
            panel.ShowOffer(new RewardOffer(
                RewardKind.NormalProtocol,
                new[]
                {
                    new RewardChoice("a", "选项 A", "A 说明", new[] { "artillery" }),
                    new RewardChoice("b", "选项 B", "B 说明", new[] { "mobility" }),
                    new RewardChoice("c", "选项 C", "C 说明", new[] { "survival" })
                }));
            Press(panel, "选项 B");

            Assert(chosen == "b", "点击奖励卡必须只提交对应的选择 Id。");
            Assert(!panel.Visible, "提交选择后奖励面板必须关闭。");
            GD.Print("[PASS] wave_reward_three_cards");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] wave_reward_three_cards: {exception.Message}");
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
        throw new InvalidOperationException($"找不到奖励卡：{text}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
