using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>
/// 顶层装配器：只组合 RunController、ArenaController、WaveDirector 与 UI。
/// 不在 _Process 中重复波次、清场或奖励规则。
/// </summary>
public partial class AppRoot : Node
{
    private static readonly ArenaDefinition BlockadeCityArena =
        GD.Load<ArenaDefinition>("res://resources/arenas/blockade_city_arena.tres");
    private static readonly ContentCatalog ProtocolCatalog =
        GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
    private static readonly CoreCatalog MobileCoreCatalog = CoreCatalog.CreateDefault();
    private static readonly BossDefinition RoadblockCommanderDefinition =
        GD.Load<BossDefinition>("res://resources/bosses/roadblock_commander.tres");

    public RunState CurrentRun { get; private set; } = null!;

    private HealthComponent _playerHealth = null!;
    private RebootController _rebootController = null!;
    private BuildController _buildController = null!;
    private LevelUpController _levelUpController = null!;
    private RewardController _rewardController = null!;
    private CombatDataCollector _combatDataCollector = null!;
    private PauseCoordinator _pauseCoordinator = null!;
    private RunController _runController = null!;
    private ArenaController _arenaController = null!;
    private WaveDirector _waveDirector;
    private Node _waveDirectorHost = null!;
    private IReadOnlyList<SpawnEntrance> _spawnEntrances = Array.Empty<SpawnEntrance>();
    private RoomNavigationFactory _navigationFactory;

    private Label _armorLabel = null!;
    private Label _coreLabel = null!;
    private Label _rebootLabel = null!;
    private Label _enemyLabel = null!;
    private Label _arenaLabel = null!;
    private Label _waveLabel = null!;
    private Label _levelLabel = null!;
    private Label _experienceLabel = null!;
    private ProgressBar _experienceBar = null!;
    private Label _eventLabel = null!;
    private Label _buildLabel = null!;
    private Label _auxiliaryLabel = null!;
    private AcceptanceMenu _acceptanceMenu = null!;
    private WaveRewardPanel _waveRewardPanel = null!;
    private StartScreen _startScreen = null!;
    private CoreSelectionPanel _coreSelectionPanel = null!;
    private LevelUpPanel _levelUpPanel = null!;
    private BossHudController _bossHud = null!;
    private RoadblockCommander _activeBoss;
    private RunResultScreen _runResultScreen = null!;
    private DebugOverlay _debugOverlay = null!;
    private SaveService _saveService = new();
    private SaveData _saveData = null!;
    private ulong _runStartedAtMsec;
    private bool _resultShown;
    private bool _autoAdvanceAcceptanceWave;
    private bool _awaitingWaveRewardAfterLevelUps;

    public override void _Ready()
    {
        CurrentRun = RunState.CreateNew(System.Environment.TickCount);
        _saveData = _saveService.LoadOrDefault();
        _runStartedAtMsec = Time.GetTicksMsec();
        ProtocolCatalog.Validate();
        BlockadeCityArena.Validate();

        _buildController = new BuildController(CurrentRun, ProtocolCatalog);
        _runController = new RunController(CurrentRun, _buildController, playableArenaCount: 1);
        _runController.PhaseChanged += OnRunPhaseChanged;
        _levelUpController = new LevelUpController(CurrentRun, _buildController, new ControlledStatOfferGenerator(), new ExperienceCurve());
        _levelUpController.OfferRequested += ShowLevelUpOffer;
        _levelUpController.QueueDrained += OnLevelUpQueueDrained;
        _rewardController = new RewardController(
            CurrentRun,
            _buildController,
            ProtocolCatalog,
            new RewardGenerator(),
            new MaintenanceRewardGenerator());

        _pauseCoordinator = new PauseCoordinator(GetTree());
        PauseController pauseController = new();
        pauseController.Configure(_pauseCoordinator);
        AddChild(pauseController);

        BindHudNodes();
        CreateUiControllers();
        BindAcceptanceMenu();
        _buildController.SnapshotChanged += () =>
        {
            ApplyBuildStatsToPlayer();
            UpdateBuildHud();
        };

        _debugOverlay = GD.Load<PackedScene>("res://scenes/ui/debug_overlay.tscn").Instantiate<DebugOverlay>();
        AddChild(_debugOverlay);
        _debugOverlay.Bind(CurrentRun, _saveData, () => _runController.Phase);

        _arenaController = new ArenaController(CurrentRun);
        _arenaController.StateChanged += OnArenaStateChanged;
        _arenaController.WaveRequested += StartWave;
        _arenaController.RewardRequested += ShowWaveReward;
        _arenaController.BossRequested += BeginBossEncounter;
        _arenaController.ArenaFailed += ShowRunFailure;
        Node2D arena = BlockadeCityArena.Scene.Instantiate<Node2D>();
        GetNode<Node>("ArenaHost").AddChild(arena);
        BindArena(arena);
        _arenaController.BeginArena(BlockadeCityArena);
        _pauseCoordinator.Acquire(PauseReason.StartScreen);
        UpdateHud();
        UpdateBuildHud();
    }

