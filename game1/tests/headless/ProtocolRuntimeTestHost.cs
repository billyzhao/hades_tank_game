using System;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>
/// 在真实 Godot 运行时执行协议 Resource 红灯测试。
/// 该宿主不替代 NUnit；它只承载无法在纯 .NET 测试进程构造的 Godot Resource。
/// </summary>
public partial class ProtocolRuntimeTestHost : Node
{
    public override void _Ready()
    {
        string[] arguments = OS.GetCmdlineUserArgs();
        if (arguments.SequenceEqual(new[] { "--suite", "reward_catalog" }))
        {
            RunRewardCatalogSuite();
        }
        else if (arguments.SequenceEqual(new[] { "--suite", "navigation_grid" }))
        {
            RunNavigationGridSuite();
        }
        else if (arguments.SequenceEqual(new[] { "--suite", "boss_phase" }))
        {
            RunBossPhaseSuite();
        }
        else
        {
            Fail($"不支持的测试参数：{string.Join(' ', arguments)}。期望：--suite reward_catalog 或 --suite navigation_grid");
        }
    }

    private void RunRewardCatalogSuite()
    {
        int failures = 0;
        failures += Run("catalog_validate_valid_resource", CatalogValidateValidResource);
        failures += Run("catalog_validate_confirmed_department_counts", CatalogValidateConfirmedDepartmentCounts);
        failures += Run("catalog_validate_rejects_duplicate_protocol_ids", CatalogValidateRejectsDuplicateProtocolIds);
        failures += Run("catalog_validate_rejects_missing_effect_reference", CatalogValidateRejectsMissingEffectReference);
        failures += Run("catalog_validate_rejects_unsatisfied_prerequisite", CatalogValidateRejectsUnsatisfiedPrerequisite);
        failures += Run("catalog_validate_rejects_invalid_numeric_resource", CatalogValidateRejectsInvalidNumericResource);
        failures += Run("catalog_validate_rejects_invalid_protocol_metadata", CatalogValidateRejectsInvalidProtocolMetadata);
        failures += Run("catalog_validate_rejects_nonpositive_base_weight", CatalogValidateRejectsNonPositiveBaseWeight);
        failures += Run("reward_generate_deterministic_offer", RewardGenerateDeterministicOffer);
        failures += Run("reward_generate_selected_protocol_order_does_not_change_offer", RewardGenerateSelectedProtocolOrderDoesNotChangeOffer);
        failures += Run("reward_generate_rejects_catalog_version_mismatch", RewardGenerateRejectsCatalogVersionMismatch);
        failures += Run("reward_generate_excludes_conflicts_prerequisites_and_full_stacks", RewardGenerateExcludesBlockedCandidates);
        failures += Run("reward_generate_excludes_unsatisfied_required_and_conflicting_tags", RewardGenerateExcludesBlockedTags);
        failures += Run("reward_generate_uses_base_weight_for_deterministic_selection", RewardGenerateUsesBaseWeight);
        failures += Run("reward_generate_forces_first_offer_card_to_be_fully_universal", RewardGenerateForcesFullyUniversalFirstCard);
        failures += Run("build_selected_protocol_applies_only_to_current_run", BuildSelectedProtocolAppliesOnlyToCurrentRun);
        failures += Run("build_end_run_clears_selected_protocol_effects", BuildEndRunClearsSelectedProtocolEffects);
        failures += Run("build_rejects_duplicate_selection_at_stack_limit", BuildRejectsDuplicateSelectionAtStackLimit);
        failures += Run("run_loading_intro_combat_uses_point_six_seconds", RunLoadingIntroCombatUsesPointSixSeconds);
        failures += Run("run_rejects_clear_before_combat", RunRejectsClearBeforeCombat);
        failures += Run("run_defeat_outside_combat_does_not_fail", RunDefeatOutsideCombatDoesNotFail);
        failures += Run("run_defeat_in_combat_without_reboot_fails", RunDefeatInCombatWithoutRebootFails);
        failures += Run("run_relay_destroyed_in_combat_fails", RunRelayDestroyedInCombatFails);
        failures += Run("run_choice_advances_one_room_only_once", RunChoiceAdvancesOneRoomOnlyOnce);

        GD.Print($"[ProtocolRuntimeTestHost] suite=reward_catalog failures={failures}");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void RunNavigationGridSuite()
    {
        int failures = 0;
        failures += Run("navigation_grid_removed_brick_opens_path", NavigationGridRemovedBrickOpensPath);
        failures += Run("navigation_grid_invalid_or_blocked_path_returns_empty", NavigationGridInvalidOrBlockedPathReturnsEmpty);
        failures += Run("room_definitions_load_two_valid_distinct_rooms", RoomDefinitionsLoadTwoValidDistinctRooms);
        failures += Run("tile_terrain_destroyed_brick_updates_blocked_cells_once", TileTerrainDestroyedBrickUpdatesBlockedCellsOnce);

        GD.Print($"[ProtocolRuntimeTestHost] suite=navigation_grid failures={failures}");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void RunBossPhaseSuite()
    {
        int failures = 0;
        failures += Run("boss_phase_transitions_once_and_is_irrevocable", BossPhaseTransitionsOnceAndIsIrrevocable);
        failures += Run("boss_definition_and_room_are_valid", BossDefinitionAndRoomAreValid);
        failures += Run("boss_hud_tracks_health_and_phase", BossHudTracksHealthAndPhase);
        failures += Run("app_has_visible_boss_validation_entry", AppHasVisibleBossValidationEntry);

        GD.Print($"[ProtocolRuntimeTestHost] suite=boss_phase failures={failures}");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private static int Run(string name, Action assertion)
    {
        try
        {
            assertion();
            GD.Print($"[PASS] {name}");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] {name}: {exception.GetType().Name}: {exception.Message}");
            GD.PrintErr(exception.StackTrace ?? "<no stack trace>");
            return 1;
        }
    }

    private static void CatalogValidateValidResource()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Validate();
    }

    private static void CatalogValidateConfirmedDepartmentCounts()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Validate();

        Assert(catalog.Protocols.Count == 10, "确认目录必须包含十项协议。");
        Assert(catalog.Protocols.Count(protocol => protocol.Department == ProtocolDepartment.Arsenal) == 5, "兵工局协议必须为五项。");
        Assert(catalog.Protocols.Count(protocol => protocol.Department == ProtocolDepartment.Recon) == 2, "侦察组协议必须为两项。");
        Assert(catalog.Protocols.Count(protocol => protocol.Department == ProtocolDepartment.Logistics) == 2, "后勤组协议必须为两项。");
        Assert(catalog.Protocols.Count(protocol => protocol.Department == ProtocolDepartment.Engineering) == 1, "工程组协议必须为一项。");
    }

