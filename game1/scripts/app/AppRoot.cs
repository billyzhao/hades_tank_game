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

    public RunState CurrentRun { get; private set; } = null!;
    private HealthComponent _playerHealth = null!;
    private RelayStation _relay = null!;
    private Label _armorLabel = null!;
    private Label _relayLabel = null!;
    private Label _rebootLabel = null!;
    private Label _enemyLabel = null!;
    private Label _roomLabel = null!;
    private Label _waveLabel = null!;
    private Label _eventLabel = null!;
    private Label _buildLabel = null!;
    private BuildController _buildController = null!;
    private RunController _runController = null!;
    private RewardPanel _rewardPanel = null!;
    private EnemyDirector _director = null!;
    private bool _wavesStarted;

    public override void _Ready()
    {
        CurrentRun = RunState.CreateNew(System.Environment.TickCount);
        ProtocolCatalog.Validate();
        _buildController = new BuildController(CurrentRun, ProtocolCatalog);
        _runController = new RunController(CurrentRun, _buildController, new RewardGenerator());
        Node roomHost = GetNode<Node>("RoomHost");
        RoomDefinition firstRoom = RoomDefinitions[0];
        firstRoom.Validate();
        Node2D room = firstRoom.Scene.Instantiate<Node2D>();
        roomHost.AddChild(room);
        _relay = room.GetNode<RelayStation>("RelayStation");
        _relay.Initialize(CurrentRun);
        _relay.AttachBuild(_buildController);
        _relay.Destroyed += () => _runController.OnRelayDestroyed();
        _relay.ShieldIntercepted += prevented => _eventLabel.Text = $"中继拦截护盾：抵消 {prevented} 点伤害";
        _playerHealth = room.GetNode<PlayerTank>("PlayerTank").GetNode<HealthComponent>("HealthComponent");
        room.GetNode<PlayerTank>("PlayerTank").GetNode<WeaponController>("WeaponController").AttachBuild(_buildController);
        room.GetNode<PlayerTank>("PlayerTank").GetNode<DashComponent>("DashComponent").AttachBuild(_buildController);
        _armorLabel = GetNode<Label>("UI/Hud/ArmorLabel");
        _relayLabel = GetNode<Label>("UI/Hud/RelayLabel");
        _rebootLabel = GetNode<Label>("UI/Hud/RebootLabel");
        _enemyLabel = GetNode<Label>("UI/Hud/EnemyLabel");
        _roomLabel = GetNode<Label>("UI/Hud/RoomLabel");
        _waveLabel = GetNode<Label>("UI/Hud/WaveLabel");
        _eventLabel = GetNode<Label>("UI/EventLabel");
        _buildLabel = GetNode<Label>("UI/BuildLabel");
        _buildController.SnapshotChanged += UpdateBuildHud;
        _buildController.RoomCleared += ApplyClearRewards;
        _rewardPanel = new RewardPanel { Position = new Vector2(24, 88) };
        GetNode<CanvasLayer>("UI").AddChild(_rewardPanel);
        _rewardPanel.ProtocolChosen += protocolId => _runController.ChooseProtocol(protocolId);
        _runController.PhaseChanged += phase =>
        {
            _roomLabel.Text = $"房间  {phase}";
            _eventLabel.Text = phase.ToString();
            if (phase == RoomPhase.Reward) _rewardPanel.ShowOffer(_runController.CurrentOffer, ProtocolCatalog);
            if (phase == RoomPhase.Exiting) RestartCombatRoom();
        };
        _runController.BeginRoom();
        _playerHealth.Depleted += () => _eventLabel.Text = "坦克报废：尝试战场重启";
        RebootController reboot = room.GetNode<RebootController>("RebootController");
        reboot.Rebooted += () => _eventLabel.Text = "重启成功：已在中继站旁恢复 50 装甲";
        reboot.RunFailed += () => _eventLabel.Text = "本局失败：没有剩余战场重启次数";
        EnemyDirector director = room.GetNode<EnemyDirector>("EnemyDirector");
        director.Configure(firstRoom, CreatePathProvider(room, firstRoom));
        _director = director;
        director.AllWavesFinished += () => _runController.OnCombatCleared();
        director.EnemyCountChanged += count => _enemyLabel.Text = $"敌军  {count}";
        director.WaveChanged += (current, total) =>
        {
            _waveLabel.Text = $"波次  {current}/{total}";
            _eventLabel.Text = $"第 {current} 波来袭：留意敌军出生闪烁与攻击预警";
        };
        director.EnemySpawned += (behavior, wave) => _eventLabel.Text = $"第 {wave} 波增援：{BehaviorName((BehaviorId)behavior)} 已进入战场";
        director.AllWavesFinished += () => _eventLabel.Text = "波次清场：房间已完成";
        _waveLabel.Text = $"波次  {director.CurrentWave}/{director.TotalWaves}";
        RoomController roomController = room.GetNode<RoomController>("RoomController");
        roomController.RoomCleared += () => _roomLabel.Text = "房间  已清场";
        roomController.RoomFailed += () => _roomLabel.Text = "房间  失败";
        UpdateHud();
        UpdateBuildHud();
    }

    public override void _Process(double delta)
    {
        // _Ready 中的资源校验若失败，Godot 仍可能继续派发帧回调；避免把首个启动错误淹没为重复空引用。
        if (_runController is null) return;
        _runController.Advance(delta);
        if (_runController.Phase == RoomPhase.Combat && !_wavesStarted)
        {
            _director.StartWaves();
            _wavesStarted = true;
        }
        UpdateHud();
        if (Input.IsActionJustPressed("debug_damage_player"))
        {
            _eventLabel.Text = "调试：坦克受到 100 点伤害";
            _playerHealth.ApplyDamage(new DamageContext(100));
        }
        else if (Input.IsActionJustPressed("debug_damage_relay"))
        {
            _relay.ApplyDamage(new DamageContext(25));
            _eventLabel.Text = CurrentRun.RelayIntegrity == 0 ? "中继站毁坏：本局失败" : "调试：中继站受到 25 点伤害";
        }
    }

    private void UpdateHud()
    {
        _armorLabel.Text = $"装甲  {_playerHealth.Armor}/{_playerHealth.MaximumArmor}";
        _relayLabel.Text = $"中继站  {CurrentRun.RelayIntegrity}/100";
        _rebootLabel.Text = $"重启  {CurrentRun.RebootsRemaining}";
    }

    /// <summary>清场维修从统一属性管线读取数值；选择协议后，下一次清场才结算，避免奖励当场反向生效。</summary>
    private void ApplyClearRewards()
    {
        int armorRepair = Mathf.RoundToInt(_buildController.EvaluateStat(StatId.ArmorMax, 0f));
        int relayRepair = Mathf.RoundToInt(_buildController.EvaluateStat(StatId.RelayRepair, 0f));
        if (armorRepair > 0) _playerHealth.RestoreArmor(armorRepair);
        if (relayRepair > 0) CurrentRun.RestoreRelayIntegrity(relayRepair, _relay.MaximumIntegrity);
        if (armorRepair > 0 || relayRepair > 0)
        {
            _eventLabel.Text = $"清场维修：装甲 +{armorRepair}，中继站 +{relayRepair}";
        }
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

    /// <summary>供 RebootController 调用的唯一失败入口，避免组件直接改写 RunState。</summary>
    public bool TryHandleTankDefeat() => _runController.OnTankDefeated();

    private void RestartCombatRoom()
    {
        Node roomHost = GetNode<Node>("RoomHost");
        foreach (Node child in roomHost.GetChildren()) child.QueueFree();
        RoomDefinition definition = RoomDefinitions[CurrentRun.RoomIndex % RoomDefinitions.Length];
        definition.Validate();
        Node2D room = definition.Scene.Instantiate<Node2D>();
        roomHost.AddChild(room);
        _relay = room.GetNode<RelayStation>("RelayStation");
        _relay.Initialize(CurrentRun);
        _relay.AttachBuild(_buildController);
        _relay.Destroyed += () => _runController.OnRelayDestroyed();
        _relay.ShieldIntercepted += prevented => _eventLabel.Text = $"中继拦截护盾：抵消 {prevented} 点伤害";
        PlayerTank player = room.GetNode<PlayerTank>("PlayerTank");
        _playerHealth = player.GetNode<HealthComponent>("HealthComponent");
        player.GetNode<WeaponController>("WeaponController").AttachBuild(_buildController);
        player.GetNode<DashComponent>("DashComponent").AttachBuild(_buildController);
        _director = room.GetNode<EnemyDirector>("EnemyDirector");
        _director.Configure(definition, CreatePathProvider(room, definition));
        _director.EnemyCountChanged += count => _enemyLabel.Text = $"敌军  {count}";
        _director.AllWavesFinished += () => _runController.OnCombatCleared();
        _wavesStarted = false;
        _runController.BeginRoom();
    }

    private static string BehaviorName(BehaviorId behavior) => behavior switch
    {
        BehaviorId.Patrol => "巡逻坦克（追击玩家）",
        BehaviorId.Assault => "突击车（快速压迫）",
        BehaviorId.Siege => "攻城炮车（攻击中继站）",
        _ => "未知单位"
    };

    private static IEnemyPathProvider CreatePathProvider(Node2D room, RoomDefinition definition)
    {
        NavigationGrid grid = new(definition.GridSize);
        TileTerrainAdapter terrain = room.GetNodeOrNull<TileTerrainAdapter>("TileTerrainAdapter");
        void RebuildNavigation()
        {
            HashSet<Vector2I> blockedCells = new();
            TileMapLayer structure = room.GetNodeOrNull<TileMapLayer>("Structure");
            if (structure is not null)
            {
                foreach (Vector2I cell in structure.GetUsedCells()) blockedCells.Add(cell);
            }

            if (terrain is not null)
            {
                foreach (Vector2I cell in terrain.BlockedNavigationCells) blockedCells.Add(cell);
            }

            grid.Rebuild(blockedCells);
        }

        RebuildNavigation();
        if (terrain is not null)
        {
            terrain.BrickDestroyed += _ => RebuildNavigation();
        }
        return new RoomPathProvider(grid, definition.CellSize);
    }
}