    /// <summary>仅供同程序集的场景级测试在进入场景树前隔离存档路径。</summary>
    internal void ConfigureSaveServiceForTesting(SaveService saveService)
    {
        if (IsInsideTree())
            throw new InvalidOperationException("测试存档服务必须在 AppRoot 进入场景树前注入。");
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
    }

    private void BindHudNodes()
    {
        _armorLabel = GetNode<Label>("UI/Hud/ArmorLabel");
        _coreLabel = GetNode<Label>("UI/Hud/CoreLabel");
        _rebootLabel = GetNode<Label>("UI/Hud/RebootLabel");
        _enemyLabel = GetNode<Label>("UI/Hud/EnemyLabel");
        _arenaLabel = GetNode<Label>("UI/Hud/ArenaLabel");
        _waveLabel = GetNode<Label>("UI/Hud/WaveLabel");
        _levelLabel = GetNode<Label>("UI/LevelLabel");
        _experienceLabel = GetNode<Label>("UI/ExperienceLabel");
        _experienceBar = GetNode<ProgressBar>("UI/ExperienceBar");
        _eventLabel = GetNode<Label>("UI/EventLabel");
        _buildLabel = GetNode<Label>("UI/BuildLabel");
        _auxiliaryLabel = GetNode<Label>("UI/AuxiliaryLabel");
        _acceptanceMenu = GetNode<AcceptanceMenu>("UI/AcceptanceMenu");
        _acceptanceMenu.Visible = OS.IsDebugBuild();
    }

    private void CreateUiControllers()
    {
        CanvasLayer ui = GetNode<CanvasLayer>("UI");
        _startScreen = new StartScreen();
        ui.AddChild(_startScreen);
        _startScreen.StartRequested += BeginRunFromStartScreen;
        _startScreen.QuitRequested += () => GetTree().Quit();
        _waveRewardPanel = new WaveRewardPanel();
        ui.AddChild(_waveRewardPanel);
        _waveRewardPanel.RewardConfirmed += OnWaveRewardChosen;
        _coreSelectionPanel = new CoreSelectionPanel();
        ui.AddChild(_coreSelectionPanel);
        _coreSelectionPanel.CoreChosen += ChooseCore;
        _levelUpPanel = new LevelUpPanel();
        ui.AddChild(_levelUpPanel);
        _levelUpPanel.UpgradeChosen += _levelUpController.Choose;

        _bossHud = new BossHudController { Visible = false };
        ui.AddChild(_bossHud);
        _runResultScreen = new RunResultScreen();
        ui.AddChild(_runResultScreen);
        _runResultScreen.RetryRequested += ReloadRun;
        _runResultScreen.ReturnRequested += ReloadRun;
    }

    private void BeginRunFromStartScreen()
    {
        _runStartedAtMsec = Time.GetTicksMsec();
        _startScreen.Dismiss();
        ShowCoreSelection();
        _pauseCoordinator.Release(PauseReason.StartScreen);
    }

    private async void BeginArenaAfterIntro()
    {
        await ToSignal(GetTree().CreateTimer(0.6d), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree() && _arenaController.State == ArenaState.Intro)
            _arenaController.OnIntroFinished();
    }

