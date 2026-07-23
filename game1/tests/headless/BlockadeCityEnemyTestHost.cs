using System;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>Alpha 02G 首区敌军内容合同，防止后续回退为旧三行为占位。</summary>
public partial class BlockadeCityEnemyTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            string[] names = Enum.GetNames<BehaviorId>();
            Assert(names.Contains("Scout"), "封锁城区必须包含侦察无人机职责。");
            Assert(names.Contains("Patrol"), "封锁城区必须包含巡逻坦克职责。");
            Assert(names.Contains("Assault"), "封锁城区必须包含突击车职责。");
            Assert(names.Contains("Mortar"), "封锁城区必须包含迫击炮车职责。");
            Assert(names.Length == 4, "首区普通敌军必须恰好四类，不能继续保留旧重炮车占位。 ");
            GD.Print("[PASS] blockade_city_enemy_contract");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] blockade_city_enemy_contract: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
