using Godot;

namespace Game1;

/// <summary>路障指挥车战斗的组合根节点；不改变 BossPhaseController 的阶段裁决权。</summary>
public partial class BossEncounterController : Node
{
    [Export] public Godot.Collections.Array<Vector2I> PhaseOneBarrierCells { get; set; } = new();
    [Export] public Godot.Collections.Array<Vector2I> PhaseTwoOpeningCells { get; set; } = new();
    [Export] public float BarrierIntervalSeconds { get; set; } = 3.2f;
    [Export] public float ThreatIntervalSeconds { get; set; } = 4.5f;
    [Export] public float ChargeIntervalSeconds { get; set; } = 1.4f;
    public RoadblockCommander Boss { get; private set; }
    private BarrierDeployment _barrier;
    private int _nextBarrierIndex;
    private bool _phaseOneActive;
    private Node2D _player = null!;
    private bool _phaseTwoActive;
    private TileTerrainAdapter _terrain;
    private BossGunEmplacement _gun;
    private BossSummonController _summons;

    /// <summary>将房间实例、目标实体和共享导航工厂交给第一阶段组件；不持有任何静态资源。</summary>
    public void Initialize(RoadblockCommander boss, Node2D room, RoomNavigationFactory navigation, int cellSize)
    {
        Boss = boss ?? throw new System.ArgumentNullException(nameof(boss));
        if (room is null) throw new System.ArgumentNullException(nameof(room));
        if (navigation is null) throw new System.ArgumentNullException(nameof(navigation));
        if (boss.Definition is null) throw new System.InvalidOperationException("Boss 必须先用 BossDefinition 初始化。");
        BarrierIntervalSeconds = boss.Definition.BarrierIntervalSeconds;
        ThreatIntervalSeconds = boss.Definition.ThreatIntervalSeconds;
        ChargeIntervalSeconds = boss.Definition.ChargeIntervalSeconds;

        _barrier = GetNode<BarrierDeployment>("BarrierDeployment");
        _player = room.GetNode<Node2D>("PlayerTank");
        _terrain = room.GetNodeOrNull<TileTerrainAdapter>("TileTerrainAdapter");
        _gun = GetNode<BossGunEmplacement>("BossGunEmplacement");
        _summons = GetNode<BossSummonController>("BossSummonController");
        _summons.Initialize(room, navigation.Provider);
        _barrier.Configure(
            room.GetNode<TileMapLayer>("Structure"),
            _player,
            cellSize,
            navigation.Rebuild);
        Boss.PhaseChanged += OnBossPhaseChanged;
        Boss.Defeated += StopEncounter;
        _phaseOneActive = true;
        RunPhaseOneBarrierLoop();
        RunPhaseOneThreatLoop();
    }

    public void StopEncounter()
    {
        _phaseOneActive = false;
        _phaseTwoActive = false;
        _gun?.Stop();
        _summons?.Stop();
    }

    private async void RunPhaseOneBarrierLoop()
    {
        while (_phaseOneActive && IsInsideTree())
        {
            await ToSignal(GetTree().CreateTimer(BarrierIntervalSeconds), SceneTreeTimer.SignalName.Timeout);
            if (!_phaseOneActive || !IsInsideTree() || PhaseOneBarrierCells.Count == 0) continue;
            Vector2I cell = PhaseOneBarrierCells[_nextBarrierIndex % PhaseOneBarrierCells.Count];
            _nextBarrierIndex++;
            _barrier.PreviewAndDeploy(cell);
        }
    }

    private void OnBossPhaseChanged(int phase)
    {
        if ((BossPhase)phase == BossPhase.PhaseTwo)
        {
            StopEncounter();
            OpenChargeLane();
            _phaseTwoActive = true;
            RunPhaseTwoChargeLoop();
        }
    }

    private void OpenChargeLane()
    {
        if (_terrain is null) return;
        foreach (Vector2I cell in PhaseTwoOpeningCells) _terrain.DestroyBrick(cell);
    }

    private async void RunPhaseOneThreatLoop()
    {
        while (_phaseOneActive && IsInsideTree())
        {
            await ToSignal(GetTree().CreateTimer(ThreatIntervalSeconds), SceneTreeTimer.SignalName.Timeout);
            if (!_phaseOneActive || !IsInsideTree()) continue;
            _gun.TriggerBurst();
            _summons.TrySummon();
        }
    }

    private async void RunPhaseTwoChargeLoop()
    {
        while (_phaseTwoActive && IsInsideTree())
        {
            await ToSignal(GetTree().CreateTimer(ChargeIntervalSeconds), SceneTreeTimer.SignalName.Timeout);
            if (_phaseTwoActive && IsInsideTree()) Boss.BeginCharge(_player.GlobalPosition);
        }
    }
}