    private void ShowCoreSelection()
    {
        _pauseCoordinator.Acquire(PauseReason.CoreSelection);
        _coreSelectionPanel.ShowChoices(MobileCoreCatalog);
        _eventLabel.Text = "开局整备：选择移动核心后进入第一波";
        _acceptanceMenu.SetStatus("请选择一个移动核心；核心只改变初始操作节奏，不锁定协议路线。");
    }

    private void ChooseCore(CoreId coreId)
    {
        _buildController.ApplyCore(MobileCoreCatalog.Get(coreId));
        _pauseCoordinator.Release(PauseReason.CoreSelection);
        UpdateHud();
        UpdateBuildHud();
        BeginArenaAfterIntro();
    }

    private void BindArena(Node2D arena)
    {
        BindPlayerAndReboot(arena);
        _waveDirectorHost = arena.GetNode<Node>("WaveDirectorHost");
        _spawnEntrances = CollectEntrances(arena.GetNode<Node>("SpawnEntrances"));
        ReplaceNavigationFactory(arena, BlockadeCityArena.GridSize, BlockadeCityArena.CellSize);
        _combatDataCollector = new CombatDataCollector();
        arena.AddChild(_combatDataCollector);
        _combatDataCollector.DataCollected += amount =>
        {
            _levelUpController.AddExperience(amount);
            UpdateHud();
            _eventLabel.Text = $"战斗数据 +{amount}：当前 {CurrentRun.Experience}/{new ExperienceCurve().GetRequiredExperience(CurrentRun.Level)}";
            _acceptanceMenu.SetStatus($"已收集战斗数据 +{amount}，等级 {CurrentRun.Level}。 ");
        };
    }

    private void BindPlayerAndReboot(Node2D arena)
    {
        PlayerTank player = arena.GetNode<PlayerTank>("PlayerTank");
        _playerHealth = player.GetNode<HealthComponent>("HealthComponent");
        _playerHealth.ValueChanged += (armor, _) =>
        {
            CurrentRun.SynchronizeArmor(armor, _playerHealth.MaximumArmor);
            UpdateHud();
        };
        _playerHealth.Depleted += () => _eventLabel.Text = "坦克报废：移动核心开始裁决重启";
        _playerHealth.InitializeArmor(CurrentRun.PlayerArmor, CurrentRun.MaximumArmor);
        player.AttachBuild(_buildController);
        player.GetNode<WeaponController>("WeaponController").AttachBuild(_buildController);
        AuxiliaryHost auxiliaryHost = player.GetNode<AuxiliaryHost>("AuxiliaryHost");
        auxiliaryHost.Activate(player);
        auxiliaryHost.AttachBuild(_buildController, ProtocolCatalog);
        player.GetNode<DashComponent>("DashComponent").AttachBuild(_buildController);

        _rebootController = arena.GetNode<RebootController>("RebootController");
        _rebootController.Configure(_runController, CurrentRun);
        _rebootController.RebootStarted += seconds =>
        {
            _eventLabel.Text = $"移动核心重构中：原地保持 {seconds:0.0} 秒";
            _acceptanceMenu.SetStatus("重构中：位置保持，装甲暂为 0");
        };
        _rebootController.Rebooted += () =>
        {
            _eventLabel.Text = "重启成功：原地恢复 50% 装甲，保护 2.0 秒";
            _acceptanceMenu.SetStatus("重启完成：原地、50% 装甲、2 秒保护");
        };
        _rebootController.RunFailed += () =>
        {
            _eventLabel.Text = "本局失败：坦克报废且重启次数耗尽";
            _acceptanceMenu.SetStatus("失败：没有剩余重启次数");
        };
    }

    private static IReadOnlyList<SpawnEntrance> CollectEntrances(Node host)
    {
        List<SpawnEntrance> entrances = new();
        foreach (Marker2D marker in host.GetChildren().OfType<Marker2D>())
        {
            Vector2 facing = Vector2.Up.Rotated(marker.GlobalRotation);
            entrances.Add(new SpawnEntrance(marker.Name, marker.GlobalPosition, facing, 0.35f));
        }
        if (entrances.Count != 4)
            throw new InvalidOperationException("封锁城区必须提供四个边缘出生入口。");
        return entrances;
    }

