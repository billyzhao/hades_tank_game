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
    private static readonly BossDefinition RoadblockCommanderDefinition =
        GD.Load<BossDefinition>("res://resources/bosses/roadblock_commander.tres");
    private static readonly PackedScene BossValidationRoomScene =
        GD.Load<PackedScene>("res://scenes/rooms/mvp_boss_room.tscn");

    public RunState CurrentRun { get; private set; } = null!;

    private HealthComponent _playerHealth = null!;
    private RebootController _rebootController = null!;
    private BuildController _buildController = null!;
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
    private Label _eventLabel = null!;
    private Label _buildLabel = null!;
    private AcceptanceMenu _acceptanceMenu = null!;
    private WaveRewardPanel _waveRewardPanel = null!;
    private BossHudController _bossHud = null!;
    private RunResultScreen _runResultScreen = null!;
    private DebugOverlay _debugOverlay = null!;
    private readonly SaveService _saveService = new();
    private SaveData _saveData = null!;
    private ulong _runStartedAtMsec;
    private bool _resultShown;
    private bool _autoAdvanceAcceptanceWave;

    public override void _Ready()
    {
        CurrentRun = RunState.CreateNew(System.Environment.TickCount);
        _saveData = _saveService.LoadOrDefault();
        _runStartedAtMsec = Time.GetTicksMsec();
        ProtocolCatalog.Validate();
        BlockadeCityArena.Validate();

        _buildController = new BuildController(CurrentRun, ProtocolCatalog);
        _runController = new RunController(CurrentRun, _buildController);
        _runController.PhaseChanged += OnRunPhaseChanged;

        PauseCoordinator pauseCoordinator = new(GetTree());
        PauseController pauseController = new();
        pauseController.Configure(pauseCoordinator);
        AddChild(pauseController);

        BindHudNodes();
        CreateUiControllers();
        BindAcceptanceMenu();
        _buildController.SnapshotChanged += UpdateBuildHud;

        _debugOverlay = GD.Load<PackedScene>("res://scenes/ui/debug_overlay.tscn").Instantiate<DebugOverlay>();
        AddChild(_debugOverlay);
        _debugOverlay.Bind(CurrentRun, _saveData, () => _runController.Phase);

        _arenaController = new ArenaController(CurrentRun);
        _arenaController.StateChanged += OnArenaStateChanged;
        _arenaController.WaveRequested += StartWave;
        _arenaController.RewardRequested += ShowWaveReward;
        _arenaController.BossRequested += ShowBossPlaceholder;
        _arenaController.ArenaFailed += ShowRunFailure;

        Node2D arena = BlockadeCityArena.Scene.Instantiate<Node2D>();
        GetNode<Node>("ArenaHost").AddChild(arena);
        BindArena(arena);
        _arenaController.BeginArena(BlockadeCityArena);
        BeginArenaAfterIntro();
        UpdateHud();
        UpdateBuildHud();
    }

    private void BindHudNodes()
    {
        _armorLabel = GetNode<Label>("UI/Hud/ArmorLabel");
        _coreLabel = GetNode<Label>("UI/Hud/CoreLabel");
        _rebootLabel = GetNode<Label>("UI/Hud/RebootLabel");
        _enemyLabel = GetNode<Label>("UI/Hud/EnemyLabel");
        _arenaLabel = GetNode<Label>("UI/Hud/ArenaLabel");
        _waveLabel = GetNode<Label>("UI/Hud/WaveLabel");
        _eventLabel = GetNode<Label>("UI/EventLabel");
        _buildLabel = GetNode<Label>("UI/BuildLabel");
        _acceptanceMenu = GetNode<AcceptanceMenu>("UI/AcceptanceMenu");
    }

    private void CreateUiControllers()
    {
        CanvasLayer ui = GetNode<CanvasLayer>("UI");
        _waveRewardPanel = new WaveRewardPanel();
        ui.AddChild(_waveRewardPanel);
        _waveRewardPanel.RewardConfirmed += rewardId => _arenaController.ConfirmReward(rewardId);

        _bossHud = new BossHudController { Visible = false };
        ui.AddChild(_bossHud);
        _runResultScreen = new RunResultScreen();
        ui.AddChild(_runResultScreen);
        _runResultScreen.RetryRequested += ReloadRun;
        _runResultScreen.ReturnRequested += ReloadRun;
    }

    private async void BeginArenaAfterIntro()
    {
        await ToSignal(GetTree().CreateTimer(0.6d), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree() && _arenaController.State == ArenaState.Intro)
            _arenaController.OnIntroFinished();
    }

    private void BindArena(Node2D arena)
    {
        BindPlayerAndReboot(arena);
        _waveDirectorHost = arena.GetNode<Node>("WaveDirectorHost");
        _spawnEntrances = CollectEntrances(arena.GetNode<Node>("SpawnEntrances"));
        ReplaceNavigationFactory(arena, BlockadeCityArena.GridSize, BlockadeCityArena.CellSize);
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
        player.GetNode<WeaponController>("WeaponController").AttachBuild(_buildController);
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
        _waveDirector.EliteStateChanged += alive =>
        {
            if (alive) _eventLabel.Text = "精英在场：清场奖励被锁定";
        };
        _waveDirector.SpawnWindowEnded += () => _arenaController.OnWaveSpawnWindowEnded();
        _waveDirector.AllEnemiesCleared += () => _arenaController.OnAllEnemiesCleared();
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

        _waveRewardPanel.ShowReward(_arenaController.CurrentWave, kind);
        _eventLabel.Text = $"第 {_arenaController.CurrentWave} 波清场：确认{RewardKindName(kind)}后继续";
    }

    private void OnArenaStateChanged(ArenaState state)
    {
        _arenaLabel.Text = $"竞技场  {CurrentRun.ArenaIndex + 1}/5";
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
                _eventLabel.Text = "五波完成：Boss 即将进入（路障指挥车接入属于 Alpha 02H）";
                _waveLabel.Text = "Boss  占位已解锁";
                break;
        }
        UpdateHud();
    }

    private void ShowBossPlaceholder()
    {
        _acceptanceMenu.SetStatus("第 5 波与稀有奖励已完成；当前停在 BossIntro，占位 Boss 将在 Alpha 02H 接入。");
    }

    private void BindAcceptanceMenu()
    {
        _acceptanceMenu.DamageRequested += amount =>
        {
            _playerHealth.ApplyDamage(new DamageContext(amount));
            _acceptanceMenu.SetStatus($"已请求装甲伤害 {amount}，当前 {CurrentRun.PlayerArmor}/{CurrentRun.MaximumArmor}");
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
        _acceptanceMenu.BossRequested += EnterBossValidationRoom;
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
        if (phase != RunPhase.Failed) return;
        _arenaController?.OnPlayerRunFailed();
        ShowRunFailure();
    }

    private void UpdateHud()
    {
        if (_playerHealth is null) return;
        _armorLabel.Text = $"装甲  {_playerHealth.Armor}/{_playerHealth.MaximumArmor}";
        _coreLabel.Text = "核心  移动核心";
        _rebootLabel.Text = $"重启  {CurrentRun.RebootsRemaining}";
        _arenaLabel.Text = $"竞技场  {CurrentRun.ArenaIndex + 1}/5";
        if (_waveDirector is null)
            _waveLabel.Text = $"波次  {_arenaController?.CurrentWave ?? 1}/5  准备中";
    }

    private void UpdateBuildHud()
    {
        if (CurrentRun.SelectedProtocolIds.Count == 0)
        {
            _buildLabel.Text = "构筑：Alpha 02E 接入协议构筑；当前验证五波节奏";
            return;
        }
        string names = string.Join("  |  ", CurrentRun.SelectedProtocolIds.Select(id => ProtocolCatalog.GetProtocol(id).DisplayName));
        _buildLabel.Text = $"构筑：{names}";
    }

    private void EnterBossValidationRoom()
    {
        _bossHud.Unbind();
        DisposeNavigationFactory();
        _waveRewardPanel.Hide();
        ClearArenaHost();
        Node2D room = BossValidationRoomScene.Instantiate<Node2D>();
        GetNode<Node>("ArenaHost").AddChild(room);
        BindPlayerAndReboot(room);

        RoadblockCommander boss = room.GetNode<RoadblockCommander>("RoadblockCommander");
        boss.Initialize(RoadblockCommanderDefinition);
        ReplaceNavigationFactory(room, RoadblockCommanderDefinition.GridSize, RoadblockCommanderDefinition.CellSize);
        room.GetNode<BossEncounterController>("BossEncounterController")
            .Initialize(boss, room, _navigationFactory, RoadblockCommanderDefinition.CellSize);
        _bossHud.Bind(boss, RoadblockCommanderDefinition);
        boss.Defeated += ShowBossResult;
        _eventLabel.Text = "独立 Boss 验收：路障指挥车只锁定玩家坦克";
        _waveLabel.Text = "阶段  1/2";
        UpdateHud();
    }

    private void ShowBossResult()
    {
        if (_resultShown) return;
        _resultShown = true;
        ClearCombatActors();
        CurrentRun.RestoreArmorForNextArena();
        _playerHealth.SetArmor(CurrentRun.PlayerArmor);
        RunResultSnapshot snapshot = CreateResultSnapshot();
        SaveLastRun(snapshot, "victory");
        _runResultScreen.ShowResult(snapshot, true);
        _eventLabel.Text = "路障指挥车已击败：装甲已全修";
    }

    private void ShowRunFailure()
    {
        if (_resultShown) return;
        _resultShown = true;
        ClearCombatActors();
        _waveRewardPanel.Hide();
        RunResultSnapshot snapshot = CreateResultSnapshot();
        SaveLastRun(snapshot, "failed");
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
        return new RunResultSnapshot(CurrentRun.Seed, CurrentRun.SelectedProtocolIds, string.Empty,
            CurrentRun.ArenaIndex, CurrentRun.WaveIndex, 1, TimeSpan.FromMilliseconds(elapsedMsec));
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
        BehaviorId.Patrol => "巡逻坦克（追击玩家）",
        BehaviorId.Assault => "突击车（快速压迫玩家）",
        BehaviorId.Siege => "重炮车（远程锁定玩家）",
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
