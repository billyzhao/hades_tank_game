using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>Alpha 02C 的 Godot 运行时契约：验证资源、限时刷新、残敌清场和精英槽位。</summary>
public partial class ArenaWaveTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            ArenaDefinition definition = GD.Load<ArenaDefinition>("res://resources/arenas/blockade_city_arena.tres");
            definition.Validate();
            Assert(definition.Waves.Select(wave => wave.SpawnDurationSeconds).SequenceEqual(new[] { 45d, 50d, 55d, 60d, 70d }),
                "正式竞技场必须使用 45/50/55/60/70 秒配置。");

            await AssertDirectorSkipsUnreachableEntrance();
            await AssertDirectorSkipsTerrainBlockedEntrance();
            await AssertAcceptanceClearCancelsPendingSpawnAfterStop();
            await AssertSpawnWindowDoesNotDeleteEliteOrRemainingEnemies();
            AssertBossCompletionTransition();
            GD.Print("[PASS] alpha_02c_arena_wave");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] alpha_02c_arena_wave: {exception.GetType().Name}: {exception.Message}");
            GD.PrintErr(exception.StackTrace ?? "<no stack trace>");
            GetTree().Quit(1);
        }
    }

    private async System.Threading.Tasks.Task AssertDirectorSkipsTerrainBlockedEntrance()
    {
        Node2D arena = new() { Name = "TerrainBlockedSpawnArena" };
        AddChild(arena);
        Node2D player = new() { Name = "Player", Position = new Vector2(240f, 135f) };
        player.AddToGroup("player");
        arena.AddChild(player);
        StaticBody2D wall = new() { Name = "SpawnWall", Position = new Vector2(72f, 72f), CollisionLayer = 1 };
        wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20f, 20f) } });
        arena.AddChild(wall);

        WaveDefinition wave = new()
        {
            WaveNumber = 1,
            SpawnDurationSeconds = 1d,
            SpawnIntervalSeconds = 0.01f,
            MaximumAliveEnemies = 1,
            MinimumPlayerDistance = 72f,
            RewardKind = RewardKind.NormalProtocol,
            IncludesElite = false
        };
        wave.Behaviors.Add(BehaviorId.Patrol);
        WaveDirector director = new();
        arena.AddChild(director);
        director.Configure(wave,
            [new SpawnEntrance("wall", new Vector2(72f, 72f), Vector2.Right, 0f)],
            20260721, 0, 0, new DirectPathProvider());
        director.StartWave();

        await ToSignal(GetTree().CreateTimer(0.08d), SceneTreeTimer.SignalName.Timeout);
        Assert(director.AliveEnemyCount == 0,
            "入口与地形碰撞重叠时不得生成会卡死的敌军。 ");
        arena.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async System.Threading.Tasks.Task AssertDirectorSkipsUnreachableEntrance()
    {
        Node2D arena = new() { Name = "BlockedSpawnArena" };
        AddChild(arena);
        Node2D player = new() { Name = "Player", Position = new Vector2(240f, 135f) };
        player.AddToGroup("player");
        arena.AddChild(player);

        WaveDefinition wave = new()
        {
            WaveNumber = 1,
            SpawnDurationSeconds = 1d,
            SpawnIntervalSeconds = 0.01f,
            MaximumAliveEnemies = 1,
            MinimumPlayerDistance = 72f,
            RewardKind = RewardKind.NormalProtocol,
            IncludesElite = false
        };
        wave.Behaviors.Add(BehaviorId.Patrol);
        WaveDirector director = new();
        arena.AddChild(director);
        director.Configure(wave,
            [new SpawnEntrance("blocked", new Vector2(72f, 72f), Vector2.Right, 0f)],
            20260721, 0, 0, new BlockedPathProvider());
        director.StartWave();

        await ToSignal(GetTree().CreateTimer(0.08d), SceneTreeTimer.SignalName.Timeout);
        Assert(director.AliveEnemyCount == 0,
            "入口到玩家不存在导航路径时不得生成会卡死的敌军。 ");
        arena.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async System.Threading.Tasks.Task AssertSpawnWindowDoesNotDeleteEliteOrRemainingEnemies()
    {
        Node2D arena = new() { Name = "Arena" };
        AddChild(arena);
        Node2D player = new() { Name = "Player", Position = new Vector2(240f, 135f) };
        player.AddToGroup("player");
        arena.AddChild(player);

        WaveDefinition shortEliteWave = new()
        {
            WaveNumber = 5,
            SpawnDurationSeconds = 0.08d,
            SpawnIntervalSeconds = 0.01f,
            MaximumAliveEnemies = 2,
            MinimumPlayerDistance = 72f,
            RewardKind = RewardKind.RareProtocol,
            IncludesElite = true
        };
        shortEliteWave.Behaviors.Add(BehaviorId.Patrol);
        shortEliteWave.Behaviors.Add(BehaviorId.Mortar);

        WaveDirector director = new();
        arena.AddChild(director);
        IReadOnlyList<SpawnEntrance> entrances =
        [
            new("north", new Vector2(240f, 36f), Vector2.Down, 0.35f),
            new("east", new Vector2(444f, 135f), Vector2.Left, 0.35f),
            new("south", new Vector2(240f, 246f), Vector2.Up, 0.35f),
            new("west", new Vector2(36f, 135f), Vector2.Right, 0.35f)
        ];
        int spawnWindowEnded = 0;
        int allEnemiesCleared = 0;
        director.SpawnWindowEnded += () => spawnWindowEnded++;
        director.AllEnemiesCleared += () => allEnemiesCleared++;
        director.Configure(shortEliteWave, entrances, 20260721, 0, 4, new DirectPathProvider());
        director.StartWave();

        await ToSignal(GetTree().CreateTimer(0.45d), SceneTreeTimer.SignalName.Timeout);

        Assert(spawnWindowEnded == 1, "刷新时长结束后必须只发出一次 SpawnWindowEnded。");
        Assert(!director.IsSpawning, "刷新窗口结束后必须停止生成。");
        Assert(director.AliveEnemyCount > 0, "刷新窗口结束不得自动删除场上残敌。");
        Assert(director.EliteAlive, "第 5 波精英槽位在被击毁前必须保持存活标记。");
        Assert(allEnemiesCleared == 0, "仍有残敌或精英时不得发出 AllEnemiesCleared。");
        Assert(GetTree().GetNodesInGroup("elite_placeholder").Count == 1, "第 5 波必须恰有一个精英槽位。");

        director.ClearAliveEnemiesForAcceptance();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        string remainingEnemies = string.Join(", ",
            arena.GetChildren().OfType<EnemyTank>()
                .Select(enemy => $"{enemy.Name}(armor={enemy.Armor},queued={enemy.IsQueuedForDeletion()})"));
        Assert(director.AliveEnemyCount == 0 && !director.EliteAlive,
            $"击毁残敌后存活数和精英标记必须归零；alive={director.AliveEnemyCount}, elite={director.EliteAlive}, nodes=[{remainingEnemies}]。");
        Assert(allEnemiesCleared == 1, "刷新结束且残敌清空时只能发出一次 AllEnemiesCleared。");
        Assert(!arena.GetChildren().OfType<EnemyTank>().Any(), "验收清场只应销毁本导演仍追踪的残敌。");

        arena.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async System.Threading.Tasks.Task AssertAcceptanceClearCancelsPendingSpawnAfterStop()
    {
        Node2D arena = new() { Name = "AcceptancePendingSpawnArena" };
        AddChild(arena);
        Node2D player = new() { Name = "Player", Position = new Vector2(240f, 135f) };
        player.AddToGroup("player");
        arena.AddChild(player);

        WaveDefinition wave = new()
        {
            WaveNumber = 1,
            SpawnDurationSeconds = 1d,
            SpawnIntervalSeconds = 1f,
            MaximumAliveEnemies = 1,
            MinimumPlayerDistance = 72f,
            RewardKind = RewardKind.NormalProtocol,
            IncludesElite = false
        };
        wave.Behaviors.Add(BehaviorId.Patrol);

        WaveDirector director = new();
        arena.AddChild(director);
        int allEnemiesCleared = 0;
        director.AllEnemiesCleared += () => allEnemiesCleared++;
        director.Configure(wave,
            [new SpawnEntrance("north", new Vector2(240f, 36f), Vector2.Down, 0.35f)],
            20260723, 0, 0, new DirectPathProvider());
        director.StartWave();

        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Assert(arena.GetChildren().OfType<SpawnWarning>().Any(),
            "回归前置条件失败：必须先进入敌军出生预警，再执行停止与清场。");
        director.StopSpawning();
        director.ClearAliveEnemiesForAcceptance();
        await ToSignal(GetTree().CreateTimer(0.45d), SceneTreeTimer.SignalName.Timeout);

        Assert(director.AliveEnemyCount == 0,
            "验收命令停止刷新并清场后，预警中的待出生敌军不得再次落地。");
        Assert(!arena.GetChildren().OfType<EnemyTank>().Any(),
            "验收清场必须同时取消尚未实例化的出生任务。");
        Assert(allEnemiesCleared == 1,
            "验收清空存活敌军与待出生队列后必须只发出一次 AllEnemiesCleared。");

        arena.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void AssertBossCompletionTransition()
    {
        RunState state = RunState.CreateNew(20260723);
        ArenaController controller = new(state);
        controller.BeginArena(WaveSchedule.CreateApproved(), 0);
        controller.OnIntroFinished();
        for (int wave = 0; wave < 5; wave++)
        {
            controller.OnWaveSpawnWindowEnded();
            controller.OnAllEnemiesCleared();
            controller.ConfirmReward($"test_reward_{wave}");
        }
        Assert(controller.State == ArenaState.BossIntro, "第 5 波奖励后必须进入 BossIntro。 ");
        controller.OnBossStarted();
        controller.OnBossDefeated();
        Assert(controller.State == ArenaState.Completed, "Boss 击败后必须成为竞技场完成事实。 ");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }


    private sealed class DirectPathProvider : IEnemyPathProvider
    {
        public IReadOnlyList<Vector2> GetWorldPath(Vector2 fromWorld, Vector2 toWorld) => [fromWorld, toWorld];
    }

    private sealed class BlockedPathProvider : IEnemyPathProvider
    {
        public IReadOnlyList<Vector2> GetWorldPath(Vector2 fromWorld, Vector2 toWorld) => Array.Empty<Vector2>();
    }
}