    private void StartWave(WaveDefinition definition)
    {
        if (_waveDirector is not null && IsInstanceValid(_waveDirector))
            _waveDirector.QueueFree();

        _waveDirector = new WaveDirector();
        _waveDirectorHost.AddChild(_waveDirector);
        _waveDirector.TimeChanged += seconds =>
        {
            string stage = _waveDirector.IsSpawning ? "刷新" : "清场";
            _waveLabel.Text = $"波次  {definition.WaveNumber}/5  {stage}  {seconds:0.0}s";
        };
        _waveDirector.EnemyCountChanged += count => _enemyLabel.Text = $"敌军  {count}";
        _waveDirector.EnemySpawned += (behavior, elite) =>
            _eventLabel.Text = elite
                ? "第 5 波精英槽位已入场：金色重装单位必须击毁"
                : $"第 {definition.WaveNumber} 波增援：{BehaviorName(behavior)}";
        _waveDirector.EnemyDefeated += (position, elite) =>
            _combatDataCollector.Spawn(_waveDirectorHost, position, elite ? 15 : 5);
        _waveDirector.EliteStateChanged += alive =>
        {
            if (alive) _eventLabel.Text = "精英在场：清场奖励被锁定";
        };
        _waveDirector.SpawnWindowEnded += () => _arenaController.OnWaveSpawnWindowEnded();
        _waveDirector.AllEnemiesCleared += OnWaveEnemiesCleared;
        _waveDirector.Configure(
            definition,
            _spawnEntrances,
            CurrentRun.Seed,
            CurrentRun.ArenaIndex,
            CurrentRun.WaveIndex,
            _navigationFactory.Provider);
        _waveDirector.StartWave();
        _acceptanceMenu.SetStatus($"第 {definition.WaveNumber} 波开始：可用“结束当前刷新窗口”快速进入清场验收。");
    }

    private void ShowWaveReward(RewardKind kind)
    {
        if (_autoAdvanceAcceptanceWave)
        {
            _autoAdvanceAcceptanceWave = false;
            _arenaController.ConfirmReward($"acceptance_auto_wave_{_arenaController.CurrentWave}");
            _acceptanceMenu.SetStatus("验收命令已完成当前波并进入下一波。");
            return;
        }

        RewardOffer offer = _rewardController.Generate(kind);
        _waveRewardPanel.ShowOffer(offer);
        _eventLabel.Text = $"第 {_arenaController.CurrentWave} 波清场：选择{RewardKindName(kind)}后继续";
    }

    private void OnWaveRewardChosen(string choiceId)
    {
        if (_rewardController.CurrentOffer is null)
        {
            _arenaController.ConfirmReward(choiceId);
            return;
        }

        RewardChoice chosen = _rewardController.Choose(choiceId);
        if (chosen.Id == "maintenance_repair_25")
        {
            _playerHealth.SetArmor(CurrentRun.PlayerArmor);
            UpdateHud();
        }
        _eventLabel.Text = $"已选择：{chosen.DisplayName}";
        _acceptanceMenu.SetStatus($"奖励已应用：{chosen.DisplayName}；已进入下一波。 ");
        _arenaController.ConfirmReward(chosen.Id);
    }

    private void OnWaveEnemiesCleared()
    {
        _combatDataCollector.CollectAllAtWaveEnd();
        if (_levelUpController.IsChoosing || CurrentRun.PendingLevelUps > 0)
        {
            _awaitingWaveRewardAfterLevelUps = true;
            return;
        }
        _arenaController.OnAllEnemiesCleared();
    }

    private void ShowLevelUpOffer(int level, IReadOnlyList<StatUpgradeOffer> offers)
    {
        _pauseCoordinator.Acquire(PauseReason.LevelUp);
        _levelUpPanel.ShowOffer(level, offers);
    }

    private void OnLevelUpQueueDrained()
    {
        if (!_levelUpPanel.Visible) return;
        _levelUpPanel.HideOffer();
        _pauseCoordinator.Release(PauseReason.LevelUp);
        _playerHealth.GrantInvulnerability(0.4d);
        if (_awaitingWaveRewardAfterLevelUps)
        {
            _awaitingWaveRewardAfterLevelUps = false;
            _arenaController.OnAllEnemiesCleared();
        }
    }

