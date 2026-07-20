using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

public partial class AppRoot : Node
{
    private static readonly RoomDefinition[] RoomDefinitions =
    [
        GD.Load<RoomDefinition>("res://resources/rooms/mvp_combat_room.tres"),
        GD.Load<RoomDefinition>("res://resources/rooms/industrial_flank_room.tres")
    ];
    private static readonly ContentCatalog ProtocolCatalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
    private static readonly BossDefinition RoadblockCommanderDefinition = GD.Load<BossDefinition>("res://resources/bosses/roadblock_commander.tres");
    private static readonly PackedScene BossValidationRoomScene = GD.Load<PackedScene>("res://scenes/rooms/mvp_boss_room.tscn");

    public RunState CurrentRun { get; private set; } = null!;
    private HealthComponent _playerHealth = null!;
    private RebootController _rebootController = null!;
    private Label _armorLabel = null!;
    private Label _coreLabel = null!;
    private Label _rebootLabel = null!;
    private Label _enemyLabel = null!;
    private Label _roomLabel = null!;
    private Label _waveLabel = null!;
    private Label _eventLabel = null!;
    private Label _buildLabel = null!;
    private BuildController _buildController = null!;
    private RunController _runController = null!;
    private RewardPanel _rewardPanel = null!;
    private BossHudController _bossHud = null!;
    private RunResultScreen _runResultScreen = null!;
    private AcceptanceMenu _acceptanceMenu = null!;
    private EnemyDirector _director = null!;
    private RoomNavigationFactory _navigationFactory;
    private bool _wavesStarted;
    private ulong _runStartedAtMsec;
    private readonly SaveService _saveService = new();
    private SaveData _saveData = null!;
    private DebugOverlay _debugOverlay = null!;
    private bool _resultShown;

    public override void _Ready()
    {
        CurrentRun = RunState.CreateNew(System.Environment.TickCount);
        _saveData = _saveService.LoadOrDefault();
        _runStartedAtMsec = Time.GetTicksMsec();
        ProtocolCatalog.Validate();
        _buildController = new BuildController(CurrentRun, ProtocolCatalog);
        _runController = new RunController(CurrentRun, _buildController, new RewardGenerator());
        PauseCoordinator pauseCoordinator = new(GetTree());
        PauseController pauseController = new();
        pauseController.Configure(pauseCoordinator);
        AddChild(pauseController);

        _armorLabel = GetNode<Label>("UI/Hud/ArmorLabel");
        _coreLabel = GetNode<Label>("UI/Hud/CoreLabel");
        _rebootLabel = GetNode<Label>("UI/Hud/RebootLabel");
        _enemyLabel = GetNode<Label>("UI/Hud/EnemyLabel");
        _roomLabel = GetNode<Label>("UI/Hud/RoomLabel");
        _waveLabel = GetNode<Label>("UI/Hud/WaveLabel");
        _eventLabel = GetNode<Label>("UI/EventLabel");
        _buildLabel = GetNode<Label>("UI/BuildLabel");
        _acceptanceMenu = GetNode<AcceptanceMenu>("UI/AcceptanceMenu");
        BindAcceptanceMenu();

        _debugOverlay = GD.Load<PackedScene>("res://scenes/ui/debug_overlay.tscn").Instantiate<DebugOverlay>();
        AddChild(_debugOverlay);
        _debugOverlay.Bind(CurrentRun, _saveData, () => _runController.Phase);

        _buildController.SnapshotChanged += UpdateBuildHud;
        _buildController.RoomCleared += ApplyClearRewards;
        _rewardPanel = new RewardPanel { Position = new Vector2(24, 88) };
        GetNode<CanvasLayer>("UI").AddChild(_rewardPanel);
        _rewardPanel.ProtocolChosen += protocolId => _runController.ChooseProtocol(protocolId);
        _bossHud = new BossHudController { Visible = false };
        GetNode<CanvasLayer>("UI").AddChild(_bossHud);
        _runResultScreen = new RunResultScreen();
        GetNode<CanvasLayer>("UI").AddChild(_runResultScreen);
        _runResultScreen.RetryRequested += ReloadRun;
        _runResultScreen.ReturnRequested += ReloadRun;
        _runController.PhaseChanged += OnRunPhaseChanged;

        RoomDefinition firstRoom = RoomDefinitions[0];
        firstRoom.Validate();
        Node2D room = firstRoom.Scene.Instantiate<Node2D>();
        GetNode<Node>("RoomHost").AddChild(room);
        BindCombatRoom(room, firstRoom);
        _runController.BeginRoom();
        UpdateHud();
        UpdateBuildHud();
    }

    public override void _Process(double delta)
    {
        if (_runController is null) return;
        _runController.Advance(delta);
        if (_runController.Phase == RoomPhase.Combat && !_wavesStarted)
        {
            _director.StartWaves();
            _wavesStarted = true;
        }
        UpdateHud();
    }

    private void BindCombatRoom(Node2D room, RoomDefinition definition)
    {
        BindPlayerAndReboot(room);
        _director = room.GetNode<EnemyDirector>("EnemyDirector");
        _director.Configure(definition, ReplaceNavigationFactory(room, definition));
        _director.AllWavesFinished += () => _runController.OnCombatCleared();
        _director.EnemyCountChanged += count => _enemyLabel.Text = $"敌军  {count}";
        _director.WaveChanged += (current, total) =>
        {
            _waveLabel.Text = $"波次  {current}/{total}";
            _eventLabel.Text = $"第 {current} 波来袭：敌军只锁定玩家坦克";
        };
        _director.EnemySpawned += (behavior, wave) =>
            _eventLabel.Text = $"第 {wave} 波增援：{BehaviorName((BehaviorId)behavior)} 已进入战场";
        _director.AllWavesFinished += () => _eventLabel.Text = "波次清场：房间已完成";
        _waveLabel.Text = $"波次  {_director.CurrentWave}/{_director.TotalWaves}";
        RoomController roomController = room.GetNode<RoomController>("RoomController");
        roomController.RoomCleared += () => _roomLabel.Text = "房间  已清场";
    }

