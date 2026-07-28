using System;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证正式 Profile 往返和玩家/敌军运行时倍率，不写入正式资源。</summary>
public partial class DebugTuningTestHost : Node
{
    public override async void _Ready()
    {
        const string temporaryProfile = "user://bc05a_balance_profile_test.tres";
        try
        {
            BlockadeCityBalanceProfile official = BlockadeCityBalanceProfileStore.LoadValidated();
            official.Validate();
            if (BlockadeCityBalanceProfileStore.CanSaveFromCurrentEnvironment)
            {
                BalanceProfileSaveResult officialSave =
                    BlockadeCityBalanceProfileStore.SaveFromEditorDebug(official.ToSettings());
                Assert(officialSave.Success,
                    $"编辑器 Debug 环境必须能把同值快照安全写回正式 Profile：{officialSave.Message}");
            }

            BalanceProfileSaveResult saveResult = BlockadeCityBalanceProfileStore.SaveToPathForTesting(
                BlockadeCityBalanceSettings.DensePreset,
                temporaryProfile);
            Assert(saveResult.Success, $"临时 Profile 保存与重载必须成功：{saveResult.Message}");
            BlockadeCityBalanceProfile roundTrip = ResourceLoader.Load<BlockadeCityBalanceProfile>(
                temporaryProfile,
                cacheMode: ResourceLoader.CacheMode.Replace);
            Assert(roundTrip.ToSettings().ApproximatelyEquals(BlockadeCityBalanceSettings.DensePreset),
                "调参快照保存后必须能精确重新加载。");

            Node2D arena = new();
            AddChild(arena);
            PlayerTank player = GD.Load<PackedScene>("res://scenes/actors/player_tank.tscn").Instantiate<PlayerTank>();
            arena.AddChild(player);
            player.ApplyBalance(1.3f, 2f);
            Assert(Mathf.IsEqualApprox(player.AppliedMoveSpeedMultiplier, 1.3f),
                "玩家移动倍率必须立即应用。");
            Assert(Mathf.IsEqualApprox(player.GetNode<WeaponController>("WeaponController").FireRateMultiplier, 2f),
                "玩家射击频率倍率必须立即应用到正式武器控制器。");

            ContentCatalog catalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
            EnemyDefinition definition = catalog.GetEnemy(BehaviorId.Patrol);
            EnemyTank enemy = GD.Load<PackedScene>("res://scenes/actors/enemy_tank.tscn").Instantiate<EnemyTank>();
            enemy.Configure(definition);
            enemy.ConfigureBalance(1.5f, 1.75f, 2f);
            arena.AddChild(enemy);
            Assert(Mathf.IsEqualApprox(enemy.MoveSpeed, definition.MoveSpeed * 1.5f),
                "新出生敌军必须应用移动倍率而不改写 EnemyDefinition。");
            Assert(enemy.Armor == definition.Armor * 2,
                "新出生敌军必须应用装甲倍率而不改写 EnemyDefinition。");
            Assert(Mathf.IsEqualApprox(enemy.AppliedAttackRateMultiplier, 1.75f),
                "新出生敌军必须保存攻击频率倍率快照。");

            arena.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(temporaryProfile));
            GD.Print("[PASS] bc05a_debug_tuning");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] bc05a_debug_tuning: {exception.GetType().Name}: {exception.Message}");
            GD.PrintErr(exception.StackTrace ?? "<no stack trace>");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