    private void OnArenaStateChanged(ArenaState state)
    {
        _arenaLabel.Text = "封锁城区";
        _levelLabel.Text = $"等级 {CurrentRun.Level}";
        int requiredExperience = new ExperienceCurve().GetRequiredExperience(CurrentRun.Level);
        _experienceLabel.Text = $"数据 {CurrentRun.Experience}/{requiredExperience}";
        _experienceBar.MaxValue = requiredExperience;
        _experienceBar.Value = CurrentRun.Experience;
        switch (state)
        {
            case ArenaState.Intro:
                _eventLabel.Text = "封锁城区：四周入口已部署，准备进入第一波";
                break;
            case ArenaState.WaveCombat:
                _eventLabel.Text = $"第 {_arenaController.CurrentWave} 波刷新中：消灭敌军并保持移动";
                break;
            case ArenaState.Cleanup:
                _eventLabel.Text = "刷新已停止：必须清除全部残敌后才能结算";
                break;
            case ArenaState.BossIntro:
                _eventLabel.Text = "五波完成：路障指挥车即将进入封锁城区";
                _waveLabel.Text = "Boss  即将入场";
                break;
        }
        UpdateHud();
    }

    private void BeginBossEncounter()
    {
        EnterBossValidationRoom();
    }

    private void BindAcceptanceMenu()
    {
        _acceptanceMenu.DamageRequested += amount =>
        {
            _playerHealth.ApplyDamage(new DamageContext(amount));
            _acceptanceMenu.SetStatus($"已请求装甲伤害 {amount}，当前 {CurrentRun.PlayerArmor}/{CurrentRun.MaximumArmor}");
        };
        _acceptanceMenu.ArmorPercentRequested += percent =>
        {
            int armor = (int)Math.Floor(_playerHealth.MaximumArmor * Math.Clamp(percent, 0, 100) / 100d);
            _playerHealth.SetArmor(armor);
            _acceptanceMenu.SetStatus($"装甲已设为 {percent}%：下一次维护奖励必须包含应急装甲修复。 ");
        };
        _acceptanceMenu.DefeatRequested += () =>
        {
            _playerHealth.ApplyDamage(new DamageContext(Math.Max(1, _playerHealth.Armor)));
            _acceptanceMenu.SetStatus("已请求坦克报废，请观察重构或失败流程");
        };
        _acceptanceMenu.StopWaveSpawningRequested += () =>
        {
            _waveDirector?.StopSpawning();
            _acceptanceMenu.SetStatus("已结束当前刷新窗口；残敌仍在，可用“敌军全灭”继续验证清场门禁。");
        };
        _acceptanceMenu.ClearWaveEnemiesRequested += () =>
        {
            _waveDirector?.ClearAliveEnemiesForAcceptance();
            _acceptanceMenu.SetStatus(_waveDirector?.IsSpawning == true
                ? "已清空当前敌军；刷新仍在进行，导演会继续补充。"
                : "已清空残敌；应立即进入波间确认。");
        };
        _acceptanceMenu.CompleteWaveRequested += CompleteCurrentWaveForAcceptance;
        _acceptanceMenu.AdvanceWaveRequested += AdvanceWaveForAcceptance;
        _acceptanceMenu.EndRunRequested += () =>
        {
            _runController.OnArenaFailed();
            _acceptanceMenu.SetStatus("验收命令已结束本局。");
        };
        _acceptanceMenu.ExperienceRequested += amount =>
        {
            _levelUpController.AddExperience(amount);
            UpdateHud();
            _acceptanceMenu.SetStatus("已授予战斗数据；若达到阈值应立即进入完全暂停升级。");
        };
        _acceptanceMenu.AuxiliaryRequested += auxiliaryId =>
        {
            try
            {
                int rank = _buildController.AddOrUpgradeAuxiliary(auxiliaryId);
                UpdateHud();
                _acceptanceMenu.SetStatus($"已授予辅助：{ProtocolCatalog.GetAuxiliary(auxiliaryId).DisplayName} Mk.{rank}，请观察其自动攻击。 ");
            }
            catch (InvalidOperationException exception)
            {
                _acceptanceMenu.SetStatus($"辅助请求未应用：{exception.Message}");
            }
        };
        _acceptanceMenu.BossRequested += () =>
        {
            if (_arenaController.State == ArenaState.BossIntro) BeginBossEncounter();
            else _acceptanceMenu.SetStatus("请先完成第 5 波与稀有奖励；Boss 只能从 BossIntro 正式入场。 ");
        };
        _acceptanceMenu.BossPhaseTwoRequested += () =>
        {
            if (_activeBoss is null || !IsInstanceValid(_activeBoss))
            {
                _acceptanceMenu.SetStatus("当前没有正在战斗的 Boss；请先完成第五波并进入 Boss 验收。");
                return;
            }

            int phaseTwoHealth = _activeBoss.MaximumHealth / 2;
            int requiredDamage = Mathf.Max(0, _activeBoss.CurrentHealth - phaseTwoHealth);
            if (requiredDamage > 0) _activeBoss.ApplyDamage(new DamageContext(requiredDamage));
            _acceptanceMenu.SetStatus("已通过正式伤害路径推进到第二阶段；请观察冲锋终点和散热弱点窗口。");
        };
        _acceptanceMenu.BossDefeatRequested += () =>
        {
            if (_activeBoss is null || !IsInstanceValid(_activeBoss))
            {
                _acceptanceMenu.SetStatus("当前没有正在战斗的 Boss；请先完成第五波。");
                return;
            }

            DamageResult result = _activeBoss.ApplyDamage(new DamageContext(_activeBoss.MaximumHealth));
            _acceptanceMenu.SetStatus(result.DepletedNow
                ? "已通过正式伤害路径击败 Boss；请检查封锁城区胜利结算。"
                : "Boss 当前处于二阶段装甲锁定；重新开始后可在一阶段直接验证胜利结算。");
        };
        _acceptanceMenu.RestartRequested += ReloadRun;
    }