    private void BindPlayerAndReboot(Node2D room)
    {
        PlayerTank player = room.GetNode<PlayerTank>("PlayerTank");
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

        _rebootController = room.GetNode<RebootController>("RebootController");
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

    private void BindAcceptanceMenu()
    {
        _acceptanceMenu.DamageRequested += amount =>
        {
            _playerHealth.ApplyDamage(new DamageContext(amount));
            _acceptanceMenu.SetStatus($"已请求装甲伤害 {amount}，当前 {CurrentRun.PlayerArmor}/{CurrentRun.MaximumArmor}");
        };
        _acceptanceMenu.DefeatRequested += () =>
        {
            _playerHealth.ApplyDamage(new DamageContext(System.Math.Max(1, _playerHealth.Armor)));
            _acceptanceMenu.SetStatus("已请求坦克报废，请观察重构或失败流程");
        };
        _acceptanceMenu.BossRequested += EnterBossValidationRoom;
        _acceptanceMenu.RestartRequested += ReloadRun;
    }

    private void OnRunPhaseChanged(RoomPhase phase)
    {
        _roomLabel.Text = $"房间  {phase}";
        if (phase == RoomPhase.Reward) _rewardPanel.ShowOffer(_runController.CurrentOffer, ProtocolCatalog);
        if (phase == RoomPhase.Exiting) RestartCombatRoom();
        if (phase == RoomPhase.Failed) ShowRunFailure();
    }

    private void UpdateHud()
    {
        if (_playerHealth is null) return;
        _armorLabel.Text = $"装甲  {_playerHealth.Armor}/{_playerHealth.MaximumArmor}";
        _coreLabel.Text = "核心  移动核心";
        _rebootLabel.Text = $"重启  {CurrentRun.RebootsRemaining}";
    }

    private void ApplyClearRewards()
    {
        int armorRepair = Mathf.RoundToInt(_buildController.EvaluateStat(StatId.ArmorMax, 0f));
        if (armorRepair <= 0) return;
        _playerHealth.RestoreArmor(armorRepair);
        _eventLabel.Text = $"清场维修：装甲 +{armorRepair}";
    }

    private void UpdateBuildHud()
    {
        if (CurrentRun.SelectedProtocolIds.Count == 0)
        {
            _buildLabel.Text = "构筑：暂无协议";
            return;
        }
        string names = string.Join("  |  ", CurrentRun.SelectedProtocolIds.Select(id => ProtocolCatalog.GetProtocol(id).DisplayName));
        _buildLabel.Text = $"构筑：{names}";
    }

    private void RestartCombatRoom()
    {
        _bossHud.Unbind();
        DisposeNavigationFactory();
        Node roomHost = GetNode<Node>("RoomHost");
        foreach (Node child in roomHost.GetChildren()) child.QueueFree();
        RoomDefinition definition = RoomDefinitions[CurrentRun.RoomIndex % RoomDefinitions.Length];
        definition.Validate();
        Node2D room = definition.Scene.Instantiate<Node2D>();
        roomHost.AddChild(room);
        BindCombatRoom(room, definition);
        _wavesStarted = false;
        _runController.BeginRoom();
    }

    private void EnterBossValidationRoom()
    {
        _bossHud.Unbind();
        DisposeNavigationFactory();
        _rewardPanel.Hide();
        _wavesStarted = true;
        Node roomHost = GetNode<Node>("RoomHost");
        foreach (Node child in roomHost.GetChildren()) child.QueueFree();
        Node2D room = BossValidationRoomScene.Instantiate<Node2D>();
        roomHost.AddChild(room);
        BindPlayerAndReboot(room);

        RoadblockCommander boss = room.GetNode<RoadblockCommander>("RoadblockCommander");
        boss.Initialize(RoadblockCommanderDefinition);
        _navigationFactory = new RoomNavigationFactory(room, RoadblockCommanderDefinition.GridSize, RoadblockCommanderDefinition.CellSize);
        room.GetNode<BossEncounterController>("BossEncounterController")
            .Initialize(boss, room, _navigationFactory, RoadblockCommanderDefinition.CellSize);
        _bossHud.Bind(boss, RoadblockCommanderDefinition);
        boss.Defeated += ShowBossResult;
        _eventLabel.Text = "Boss 验收：路障指挥车只锁定玩家坦克";
        _roomLabel.Text = "房间  Boss 验收";
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
        _rewardPanel.Hide();
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
            System.Math.Clamp(CurrentRun.RoomIndex, 0, 4), 0, 1, System.TimeSpan.FromMilliseconds(elapsedMsec));
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

    private IEnemyPathProvider ReplaceNavigationFactory(Node2D room, RoomDefinition definition)
    {
        DisposeNavigationFactory();
        _navigationFactory = new RoomNavigationFactory(room, definition.GridSize, definition.CellSize);
        return _navigationFactory.Provider;
    }

    private void DisposeNavigationFactory()
    {
        _navigationFactory?.Dispose();
        _navigationFactory = null;
    }

    private void ReloadRun() => GetTree().ReloadCurrentScene();
    public override void _ExitTree() => DisposeNavigationFactory();
}
