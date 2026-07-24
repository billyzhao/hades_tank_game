using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>验证封锁城区从五波、Boss 到正式胜利结果的单竞技场交付闭环。</summary>
public partial class SingleArenaFlowTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            RunState state = RunState.CreateNew(seed: 20260724);
            ContentCatalog catalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
            BuildController build = new(state, catalog);
            build.ApplyCore(CoreCatalog.CreateDefault().Get(CoreId.BreakthroughCannon));
            RunController run = new(state, build, playableArenaCount: 1);
            ArenaController arena = new(state);

            arena.BeginArena(WaveSchedule.CreateApproved(), arenaIndex: 0);
            arena.OnIntroFinished();
            for (int wave = 0; wave < 5; wave++)
            {
                arena.OnWaveSpawnWindowEnded();
                arena.OnAllEnemiesCleared();
                arena.ConfirmReward($"bc01_wave_{wave + 1}");
            }

            arena.OnBossStarted();
            arena.OnBossDefeated();
            run.OnArenaCompleted();

            Assert(run.Phase == RunPhase.Completed, "Boss 完成后必须结束封锁城区本局。");
            Assert(state.ArenaIndex == 0, "胜利结算不得推进未交付竞技场。");

            RunResultScreen resultScreen = new();
            AddChild(resultScreen);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            RunResultSnapshot snapshot = new(
                state.Seed,
                state.SelectedProtocolIds,
                state.SelectedCore?.ToString() ?? string.Empty,
                state.ArenaIndex,
                state.WaveIndex,
                state.Level,
                TimeSpan.FromMinutes(12));
            resultScreen.ShowResult(snapshot, victory: true);

            string visibleText = string.Join(
                "\n",
                resultScreen.FindChildren("*", "Label", true, false)
                    .OfType<Label>()
                    .Select(label => label.Text));
            Assert(resultScreen.Visible, "胜利结果界面必须可见。");
            Assert(visibleText.Contains("封锁城区突破", StringComparison.Ordinal),
                "胜利标题必须明确封锁城区已经突破。");
            Assert(visibleText.Contains("BreakthroughCannon", StringComparison.Ordinal),
                "胜利摘要必须记录本局核心。");
            Assert(visibleText.Contains("等级", StringComparison.Ordinal) &&
                   visibleText.Contains("12:00", StringComparison.Ordinal),
                "胜利摘要必须包含等级与耗时。");

            StartScreen startScreen = new();
            AddChild(startScreen);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Assert(startScreen.Visible, "标题界面初始必须可见。");
            Assert(startScreen.GetNode<Button>("Panel/StartButton").Text == "进入封锁城区",
                "开始按钮必须说明当前交付内容。");
            Assert(startScreen.GetNode<Button>("Panel/QuitButton").Text == "退出游戏",
                "标题界面必须提供退出入口。");

            PauseCoordinator pauseCoordinator = new(GetTree());
            pauseCoordinator.Acquire(PauseReason.RunResult);
            Assert(GetTree().Paused, "胜负结果显示期间必须冻结战斗场景树。");
            Assert(resultScreen.ProcessMode == ProcessModeEnum.Always,
                "结果界面必须在暂停期间继续接收重试和返回输入。");
            pauseCoordinator.Release(PauseReason.RunResult);

            await VerifyRealAppRootWiring();

            GD.Print("[PASS] bc01_single_arena_flow");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] bc01_single_arena_flow: {exception.Message}");
            GD.PrintErr(exception.StackTrace ?? "<no stack trace>");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private async Task VerifyRealAppRootWiring()
    {
        string savePath = Path.Combine(
            OS.GetUserDataDir(),
            $"bc01-app-root-{Guid.NewGuid():N}.json");
        AppRoot app = null;
        try
        {
            app = GD.Load<PackedScene>("res://scenes/app/main.tscn").Instantiate<AppRoot>();
            app.ConfigureSaveServiceForTesting(new SaveService(savePath, emitWarnings: false));
            AddChild(app);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            StartScreen start = app.FindChildren("*", "Control", true, false)
                .OfType<StartScreen>()
                .Single();
            CoreSelectionPanel coreSelection = app.FindChildren("*", "Control", true, false)
                .OfType<CoreSelectionPanel>()
                .Single();
            Assert(start.Visible && GetTree().Paused,
                "真实 AppRoot 启动后必须显示标题并持有开始界面暂停。");

            start.EmitSignal(StartScreen.SignalName.StartRequested);
            Assert(coreSelection.Visible && GetTree().Paused,
                "真实 AppRoot 必须从标题无缝交接到核心选择暂停。");
            Button firstCore = coreSelection.FindChildren("*", "Button", true, false)
                .OfType<Button>()
                .First();
            firstCore.EmitSignal(Button.SignalName.Pressed);
            Assert(!GetTree().Paused && app.CurrentRun.SelectedCore == CoreId.BreakthroughCannon,
                "选择核心后必须解除整备暂停并写入真实 RunState。");

            ArenaController arena = GetPrivateField<ArenaController>(app, "_arenaController");
            RewardController rewards = GetPrivateField<RewardController>(app, "_rewardController");
            Assert(arena.State == ArenaState.Intro, "核心选择完成后竞技场必须仍处于正式 Intro。");
            arena.OnIntroFinished();
            for (int wave = 1; wave <= 5; wave++)
            {
                WaveDirector director = GetPrivateField<WaveDirector>(app, "_waveDirector");
                director.SetProcess(false);
                director.SetPhysicsProcess(false);
                arena.OnWaveSpawnWindowEnded();
                arena.OnAllEnemiesCleared();
                string choiceId = rewards.CurrentOffer?.Choices[0].Id
                    ?? throw new InvalidOperationException($"第 {wave} 波未通过 AppRoot 生成奖励候选。");
                rewards.Choose(choiceId);
                arena.ConfirmReward(choiceId);
            }

            Assert(arena.State == ArenaState.BossCombat,
                "真实 AppRoot 完成五波后必须通过 BossRequested 接线进入 BossCombat。");
            RoadblockCommander boss = app.FindChild("RoadblockCommander", true, false) as RoadblockCommander
                ?? throw new InvalidOperationException("BossRequested 未在真实 AppRoot 中创建路障指挥车。");
            boss.ApplyDamage(new DamageContext(boss.MaximumHealth));

            RunController run = GetPrivateField<RunController>(app, "_runController");
            RunResultScreen result = app.FindChildren("*", "Control", true, false)
                .OfType<RunResultScreen>()
                .Single();
            Assert(run.Phase == RunPhase.Completed && app.CurrentRun.ArenaIndex == 0,
                "真实 Boss Defeated 接线必须完成本局且不得推进第二竞技场。");
            Assert(result.Visible && GetTree().Paused,
                "真实胜利接线必须显示结果页并持有结果暂停。");

            SaveData saved = new SaveService(savePath, emitWarnings: false).LoadOrDefault();
            Assert(saved.LastRun.Result == "victory",
                "真实胜利接线必须保存 victory 结果。");
            Assert(saved.LastRun.CoreId == CoreId.BreakthroughCannon.ToString(),
                "真实胜利存档必须记录本局核心。");
        }
        finally
        {
            GetTree().Paused = false;
            if (app is not null && IsInstanceValid(app))
            {
                app.QueueFree();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            DeleteIfExists(savePath);
            DeleteIfExists(savePath + ".tmp");
            DeleteIfExists(savePath + ".bak");
            DeleteIfExists(savePath + ".broken");
        }
    }

    private static T GetPrivateField<T>(object target, string fieldName)
        where T : class
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().Name, fieldName);
        return field.GetValue(target) as T
            ?? throw new InvalidOperationException($"{fieldName} 尚未初始化为 {typeof(T).Name}。");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