    private void CompleteCurrentWaveForAcceptance()
    {
        if (_arenaController.State is not (ArenaState.WaveCombat or ArenaState.Cleanup))
        {
            _acceptanceMenu.SetStatus("当前不在战斗或清场阶段，不能结束本轮。");
            return;
        }

        _waveDirector?.StopSpawning();
        _waveDirector?.ClearAliveEnemiesForAcceptance();
        _acceptanceMenu.SetStatus("验收命令已结束本轮；波间确认面板应出现。");
    }

    private void AdvanceWaveForAcceptance()
    {
        if (_arenaController.State == ArenaState.Reward)
        {
            _arenaController.ConfirmReward($"acceptance_next_wave_{_arenaController.CurrentWave}");
            _acceptanceMenu.SetStatus("验收命令已确认奖励并进入下一波。");
            return;
        }

        if (_arenaController.State is not (ArenaState.WaveCombat or ArenaState.Cleanup))
        {
            _acceptanceMenu.SetStatus("当前阶段不能直接进入下一波。");
            return;
        }

        _autoAdvanceAcceptanceWave = true;
        CompleteCurrentWaveForAcceptance();
    }

    private void OnRunPhaseChanged(RunPhase phase)
    {
        switch (phase)
        {
            case RunPhase.Completed:
                ShowRunVictory();
                break;
            case RunPhase.Failed:
                _arenaController?.OnPlayerRunFailed();
                ShowRunFailure();
                break;
        }
    }

    private void UpdateHud()
    {
        if (_playerHealth is null) return;
        _armorLabel.Text = $"装甲  {_playerHealth.Armor}/{_playerHealth.MaximumArmor}";
        _coreLabel.Text = CurrentRun.SelectedCore is CoreId selectedCore
            ? $"核心  {MobileCoreCatalog.Get(selectedCore).DisplayName}"
            : "核心  待选择";
        _rebootLabel.Text = $"重启  {CurrentRun.RebootsRemaining}";
        _arenaLabel.Text = "封锁城区";
        _levelLabel.Text = $"等级 {CurrentRun.Level}";
        int requiredExperience = new ExperienceCurve().GetRequiredExperience(CurrentRun.Level);
        _experienceLabel.Text = $"数据 {CurrentRun.Experience}/{requiredExperience}";
        _experienceBar.MaxValue = requiredExperience;
        _experienceBar.Value = CurrentRun.Experience;
        _auxiliaryLabel.Text = CurrentRun.AuxiliarySlots.Count == 0
            ? "辅助槽  [空]  [空]"
            : $"辅助槽  {string.Join("  |  ", CurrentRun.AuxiliarySlots.Select(slot => $"{ProtocolCatalog.GetAuxiliary(slot.AuxiliaryId).DisplayName} Mk.{slot.Rank}"))}";
        if (_waveDirector is null)
            _waveLabel.Text = $"波次  {_arenaController?.CurrentWave ?? 1}/5  准备中";
    }

