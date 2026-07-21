using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证奖励控制器是维护修复和协议应用的唯一入口。</summary>
public partial class RewardControllerTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            RunState state = RunState.CreateNew(seed: 54);
            state.SynchronizeArmor(20, 100);
            ContentCatalog catalog = CreateCatalog();
            BuildController build = new(state, catalog);
            RewardController rewards = new(state, build, catalog, new RewardGenerator(), new MaintenanceRewardGenerator());

            RewardOffer maintenance = rewards.Generate(RewardKind.Maintenance);
            Assert(Contains(maintenance, "maintenance_repair_25"), "低于 30% 装甲时维护候选必须包含修复。\n");
            rewards.Choose("maintenance_repair_25");
            Assert(state.PlayerArmor == 45, "选择修复后必须恢复 25% 最大装甲。\n");

            RewardOffer protocol = rewards.Generate(RewardKind.NormalProtocol);
            string selected = protocol.Choices[0].Id;
            rewards.Choose(selected);
            Assert(state.GetProtocolRank(selected) == ProtocolRank.MkI, "协议奖励必须经控制器应用为 Mk.I。\n");
            GD.Print("[PASS] reward_controller_flow");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] reward_controller_flow: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static bool Contains(RewardOffer offer, string id)
    {
        foreach (RewardChoice choice in offer.Choices)
            if (choice.Id == id) return true;
        return false;
    }

    private static ContentCatalog CreateCatalog()
    {
        ContentCatalog catalog = new() { Version = "reward-controller-v1" };
        for (int index = 0; index < 3; index++)
        {
            ProtocolEffectDefinition effect = new()
            {
                EffectId = $"reward_controller_effect_{index}",
                Stat = StatId.Damage,
                FlatAdd = index + 1
            };
            catalog.Protocols.Add(new ProtocolDefinition
            {
                Id = $"reward_protocol_{index}",
                DisplayName = $"奖励协议 {index}",
                Description = "奖励控制器测试协议。",
                Department = ProtocolDepartment.Arsenal,
                Rarity = 1,
                BaseWeight = 1f,
                StackLimit = 1,
                Effects = new Godot.Collections.Array<ProtocolEffectDefinition> { effect }
            });
        }
        return catalog;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
