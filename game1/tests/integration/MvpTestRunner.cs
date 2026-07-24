using System;
using System.IO;
using System.Linq;
using Godot;

namespace Game1.Tests.Integration;

/// <summary>
/// 迭代 08 的 Godot 运行时验收入口。它集中验证 MVP 关键闭环与压力基线，
/// 只在迭代完成后运行，不替代策划人员的可见试玩验收。
/// </summary>
public partial class MvpTestRunner : Node2D
{
    private int _failures;

    public override void _Ready() => RunAll();

    private async void RunAll()
    {
        Check("dash_and_steel_collision_contract", DashAndSteelCollisionContract);
        Check("projectile_reflection", ProjectileReflection);
        Check("projectile_team_targeting_contract", ProjectileTeamTargetingContract);
        Check("brick_destruction", BrickDestruction);
        Check("all_enemy_behaviors_target_player", AllEnemyBehaviorsTargetPlayer);
        Check("room_reward_and_run_failures", RoomRewardAndRunFailures);
        Check("boss_victory_phase", BossVictoryPhase);
        Check("save_round_trip", SaveRoundTrip);
        await CheckStressBudget();

        GD.Print($"[MvpTestRunner] failures={_failures}");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(string name, Action assertion)
    {
        try
        {
            assertion();
            GD.Print($"[PASS] {name}");
        }
        catch (Exception exception)
        {
            _failures++;
            GD.PrintErr($"[FAIL] {name}: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void DashAndSteelCollisionContract()
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/rooms/mvp_combat_room.tscn");
        Node2D room = scene.Instantiate<Node2D>();
        CharacterBody2D player = room.GetNode<CharacterBody2D>("PlayerTank");
        TileMapLayer structure = room.GetNode<TileMapLayer>("Structure");

        Assert((player.CollisionMask & 1u) != 0u, "玩家必须检测钢墙使用的物理层 1。");
        Assert(structure.TileSet is not null && structure.TileSet.GetPhysicsLayersCount() > 0,
            "钢墙图层必须包含物理碰撞层。");
        Assert(structure.GetNodeOrNull<TileLayerPainter>("GroundPainter") is null, "结构图层不能误用地面绘制节点。");
        room.Free();

        DashState dash = new(0.14f, 0.8f);
        Assert(dash.TryStart(Vector2.Right), "冲刺应能从空闲态启动。");
        Assert(dash.Advance(0.14f) == DashAdvanceResult.Ended, "冲刺必须在固定窗口结束，碰撞交由 CharacterBody2D 阻挡。");
    }

    private static void ProjectileReflection()
    {
        Vector2 reflected = ProjectileMath.Reflect(Vector2.Right, Vector2.Left);
        Assert(reflected.IsEqualApprox(Vector2.Left), "正面命中钢墙后炮弹方向必须反转。");
    }

    private static void ProjectileTeamTargetingContract()
    {
        Assert(ProjectileTargeting.CollisionMaskFor(Team.Player) == 11u,
            "玩家炮弹只能查询世界层与敌军层，不能查询玩家层。");
        Assert(ProjectileTargeting.CollisionMaskFor(Team.Enemy) == 7u,
            "敌方炮弹只能查询世界层与玩家层，不能查询敌军层。");
        Assert(ProjectileTargeting.CanDamage(Team.Player, Team.Enemy),
            "玩家炮弹必须能伤害敌军。");
        Assert(!ProjectileTargeting.CanDamage(Team.Player, Team.Player),
            "玩家炮弹绝不能伤害自身阵营。");
        Assert(ProjectileTargeting.CanDamage(Team.Enemy, Team.Player),
            "敌方炮弹必须能伤害玩家。");
        Assert(!ProjectileTargeting.CanDamage(Team.Enemy, Team.Enemy),
            "敌方炮弹绝不能误伤敌军。");
    }

    private static void BrickDestruction()
    {
        TileMapLayer layer = new() { TileSet = GD.Load<TileSet>("res://resources/tiles/industrial_tileset.tres") };
        TileTerrainAdapter terrain = new();
        Vector2I cell = new(2, 3);
        terrain.Initialize(layer, new[] { cell }, 20);

        Assert(!terrain.DamageBrick(cell, 10), "第一发不应摧毁满耐久砖墙。");
        Assert(terrain.DamageBrick(cell, 10), "累计伤害达到耐久后必须摧毁砖墙。");
        Assert(!terrain.BlockedNavigationCells.Contains(cell), "砖墙摧毁后必须同步开放导航格。");
        terrain.Free();
        layer.Free();
    }

    private static void AllEnemyBehaviorsTargetPlayer()
    {
        foreach (BehaviorId behavior in Enum.GetValues<BehaviorId>())
        {
            TargetId selected = TargetPolicy.SelectTarget(behavior, new TargetSnapshot(PlayerAvailable: true));
            Assert(selected == TargetId.Player, $"{behavior} 必须只选择玩家坦克。");
        }
    }

    private static void RoomRewardAndRunFailures()
    {
        ContentCatalog catalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
        catalog.Validate();
        RunState state = RunState.CreateNew(8080);
        BuildController build = new(state, catalog);
        RunController run = new(state, build, playableArenaCount: 5);
        Assert(run.Phase == RunPhase.Arena, "移动核心重构后单局必须从竞技场阶段开始。");

        RunState rebootState = RunState.CreateNew(8081, reboots: 1);
        RunController rebootRun = new(rebootState, new BuildController(rebootState, catalog), playableArenaCount: 5);
        Assert(rebootRun.OnTankDefeated() && rebootState.RebootsRemaining == 0,
            "首次报废必须消耗一次重启并继续战斗。");
        Assert(!rebootRun.OnTankDefeated() && rebootRun.Phase == RunPhase.Failed,
            "重启耗尽后的再次报废必须判负。");

        Assert(rebootState.RebootsRemaining == 0, "重启次数不得降为负数。");
    }

    private static void BossVictoryPhase()
    {
        BossPhaseController controller = new();
        Assert(controller.ReportHealth(50, 100) == BossPhase.PhaseTwo, "Boss 半血必须进入二阶段。");
        Assert(controller.ReportHealth(0, 100) == BossPhase.Defeated, "Boss 耗尽后必须进入胜利结算前置状态。");
    }

    private static void SaveRoundTrip()
    {
        string directory = ProjectSettings.GlobalizePath("user://mvp_integration");
        string path = Path.Combine(directory, "save.json");
        if (Directory.Exists(directory)) Directory.Delete(directory, true);

        SaveService service = new(path, emitWarnings: false);
        SaveData source = SaveData.CreateDefault();
        source.UnlockedIds.Add("boss_roadblock_commander");
        source.LastRun = new LastRunSummary
        {
            Seed = 8080,
            CoreId = "starter_core",
            ArenaIndex = 1,
            WaveIndex = 5,
            Level = 4,
            ElapsedSeconds = 125d,
            Result = "victory"
        };
        service.SaveAtomic(source);
        SaveData loaded = service.LoadOrDefault();

        Assert(loaded.SchemaVersion == SaveData.CurrentSchemaVersion, "存档版本必须保持一致。");
        Assert(loaded.UnlockedIds.SequenceEqual(source.UnlockedIds), "解锁记录必须可回读。");
        Assert(loaded.LastRun.Seed == 8080 && loaded.LastRun.Result == "victory", "最近一局摘要必须可回读。");

        File.WriteAllText(path, "{ broken-json");
        SaveData recovered = service.LoadOrDefault();
        Assert(recovered.SchemaVersion == SaveData.CurrentSchemaVersion, "损坏存档必须回退默认数据。");
        Assert(File.Exists(path + ".broken"), "损坏存档必须保留为 .broken 便于排查。");
        Directory.Delete(directory, true);
    }

    private async System.Threading.Tasks.Task CheckStressBudget()
    {
        Node2D stressRoot = new() { Name = "StressRoot" };
        AddChild(stressRoot);
        PackedScene enemyScene = GD.Load<PackedScene>("res://scenes/actors/enemy_tank.tscn");
        PackedScene projectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");

        for (int index = 0; index < 30; index++)
        {
            EnemyTank enemy = enemyScene.Instantiate<EnemyTank>();
            enemy.Position = new Vector2(20 + index * 12, 40);
            stressRoot.AddChild(enemy);
        }
        for (int index = 0; index < 160; index++)
        {
            Projectile projectile = projectileScene.Instantiate<Projectile>();
            projectile.Position = new Vector2(10 + index % 40 * 8, 90 + index / 40 * 8);
            projectile.Initialize(new ProjectileSpec(1, 80f, 3f, 0), Team.Enemy, Vector2.Right);
            stressRoot.AddChild(projectile);
        }
        for (int index = 0; index < 40; index++) stressRoot.AddChild(new Area2D { Name = $"Hazard{index}" });

        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check("stress_30_enemies_160_projectiles_40_hazards", () =>
        {
            Assert(stressRoot.GetChildCount() == 230, "压力场景节点数量不完整或运行两帧后异常丢失。");
            Assert(GetTree().GetNodesInGroup("enemy_projectiles").Count == 160, "敌方炮弹组数量必须保持 160。");
        });
        stressRoot.QueueFree();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