    private void UpdateBuildHud()
    {
        if (CurrentRun.SelectedCore is null && CurrentRun.SelectedProtocolIds.Count == 0)
        {
            _buildLabel.Text = "构筑：请先选择移动核心";
            return;
        }
        string core = CurrentRun.SelectedCore is CoreId selectedCore
            ? MobileCoreCatalog.Get(selectedCore).DisplayName
            : "未选择核心";
        string protocols = CurrentRun.SelectedProtocolIds.Count == 0
            ? "未获得协议"
            : string.Join("  |  ", CurrentRun.SelectedProtocolIds.Select(id => ProtocolCatalog.GetProtocol(id).DisplayName));
        _buildLabel.Text = $"构筑：{core}  |  {protocols}";
    }

    private void ApplyBuildStatsToPlayer()
    {
        if (_playerHealth is null) return;
        int maximumArmor = Mathf.RoundToInt(_buildController.EvaluateStat(StatId.ArmorMax, 100f));
        if (maximumArmor == _playerHealth.MaximumArmor) return;
        _playerHealth.InitializeArmor(_playerHealth.Armor, maximumArmor);
    }

    private void EnterBossValidationRoom()
    {
        _bossHud.Unbind();
        _waveRewardPanel.Hide();
        foreach (Node projectile in GetTree().GetNodesInGroup("enemy_projectiles")) projectile.QueueFree();
        Node2D room = GetNode<Node>("ArenaHost").GetChild(0) as Node2D
            ?? throw new InvalidOperationException("Boss 必须在当前竞技场实例中登场。 ");
        RoadblockCommander boss = RoadblockCommanderDefinition.Scene.Instantiate<RoadblockCommander>();
        _activeBoss = boss;
        boss.Name = "RoadblockCommander";
        boss.GlobalPosition = new Vector2(240f, 66f);
        room.AddChild(boss);
        boss.Initialize(RoadblockCommanderDefinition);
        BossEncounterController encounter = new() { Name = "BossEncounterController" };
        encounter.PhaseOneBarrierCells = new Godot.Collections.Array<Vector2I>
            { new(7, 3), new(12, 3), new(6, 6), new(13, 6) };
        encounter.PhaseTwoOpeningCells = new Godot.Collections.Array<Vector2I>
            { new(8, 8), new(8, 9), new(11, 8), new(11, 9) };
        encounter.AddChild(new BarrierDeployment { Name = "BarrierDeployment" });
        encounter.AddChild(new BossGunEmplacement { Name = "BossGunEmplacement", Position = new Vector2(420f, 132f) });
        encounter.AddChild(new BossSummonController
        {
            Name = "BossSummonController",
            MaximumAlive = 2,
            SpawnPoints = new Godot.Collections.Array<Vector2> { new(72f, 42f), new(408f, 42f) }
        });
        room.AddChild(encounter);
        encounter.Initialize(boss, room, _navigationFactory, RoadblockCommanderDefinition.CellSize);
        _bossHud.Bind(boss, RoadblockCommanderDefinition);
        boss.Defeated += ShowBossResult;
        if (_arenaController.State == ArenaState.BossIntro) _arenaController.OnBossStarted();
        _eventLabel.Text = "路障指挥车入场：只锁定玩家坦克；拆障、躲避冲撞并攻击弱点窗口";
        _waveLabel.Text = "阶段  1/2";
        UpdateHud();
    }

