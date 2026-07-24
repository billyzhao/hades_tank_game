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

            ContentCatalog catalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
            catalog.Validate();
            Assert(catalog.Enemies.Count == 4, "内容目录必须恰好登记四类封锁城区敌军。");
            Assert(catalog.Enemies.Select(enemy => enemy.Behavior).Distinct().Count() == 4,
                "四类敌军必须拥有唯一职责标识。");
            EnemyDefinition scout = catalog.GetEnemy(BehaviorId.Scout);
            EnemyDefinition patrol = catalog.GetEnemy(BehaviorId.Patrol);
            EnemyDefinition assault = catalog.GetEnemy(BehaviorId.Assault);
            EnemyDefinition mortar = catalog.GetEnemy(BehaviorId.Mortar);
            Assert(scout.MoveSpeed > assault.MoveSpeed && assault.MoveSpeed > patrol.MoveSpeed &&
                   patrol.MoveSpeed > mortar.MoveSpeed,
                "移动速度必须形成侦察 > 突击 > 巡逻 > 迫击炮的可读职责差异。");
            Assert(mortar.AttackRange > mortar.RetreatRange && mortar.TelegraphSeconds > patrol.TelegraphSeconds,
                "迫击炮车必须拥有远射程、近身撤退距离和最长预警。");
            Assert(scout.Armor < patrol.Armor && scout.Damage < patrol.Damage,
                "侦察无人机必须以低装甲、低伤害换取侧绕速度。");

            EnemyMovementIntent scoutIntent = EnemyMovementPolicy.Calculate(
                scout.MovementMode, Vector2.Zero, new Vector2(100f, 0f), scout.AttackRange, scout.RetreatRange, 0f);
            Assert(!scoutIntent.Destination.IsEqualApprox(new Vector2(100f, 0f)),
                "侦察无人机必须生成侧绕点，而不是与巡逻坦克相同的直线追击点。");
            EnemyMovementIntent mortarClose = EnemyMovementPolicy.Calculate(
                mortar.MovementMode, new Vector2(80f, 0f), new Vector2(100f, 0f),
                mortar.AttackRange, mortar.RetreatRange, 0f);
            Assert(mortarClose.ShouldMove && mortarClose.Destination.DistanceTo(new Vector2(100f, 0f)) > 20f,
                "迫击炮车被贴近时必须撤退并拉大距离。");
            EnemyMovementIntent mortarReady = EnemyMovementPolicy.Calculate(
                mortar.MovementMode, Vector2.Zero, new Vector2(130f, 0f),
                mortar.AttackRange, mortar.RetreatRange, 0f);
            Assert(!mortarReady.ShouldMove, "迫击炮车进入有效站位后必须停火准备，而不是继续贴脸。");

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
