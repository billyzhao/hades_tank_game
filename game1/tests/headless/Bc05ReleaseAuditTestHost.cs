using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>
/// BC-05 发布候选玩法审计：用正式控制器走完三核心、多种子、五波、奖励与 Boss，
/// 同时锁定维修阈值、升级队列和初始核心输出边界。它不模拟玩家命中率，也不替代实机试玩。
/// </summary>
public partial class Bc05ReleaseAuditTestHost : Node
{
    private const int SeedSamplesPerCore = 24;

    public override void _Ready()
    {
        try
        {
            ContentCatalog catalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
            ArenaDefinition arena = GD.Load<ArenaDefinition>("res://resources/arenas/blockade_city_arena.tres");
            catalog.Validate();
            arena.Validate();

            AuditProjectIdentity();
            AuditMaintenanceBoundary();
            AuditCoreOpeningDamage(catalog);
            AuditCompleteRuns(catalog, arena);
            AuditFreshRestart(catalog, arena);

            GD.Print("[PASS] bc05_release_audit");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] bc05_release_audit: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void AuditProjectIdentity()
    {
        Assert(ProjectSettings.GetSetting("application/config/name").AsString() == "废土中继",
            "发布候选必须使用正式产品名。");
        Assert(ProjectSettings.GetSetting("application/config/version").AsString() == "0.1.0-rc1",
            "发布候选版本必须固定为 0.1.0-rc1。");
        Assert(ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32() == 480 &&
               ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32() == 270,
            "逻辑画布必须保持 480×270。");
    }

    private static void AuditMaintenanceBoundary()
    {
        MaintenanceRewardGenerator generator = new();
        RewardOffer below = generator.Generate(7001, 29, 100);
        RewardOffer exact = generator.Generate(7001, 30, 100);
        RewardOffer above = generator.Generate(7001, 31, 100);

        Assert(below.Choices.Any(choice => choice.Id == "maintenance_repair_25"),
            "装甲 29% 时维护奖励必须包含应急装甲修复。");
        Assert(exact.Choices.All(choice => choice.Id != "maintenance_repair_25"),
            "装甲恰好 30% 时不能触发“低于 30%”保障。");
        Assert(above.Choices.All(choice => choice.Id != "maintenance_repair_25"),
            "装甲高于 30% 时不能强制插入修复。");
    }

    private static void AuditCoreOpeningDamage(ContentCatalog catalog)
    {
        List<double> openingDps = new();
        foreach (CoreDefinition core in CoreCatalog.CreateDefault().Definitions)
        {
            RunState state = RunState.CreateNew(9000 + (int)core.Id);
            BuildController build = new(state, catalog);
            build.ApplyCore(core);
            float damage = build.EvaluateStat(StatId.Damage, 10f);
            float cooldown = build.EvaluateStat(StatId.FireCooldown, .22f);
            Assert(damage > 0f && cooldown >= .08f,
                $"{core.DisplayName} 的开局伤害或射击冷却越过安全边界。");
            openingDps.Add(damage / cooldown);
        }

        double spread = openingDps.Max() / openingDps.Min();
        Assert(spread <= 1.25d,
            $"三核心开局持续输出差距过大：最高/最低={spread:F2}。核心应主要改变节奏而非决定胜负。");
        foreach (double dps in openingDps)
        {
            double idealExposureSeconds = 300d / dps;
            Assert(idealExposureSeconds is >= 4d and <= 9d,
                $"Boss 300 装甲相对开局火力的纯暴露时长异常：{idealExposureSeconds:F2}s。");
        }
    }

    private static void AuditCompleteRuns(ContentCatalog catalog, ArenaDefinition arena)
    {
        BuildRouteCatalog routeCatalog = BuildRouteCatalog.CreateDefault();
        foreach (CoreDefinition core in CoreCatalog.CreateDefault().Definitions)
        {
            HashSet<string> observedTags = new(StringComparer.Ordinal);
            HashSet<string> observedProtocols = new(StringComparer.Ordinal);
            IReadOnlyList<BuildRouteDefinition> routes = routeCatalog.GetRoutes(core.Id);

            for (int sample = 0; sample < SeedSamplesPerCore; sample++)
            {
                int seed = 17000 + (int)core.Id * 1000 + sample * 37;
                string preferredTag = routes[sample % routes.Count].Tag;
                RunState state = RunState.CreateNew(seed);
                BuildController build = new(state, catalog);
                build.ApplyCore(core);
                RunLevelUpQueue(state, build);

                RewardController rewards = new(
                    state,
                    build,
                    catalog,
                    new RewardGenerator(),
                    new MaintenanceRewardGenerator());
                ArenaController controller = new(state);
                controller.BeginArena(arena);
                controller.OnIntroFinished();

                for (int wave = 1; wave <= 5; wave++)
                {
                    Assert(controller.State == ArenaState.WaveCombat && controller.CurrentWave == wave,
                        $"{core.DisplayName}/seed {seed} 未进入第 {wave} 波。");
                    controller.OnWaveSpawnWindowEnded();
                    controller.OnAllEnemiesCleared();
                    RewardKind kind = arena.GetWave(wave).RewardKind;

                    if (wave == 2) state.SynchronizeArmor(29, 100);
                    if (wave == 4) state.SynchronizeArmor(30, 100);
                    RewardOffer offer = rewards.Generate(kind);
                    Assert(offer.Choices.Count == 3 &&
                           offer.Choices.Select(choice => choice.Id).Distinct(StringComparer.Ordinal).Count() == 3,
                        $"{core.DisplayName}/seed {seed}/wave {wave} 奖励不是三个唯一可选项。");

                    RewardChoice choice = SelectChoice(offer, preferredTag);
                    rewards.Choose(choice.Id);
                    if (kind is RewardKind.NormalProtocol or RewardKind.RareProtocol && !choice.IsAuxiliary)
                    {
                        observedProtocols.Add(choice.Id);
                        foreach (string tag in catalog.GetProtocol(choice.Id).Tags) observedTags.Add(tag);
                    }
                    controller.ConfirmReward(choice.Id);
                }

                Assert(controller.State == ArenaState.BossIntro,
                    $"{core.DisplayName}/seed {seed} 五波后未进入 BossIntro。");
                controller.OnBossStarted();
                controller.OnBossDefeated();
                Assert(controller.State == ArenaState.Completed,
                    $"{core.DisplayName}/seed {seed} Boss 击败后未完成竞技场。");
                Assert(state.SelectedCore == core.Id && state.Level >= 4,
                    $"{core.DisplayName}/seed {seed} 的核心或即时升级状态未贯穿完整局。");
            }

            Assert(routes.All(route => observedTags.Contains(route.Tag)),
                $"{core.DisplayName} 的 {SeedSamplesPerCore} 个完整局样本未覆盖三条构筑路线。");
            Assert(observedProtocols.Count >= 8,
                $"{core.DisplayName} 的受控随机变化不足，只观察到 {observedProtocols.Count} 个协议。");
        }
    }

    private static void AuditFreshRestart(ContentCatalog catalog, ArenaDefinition arena)
    {
        RunState first = RunState.CreateNew(28001);
        BuildController firstBuild = new(first, catalog);
        firstBuild.ApplyCore(CoreCatalog.CreateDefault().Get(CoreId.BreakthroughCannon));
        firstBuild.EndRun();
        Assert(first.SelectedCore is null && first.SelectedProtocolIds.Count == 0,
            "结束本局必须清空构筑状态。");

        RunState restarted = RunState.CreateNew(28001);
        BuildController restartedBuild = new(restarted, catalog);
        restartedBuild.ApplyCore(CoreCatalog.CreateDefault().Get(CoreId.ElectricRider));
        ArenaController restartedArena = new(restarted);
        restartedArena.BeginArena(arena);
        restartedArena.OnIntroFinished();
        Assert(restartedArena.State == ArenaState.WaveCombat &&
               restarted.SelectedCore == CoreId.ElectricRider &&
               restarted.PlayerArmor == restarted.MaximumArmor,
            "同一固定种子重开必须得到干净的新局，并允许重新选择核心。");
    }

    private static void RunLevelUpQueue(RunState state, BuildController build)
    {
        LevelUpController levelUps = new(
            state,
            build,
            new ControlledStatOfferGenerator(),
            new ExperienceCurve());
        levelUps.AddExperience(90);
        int choices = 0;
        while (levelUps.IsChoosing)
        {
            StatUpgradeOffer selected = levelUps.CurrentOffer[(state.Seed + choices) % levelUps.CurrentOffer.Count];
            levelUps.Choose(selected.Id);
            choices++;
        }
        Assert(choices == 3 && state.PendingLevelUps == 0,
            "90 战斗数据必须按 FIFO 完成 3 次即时升级，且不能遗留暂停队列。");
    }

    private static RewardChoice SelectChoice(RewardOffer offer, string preferredTag)
    {
        RewardChoice routeChoice = offer.Choices.FirstOrDefault(choice =>
            !choice.IsAuxiliary && choice.Tags.Contains(preferredTag, StringComparer.Ordinal));
        return routeChoice ?? offer.Choices[0];
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