    private static void CatalogValidateRejectsDuplicateProtocolIds()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Protocols.Add(CreateProtocol("arsenal_ricochet", ProtocolDepartment.Arsenal, StatId.Damage));

        AssertThrows<ArgumentException>(catalog.Validate, "重复协议 ID 必须被目录拒绝。");
    }

    private static void CatalogValidateRejectsMissingEffectReference()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Protocols[0].Effects = new Godot.Collections.Array<ProtocolEffectDefinition> { null! };

        AssertThrows<ArgumentException>(catalog.Validate, "空效果引用必须被目录拒绝。");
    }

    private static void CatalogValidateRejectsUnsatisfiedPrerequisite()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Protocols[0].PrerequisiteIds.Add("not_in_catalog");

        AssertThrows<ArgumentException>(catalog.Validate, "不存在的前置协议必须被目录拒绝。");
    }

    private static void RewardGenerateDeterministicOffer()
    {
        ContentCatalog catalog = CreateCatalog();
        RewardGenerationInput input = new(731, 0, Array.Empty<string>(), catalog.Version);
        RewardGenerator generator = new();

        ProtocolOffer first = generator.Generate(input, catalog);
        ProtocolOffer again = generator.Generate(input, catalog);

        Assert(first.ProtocolIds.Count == 3, "奖励候选必须恰好三项。");
        Assert(first.ProtocolIds.Distinct().Count() == 3, "奖励候选 ID 不得重复。");
        Assert(first.ProtocolIds.SequenceEqual(again.ProtocolIds), "完整输入相同必须生成相同有序候选。");
    }

    private static void RewardGenerateSelectedProtocolOrderDoesNotChangeOffer()
    {
        ContentCatalog catalog = CreateCatalog();
        RewardGenerator generator = new();
        ProtocolOffer first = generator.Generate(new RewardGenerationInput(42, 1, new[] { "arsenal_damage", "recon_trail" }, catalog.Version), catalog);
        ProtocolOffer reordered = generator.Generate(new RewardGenerationInput(42, 1, new[] { "recon_trail", "arsenal_damage" }, catalog.Version), catalog);

        Assert(first.ProtocolIds.SequenceEqual(reordered.ProtocolIds), "已选协议的输入顺序不得改变候选。");
    }

    private static void RewardGenerateRejectsCatalogVersionMismatch()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Version = "headless-v2";

        AssertThrows<ArgumentException>(
            () => new RewardGenerator().Generate(new RewardGenerationInput(42, 0, Array.Empty<string>(), "headless-v1"), catalog),
            "候选输入版本与已加载目录不匹配时必须拒绝生成。");
    }

    private static void CatalogValidateRejectsInvalidNumericResource()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Protocols[0].Effects[0].FlatAdd = float.NaN;

        AssertThrows<ArgumentException>(catalog.Validate, "非有限数值必须被内容目录拒绝。");
    }

    private static void CatalogValidateRejectsInvalidProtocolMetadata()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Protocols[0].Rarity = 0;

        AssertThrows<ArgumentException>(catalog.Validate, "非正稀有度必须被内容目录拒绝。");
    }

    private static void CatalogValidateRejectsNonPositiveBaseWeight()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Protocols[0].BaseWeight = 0f;

        AssertThrows<ArgumentException>(catalog.Validate, "非正基础权重必须被内容目录拒绝。");
    }

    private static void RewardGenerateExcludesBlockedCandidates()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Protocols.First(protocol => protocol.Id == "arsenal_ricochet").ConflictIds.Add("arsenal_damage");
        catalog.Protocols.First(protocol => protocol.Id == "arsenal_heavy").PrerequisiteIds.Add("recon_trail");
        RewardGenerationInput input = new(
            187,
            2,
            new[] { "arsenal_damage", "arsenal_split" },
            catalog.Version);

        ProtocolOffer offer = new RewardGenerator().Generate(input, catalog);

        Assert(!offer.ProtocolIds.Contains("arsenal_damage"), "已满层协议不得再次成为候选。");
        Assert(!offer.ProtocolIds.Contains("arsenal_split"), "已满层协议不得再次成为候选。");
        Assert(!offer.ProtocolIds.Contains("arsenal_ricochet"), "与已选协议冲突的协议不得成为候选。");
        Assert(!offer.ProtocolIds.Contains("arsenal_heavy"), "前置条件不满足的协议不得成为候选。");
        Assert(offer.ProtocolIds.Count == 3, "排除受阻候选后仍必须返回三项候选。");
    }

    private static void RewardGenerateExcludesBlockedTags()
    {
        ContentCatalog catalog = CreateCatalog();
        catalog.Protocols.First(protocol => protocol.Id == "arsenal_ricochet").Tags.Add("kinetic");
        ProtocolDefinition satisfiedCandidate = catalog.Protocols.First(protocol => protocol.Id == "arsenal_damage");
        satisfiedCandidate.RequiredTags.Add("kinetic");
        satisfiedCandidate.BaseWeight = 1_000_000_000f;
        catalog.Protocols.First(protocol => protocol.Id == "engineering_shield").Tags.Add("repair");
        catalog.Protocols.First(protocol => protocol.Id == "arsenal_split").RequiredTags.Add("repair");
        catalog.Protocols.First(protocol => protocol.Id == "arsenal_rapid").ConflictTags.Add("kinetic");
        RewardGenerationInput input = new(194, 1, new[] { "arsenal_ricochet" }, catalog.Version);

        ProtocolOffer offer = new RewardGenerator().Generate(input, catalog);

        Assert(offer.ProtocolIds.Contains("arsenal_damage"), "已满足的需求标签协议必须可以进入候选。");
        Assert(!offer.ProtocolIds.Contains("arsenal_split"), "未满足需求标签的协议不得成为候选。");
        Assert(!offer.ProtocolIds.Contains("arsenal_rapid"), "与已选标签冲突的协议不得成为候选。");
    }

    private static void RewardGenerateUsesBaseWeight()
    {
        ContentCatalog catalog = CreateCatalog();
        foreach (ProtocolDefinition protocol in catalog.Protocols)
        {
            protocol.BaseWeight = 1f;
        }

        ProtocolDefinition dominant = catalog.Protocols.First(protocol => protocol.Id == "engineering_shield");
        dominant.BaseWeight = 1_000_000_000f;
        RewardGenerationInput input = new(812, 0, Array.Empty<string>(), catalog.Version);

        ProtocolOffer offer = new RewardGenerator().Generate(input, catalog);

        Assert(offer.ProtocolIds.Contains(dominant.Id), "基础权重显著更高的协议必须被确定性加权抽取到候选中。");
    }

    private static void RewardGenerateForcesFullyUniversalFirstCard()
    {
        ContentCatalog catalog = CreateCatalog();
        ProtocolDefinition constrained = catalog.Protocols.First(protocol => protocol.Id == "engineering_shield");
        constrained.ConflictIds.Add("arsenal_damage");
        constrained.ConflictTags.Add("kinetic");
        constrained.BaseWeight = 1_000_000_000f;

        ProtocolOffer offer = new RewardGenerator().Generate(
            new RewardGenerationInput(901, 0, Array.Empty<string>(), catalog.Version),
            catalog);
        ProtocolDefinition forcedUniversal = catalog.GetProtocol(offer.ProtocolIds[0]);

        Assert(forcedUniversal.PrerequisiteIds.Count == 0, "universal candidate must not require a protocol");
        Assert(forcedUniversal.RequiredTags.Count == 0, "universal candidate must not require a tag");
        Assert(forcedUniversal.ConflictIds.Count == 0, "universal candidate must not conflict with a protocol");
        Assert(forcedUniversal.ConflictTags.Count == 0, "universal candidate must not conflict with a tag");
    }

    private static void BuildSelectedProtocolAppliesOnlyToCurrentRun()
    {
        RunState state = RunState.CreateNew(seed: 11);
        BuildController build = new(state, CreateCatalog());
        build.SelectProtocol("arsenal_ricochet");

        Assert(build.EvaluateStat(StatId.Damage, 10f) == 12f, "已选协议必须影响本局目标属性。");
        Assert(build.EvaluateStat(StatId.DashCooldown, 10f) == 10f, "协议不得影响未声明的属性。");
    }

    private static void BuildEndRunClearsSelectedProtocolEffects()
    {
        RunState state = RunState.CreateNew(seed: 12);
        BuildController build = new(state, CreateCatalog());
        build.SelectProtocol("arsenal_ricochet");
        build.EndRun();

        Assert(build.EvaluateStat(StatId.Damage, 10f) == 10f, "本局结束后不能残留协议效果。");
    }

    private static void BuildRejectsDuplicateSelectionAtStackLimit()
    {
        BuildController build = new(RunState.CreateNew(seed: 13), CreateCatalog());
        build.SelectProtocol("arsenal_ricochet");

        AssertThrows<InvalidOperationException>(() => build.SelectProtocol("arsenal_ricochet"), "达到叠层上限后必须拒绝重复选择。");
    }

    private static void RunLoadingIntroCombatUsesPointSixSeconds()
    {
        RunController run = CreateRunController();
        Assert(run.Phase == RoomPhase.Loading, "新房间必须从 Loading 开始。");
        run.BeginRoom();
        run.Advance(.599d);
        Assert(run.Phase == RoomPhase.Intro, "Intro 未满 0.6 秒不得进入 Combat。");
        run.Advance(.001d);
        Assert(run.Phase == RoomPhase.Combat, "Intro 满 0.6 秒必须进入 Combat。");
    }

    private static void RunRejectsClearBeforeCombat()
    {
        RunController run = CreateRunController();
        AssertThrows<InvalidOperationException>(run.OnCombatCleared, "非 Combat 阶段不得清场。");
    }

    private static void RunDefeatOutsideCombatDoesNotFail()
    {
        RunController run = CreateRunController(reboots: 0);
        run.BeginRoom();
        run.OnTankDefeated();

        Assert(run.Phase == RoomPhase.Intro, "非 Combat 阶段的坦克失败不得进入 Failed。");
    }

    private static void RunDefeatInCombatWithoutRebootFails()
    {
        RunController run = CreateRunController(reboots: 0);
        run.BeginRoom();
        run.Advance(.6d);
        run.OnTankDefeated();

        Assert(run.Phase == RoomPhase.Failed, "Combat 阶段且无重启次数时必须进入 Failed。");
    }

    private static void RunRelayDestroyedInCombatFails()
    {
        RunController run = CreateRunController();
        run.BeginRoom();
        run.Advance(.6d);
        run.OnRelayDestroyed();

        Assert(run.Phase == RoomPhase.Failed, "Combat 阶段中继站摧毁时必须进入 Failed。");
    }

    private static void RunChoiceAdvancesOneRoomOnlyOnce()
    {
        RunState state = RunState.CreateNew(seed: 42);
        RunController run = CreateRunController(state);
        run.BeginRoom();
        run.Advance(.6d);
        run.OnCombatCleared();

        string selectedId = run.CurrentOffer.ProtocolIds[0];
        run.ChooseProtocol(selectedId);
        run.ChooseProtocol(selectedId);

        Assert(run.Phase == RoomPhase.Exiting, "选择协议后必须进入 Exiting。");
        Assert(state.RoomIndex == 1, "重复选择不得额外推进房间。");
    }

    private static void NavigationGridRemovedBrickOpensPath()
    {
        NavigationGrid grid = new(new Vector2I(5, 3));
        grid.Rebuild(new System.Collections.Generic.HashSet<Vector2I> { new(2, 0), new(2, 1), new(2, 2) });
        Assert(grid.FindPath(new Vector2I(0, 1), new Vector2I(4, 1)).Count == 0, "完整砖墙必须阻断路径。");

        grid.Rebuild(new System.Collections.Generic.HashSet<Vector2I> { new(2, 0), new(2, 2) });
        Assert(grid.FindPath(new Vector2I(0, 1), new Vector2I(4, 1)).Count > 0, "移除关键砖块后下一次查询必须得到路径。");
    }

    private static void BossPhaseTransitionsOnceAndIsIrrevocable()
    {
        BossPhaseController controller = new();
        int phaseEvents = 0;
        int defeatedEvents = 0;
        controller.PhaseChanged += _ => phaseEvents++;
        controller.Defeated += () => defeatedEvents++;

        Assert(controller.ReportHealth(100, 100) == BossPhase.PhaseOne, "满血必须为第一阶段。");
        Assert(controller.ReportHealth(50, 100) == BossPhase.PhaseTwo, "生命值首次到 50% 必须进入第二阶段。");
        controller.ReportHealth(40, 100);
        controller.ReportHealth(0, 100);
        controller.ReportHealth(0, 100);

        Assert(phaseEvents == 1, "阶段变化事件只能触发一次。");
        Assert(defeatedEvents == 1, "击败事件只能触发一次。");
        Assert(controller.ReportHealth(100, 100) == BossPhase.Defeated, "击败后状态不可逆。");
    }

    private static void BossDefinitionAndRoomAreValid()
    {
        BossDefinition definition = GD.Load<BossDefinition>("res://resources/bosses/roadblock_commander.tres");
        definition.Validate();

        Assert(definition.DisplayName == "路障指挥车", "Boss 资源必须使用已确认名称。");
        Assert(definition.MaximumHealth == 300, "07A Boss 最大生命必须为 300。");

        Node2D room = GD.Load<PackedScene>("res://scenes/rooms/mvp_boss_room.tscn").Instantiate<Node2D>();
        try
        {
            Assert(room.GetNodeOrNull<TileMapLayer>("Ground") is not null, "Boss 房必须有 Ground TileMapLayer。");
            Assert(room.GetNodeOrNull<TileMapLayer>("Structure") is not null, "Boss 房必须有 Structure TileMapLayer。");
            Assert(room.GetNodeOrNull<TileMapLayer>("Destructible") is not null, "Boss 房必须有 Destructible TileMapLayer。");
            Assert(room.GetNodeOrNull<RoadblockCommander>("RoadblockCommander") is not null, "Boss 房必须实例化路障指挥车。");
        }
        finally
        {
            room.Free();
        }
    }

    private void BossHudTracksHealthAndPhase()
    {
        BossDefinition definition = GD.Load<BossDefinition>("res://resources/bosses/roadblock_commander.tres");
        RoadblockCommander boss = definition.Scene.Instantiate<RoadblockCommander>();
        BossHudController hud = new();
        AddChild(boss);
        AddChild(hud);
        try
        {
            boss.Initialize(definition);
            hud.Bind(boss, definition);
            boss.ApplyDamage(new DamageContext(150));

            Assert(hud.GetNode<Label>("PhaseLabel").Text == "第二阶段", "Boss HUD 必须显示第二阶段文字。");
            Assert(hud.GetNode<ProgressBar>("HealthBar").Value == 150d, "Boss HUD 必须同步当前生命值。");
        }
        finally
        {
            hud.Unbind();
            boss.Free();
            hud.Free();
        }
    }

    private static void AppHasVisibleBossValidationEntry()
    {
        Node app = GD.Load<PackedScene>("res://scenes/app/main.tscn").Instantiate<Node>();
        try
        {
            Button button = app.GetNodeOrNull<Button>("UI/BossValidationButton");
            Assert(button is not null, "应用 HUD 必须提供可点击的 Boss 验收入口。");
            Assert(button.Text == "Boss 验收", "Boss 验收入口文案必须明确。");
        }
        finally
        {
            app.Free();
        }
    }

    private static void NavigationGridInvalidOrBlockedPathReturnsEmpty()
    {
        NavigationGrid grid = new(new Vector2I(3, 3));
        grid.Rebuild(new System.Collections.Generic.HashSet<Vector2I> { new(1, 0), new(1, 1), new(1, 2) });

        Assert(grid.FindPath(new Vector2I(-1, 0), new Vector2I(2, 0)).Count == 0, "越界起点必须安全返回空路径。");
        Assert(grid.FindPath(new Vector2I(0, 1), new Vector2I(2, 1)).Count == 0, "无可达路径必须安全返回空路径。");
    }

    private static void RoomDefinitionsLoadTwoValidDistinctRooms()
    {
        RoomDefinition first = GD.Load<RoomDefinition>("res://resources/rooms/mvp_combat_room.tres");
        RoomDefinition second = GD.Load<RoomDefinition>("res://resources/rooms/industrial_flank_room.tres");
        first.Validate();
        second.Validate();

        Assert(first.Scene.ResourcePath != second.Scene.ResourcePath, "两个房间定义必须指向不同场景。");
        Assert(first.Waves.Count > 0 && second.Waves.Count > 0, "两个房间必须拥有非空波次定义。");
        Assert(first.EnemySpawnPoints.Count > 0 && second.EnemySpawnPoints.Count > 0, "两个房间必须各自声明敌军出生点。");
        Assert(!first.EnemySpawnPoints.SequenceEqual(second.EnemySpawnPoints), "工业侧翼房必须使用不同的敌军出生边配置。");

        AssertRoomHasTerrainLayers(first, "首战房");
        AssertRoomHasTerrainLayers(second, "工业侧翼房");
    }

    private static void AssertRoomHasTerrainLayers(RoomDefinition definition, string roomName)
    {
        Node2D room = definition.Scene.Instantiate<Node2D>();
        try
        {
            Assert(room.GetNodeOrNull<TileMapLayer>("Ground") is not null, $"{roomName}必须有 Ground TileMapLayer。");
            Assert(room.GetNodeOrNull<TileMapLayer>("Structure") is not null, $"{roomName}必须有 Structure TileMapLayer。");
            Assert(room.GetNodeOrNull<TileMapLayer>("Destructible") is not null, $"{roomName}必须有 Destructible TileMapLayer。");
        }
        finally
        {
            room.Free();
        }
    }

    private static void TileTerrainDestroyedBrickUpdatesBlockedCellsOnce()
    {
        TileTerrainAdapter terrain = new();
        TileMapLayer layer = new();
        try
        {
            terrain.Initialize(layer, new[] { new Vector2I(1, 1) }, hitPoints: 2);
            int eventCount = 0;
            terrain.BrickDestroyed += _ => eventCount++;

            terrain.DamageBrick(new Vector2I(1, 1), 2);
            terrain.DamageBrick(new Vector2I(1, 1), 2);

            Assert(!terrain.BlockedNavigationCells.Contains(new Vector2I(1, 1)), "砖块耐久归零后必须从阻塞格集合移除。");
            Assert(eventCount == 1, "同一砖块的破坏事件只能发送一次。");
        }
        finally
        {
            terrain.Free();
            layer.Free();
        }
    }

    private static ContentCatalog CreateCatalog()
    {
        return new ContentCatalog
        {
            Version = "headless-v1",
            Protocols = new Godot.Collections.Array<ProtocolDefinition>
            {
                CreateProtocol("arsenal_ricochet", ProtocolDepartment.Arsenal, StatId.Damage),
                CreateProtocol("arsenal_damage", ProtocolDepartment.Arsenal, StatId.Damage),
                CreateProtocol("arsenal_split", ProtocolDepartment.Arsenal, StatId.Damage),
                CreateProtocol("arsenal_rapid", ProtocolDepartment.Arsenal, StatId.FireCooldown),
                CreateProtocol("arsenal_heavy", ProtocolDepartment.Arsenal, StatId.Damage),
                CreateProtocol("recon_trail", ProtocolDepartment.Recon, StatId.DashCooldown),
                CreateProtocol("recon_cooldown", ProtocolDepartment.Recon, StatId.DashCooldown),
                CreateProtocol("logistics_armor", ProtocolDepartment.Logistics, StatId.ArmorMax),
                CreateProtocol("logistics_repair", ProtocolDepartment.Logistics, StatId.RelayRepair),
                CreateProtocol("engineering_shield", ProtocolDepartment.Engineering, StatId.RelayShield)
            }
        };
    }

    private static RunController CreateRunController(int reboots = 1)
    {
        return CreateRunController(RunState.CreateNew(seed: 42, reboots: reboots));
    }

    private static RunController CreateRunController(RunState state)
    {
        ContentCatalog catalog = CreateCatalog();
        return new RunController(state, new BuildController(state, catalog), new RewardGenerator());
    }

    private static ProtocolDefinition CreateProtocol(string id, ProtocolDepartment department, StatId stat)
    {
        return new ProtocolDefinition
        {
            Id = id,
            DisplayName = id,
            Description = id,
            Department = department,
            Rarity = 1,
            BaseWeight = 1f,
            StackLimit = 1,
            Effects = new Godot.Collections.Array<ProtocolEffectDefinition>
            {
                new ProtocolEffectDefinition
                {
                    EffectId = $"{id}_effect",
                    Stat = stat,
                    // 让构筑作用域测试使用与验收公式一致的真实资源数值；其他协议仍保持零修正。
                    FlatAdd = id == "arsenal_ricochet" ? 2f : 0f
                }
            }
        };
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private void Fail(string message)
    {
        GD.PrintErr($"[FAIL] ProtocolRuntimeTestHost: {message}");
        GetTree().Quit(2);
    }
}