    private void ShowBossResult()
    {
        if (_resultShown) return;
        _activeBoss = null;
        _arenaController.OnBossDefeated();
        ClearCombatActors();
        CurrentRun.RestoreArmorForNextArena();
        _playerHealth.SetArmor(CurrentRun.PlayerArmor);
        _runController.OnArenaCompleted();
    }

    private void ShowRunVictory()
    {
        if (_resultShown) return;
        _resultShown = true;
        _bossHud.Unbind();
        ClearCombatActors();
        _waveRewardPanel.Hide();
        RunResultSnapshot snapshot = CreateResultSnapshot();
        SaveLastRun(snapshot, "victory");
        _pauseCoordinator.Acquire(PauseReason.RunResult);
        _runResultScreen.ShowResult(snapshot, true);
        _waveLabel.Text = "封锁城区  已完成";
        _arenaLabel.Text = "封锁城区";
        _eventLabel.Text = "封锁城区突破完成：可重新开始挑战其他核心与构筑。";
        _acceptanceMenu.SetStatus("封锁城区已完成：胜利结算已记录本局核心、协议、等级与耗时。");
    }

    private void ShowRunFailure()
    {
        if (_resultShown) return;
        _resultShown = true;
        ClearCombatActors();
        _waveRewardPanel.Hide();
        RunResultSnapshot snapshot = CreateResultSnapshot();
        SaveLastRun(snapshot, "failed");
        _pauseCoordinator.Acquire(PauseReason.RunResult);
        _runResultScreen.ShowResult(snapshot, false);
        _eventLabel.Text = "坦克报废：可立即重试本局";
    }

    private void ClearCombatActors()
    {
        foreach (Node enemy in GetTree().GetNodesInGroup("enemies")) enemy.QueueFree();
        foreach (Node projectile in GetTree().GetNodesInGroup("enemy_projectiles")) projectile.QueueFree();
    }

    private RunResultSnapshot CreateResultSnapshot()
    {
        ulong elapsedMsec = Time.GetTicksMsec() - _runStartedAtMsec;
        string coreId = CurrentRun.SelectedCore?.ToString() ?? string.Empty;
        return new RunResultSnapshot(CurrentRun.Seed, CurrentRun.SelectedProtocolIds, coreId,
            CurrentRun.ArenaIndex, CurrentRun.WaveIndex, CurrentRun.Level, TimeSpan.FromMilliseconds(elapsedMsec));
    }

    private void SaveLastRun(RunResultSnapshot snapshot, string result)
    {
        _saveData.LastRun = new LastRunSummary
        {
            Seed = snapshot.Seed,
            CoreId = snapshot.CoreId,
            ArenaIndex = snapshot.ArenaIndex,
            WaveIndex = snapshot.WaveIndex,
            Level = snapshot.Level,
            ElapsedSeconds = snapshot.Elapsed.TotalSeconds,
            Result = result
        };
        _saveService.SaveAtomic(_saveData);
    }

    private static string BehaviorName(BehaviorId behavior) => behavior switch
    {
        BehaviorId.Scout => "侦察无人机（高速骚扰）",
        BehaviorId.Patrol => "巡逻坦克（追击玩家）",
        BehaviorId.Assault => "突击车（快速压迫玩家）",
        BehaviorId.Mortar => "迫击炮车（远程预警）",
        _ => "未知单位"
    };

    private static string RewardKindName(RewardKind kind) => kind switch
    {
        RewardKind.NormalProtocol => "协议节奏",
        RewardKind.Maintenance => "维护节奏",
        RewardKind.RareProtocol => "稀有协议节奏",
        _ => "奖励"
    };

    private void ReplaceNavigationFactory(Node2D arena, Vector2I gridSize, int cellSize)
    {
        DisposeNavigationFactory();
        _navigationFactory = new RoomNavigationFactory(arena, gridSize, cellSize);
    }

    private void ClearArenaHost()
    {
        Node arenaHost = GetNode<Node>("ArenaHost");
        foreach (Node child in arenaHost.GetChildren()) child.QueueFree();
    }

    private void DisposeNavigationFactory()
    {
        _navigationFactory?.Dispose();
        _navigationFactory = null;
    }

    private void ReloadRun() => GetTree().ReloadCurrentScene();

    public override void _ExitTree() => DisposeNavigationFactory();
}
