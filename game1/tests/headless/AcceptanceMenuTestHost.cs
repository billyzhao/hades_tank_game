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
            int bossPhaseTwoCount = 0;
            int bossDefeatCount = 0;
            int tuningCount = 0;
            int saveCount = 0;
            int protocolCount = 0;
            int auxiliaryCount = 0;
            string receivedProtocol = string.Empty;
            string receivedAuxiliary = string.Empty;
            BlockadeCityBalanceSettings receivedTuning = BlockadeCityBalanceSettings.DesignBaseline;
            menu.ClearWaveEnemiesRequested += () => clearCount++;
            menu.CompleteWaveRequested += () => completeCount++;
            menu.AdvanceWaveRequested += () => advanceCount++;
            menu.EndRunRequested += () => endRunCount++;
            menu.BossPhaseTwoRequested += () => bossPhaseTwoCount++;
            menu.BossDefeatRequested += () => bossDefeatCount++;
            menu.TuningRequested += (spawn, alive, enemyMove, enemyAttack, enemyArmor, playerMove, playerFire) =>
            {
                tuningCount++;
                receivedTuning = new BlockadeCityBalanceSettings(
                    spawn, alive, enemyMove, enemyAttack, enemyArmor, playerMove, playerFire);
            };
            menu.SaveTuningRequested += () => saveCount++;
            menu.ProtocolRequested += protocolId =>
            {
                protocolCount++;
                receivedProtocol = protocolId;
            };
            menu.AuxiliaryRequested += auxiliaryId =>
            {
                auxiliaryCount++;
                receivedAuxiliary = auxiliaryId;
            };

            Press(menu, "敌军全灭（当前波）");
            Press(menu, "结束本轮并结算");
            Press(menu, "确认并到下一波");
            Press(menu, "结束本局（验收）");
            Press(menu, "Boss 推进到第二阶段");
            Press(menu, "击败 Boss（验收）");
            menu.InitializeTuning(BlockadeCityBalanceSettings.DesignBaseline, true);
            Press(menu, "密集");
            Press(menu, "保存为正式配置");
            ConfirmationDialog confirmation = menu.GetNode<ConfirmationDialog>("SaveTuningConfirmation");
            confirmation.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Press(menu, "授予协议：军械模块");
            Press(menu, "授予辅助：侧挂速射炮");

            Assert(clearCount == 1, "敌军全灭按钮必须只发出一次清场请求。");
            Assert(completeCount == 1, "结束本轮按钮必须只发出一次结算请求。");
            Assert(advanceCount == 1, "到下一波按钮必须只发出一次推进请求。");
            Assert(endRunCount == 1, "结束本局按钮必须只发出一次结束请求。");
            Assert(bossPhaseTwoCount == 1, "Boss 二阶段按钮必须只发出一次正式伤害推进请求。");
            Assert(bossDefeatCount == 1, "Boss 击败按钮必须只发出一次验收请求。");
            Assert(tuningCount == 1 && receivedTuning.ApproximatelyEquals(BlockadeCityBalanceSettings.DensePreset),
                "密集预设必须只发出一次确定的正式平衡快照。");
            Assert(menu.HasUnsavedTuning, "应用调参预设后必须显示存在未保存变更。");
            Assert(saveCount == 1, "二次确认后必须只发出一次正式配置保存请求。");
            Assert(protocolCount == 1 && receivedProtocol == "arsenal_damage",
                "军械验收按钮必须只通过正式协议 Id 发出一次请求。");
            Assert(auxiliaryCount == 1 && receivedAuxiliary == "aux_side_cannon",
                "辅助验收按钮必须只通过正式辅助 Id 发出一次请求。");
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
