using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>
/// 限时增援导演：只负责生成、刷新计时与存活计数。
/// 奖励、下一波和 Boss 迁移完全由 ArenaController 决定。
/// </summary>
public partial class WaveDirector : Node
{
    private static readonly PackedScene EnemyScene =
        GD.Load<PackedScene>("res://scenes/actors/enemy_tank.tscn");
    private static readonly RectangleShape2D SpawnClearanceShape = new() { Size = new Vector2(16f, 16f) };

    private WaveDefinition _definition;
    private IReadOnlyList<SpawnEntrance> _entrances = Array.Empty<SpawnEntrance>();
    private IEnemyPathProvider _pathProvider;
    private int _entranceOffset;
    private int _spawnOrdinal;
    private float _spawnCooldown;
    private bool _configured;
    private bool _started;
    private bool _clearEmitted;
    private bool _eliteSpawned;
    private bool _cancelPendingSpawnsForAcceptance;
    private int _pendingSpawns;
    private readonly HashSet<EnemyTank> _aliveEnemies = new();

    public int AliveEnemyCount { get; private set; }
    public double RemainingSpawnSeconds { get; private set; }
    public bool IsSpawning { get; private set; }
    public bool EliteAlive { get; private set; }

    public event Action<double> TimeChanged;
    public event Action<int> EnemyCountChanged;
    public event Action<BehaviorId, bool> EnemySpawned;
    public event Action<Vector2, bool> EnemyDefeated;
    public event Action<bool> EliteStateChanged;
    public event Action SpawnWindowEnded;
    public event Action AllEnemiesCleared;

    public override void _Ready() => SetPhysicsProcess(false);

    public void Configure(
        WaveDefinition definition,
        IReadOnlyList<SpawnEntrance> entrances,
        int runSeed,
        int arenaIndex,
        int waveIndex,
        IEnemyPathProvider pathProvider)
    {
        if (_started) throw new InvalidOperationException("已经启动的 WaveDirector 不能重新配置。");
        definition = definition ?? throw new ArgumentNullException(nameof(definition));
        definition.Validate();
        if (entrances is null || entrances.Count == 0)
            throw new ArgumentException("WaveDirector 至少需要一个手工入口。", nameof(entrances));
        if (arenaIndex is < 0 or > 4) throw new ArgumentOutOfRangeException(nameof(arenaIndex));
        if (waveIndex is < 0 or > 4) throw new ArgumentOutOfRangeException(nameof(waveIndex));
        pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));

        HashSet<string> entranceIds = new(StringComparer.Ordinal);
        foreach (SpawnEntrance entrance in entrances)
        {
            entrance.Validate();
            if (!entranceIds.Add(entrance.Id))
                throw new ArgumentException($"检测到重复入口 Id：'{entrance.Id}'。", nameof(entrances));
        }

        _definition = definition;
        _entrances = entrances.ToArray();
        _pathProvider = pathProvider;
        uint stableOffset = unchecked((uint)(runSeed * 397 ^ arenaIndex * 31 ^ waveIndex * 17));
        _entranceOffset = (int)(stableOffset % (uint)_entrances.Count);
        _configured = true;
    }

    public void StartWave()
    {
        if (!_configured) throw new InvalidOperationException("WaveDirector 必须先完成配置。");
        if (_started) throw new InvalidOperationException("同一个 WaveDirector 只能启动一次。");

        _started = true;
        _clearEmitted = false;
        _eliteSpawned = false;
        _cancelPendingSpawnsForAcceptance = false;
        _aliveEnemies.Clear();
        EliteAlive = false;
        AliveEnemyCount = 0;
        RemainingSpawnSeconds = _definition.SpawnDurationSeconds;
        _spawnCooldown = 0f;
        IsSpawning = true;
        SetPhysicsProcess(true);
        TimeChanged?.Invoke(RemainingSpawnSeconds);
        EnemyCountChanged?.Invoke(AliveEnemyCount);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsSpawning) return;

        RemainingSpawnSeconds = Math.Max(0d, RemainingSpawnSeconds - delta);
        TimeChanged?.Invoke(RemainingSpawnSeconds);
        _spawnCooldown -= (float)delta;
        if (_spawnCooldown <= 0f && AliveEnemyCount + _pendingSpawns < _definition.MaximumAliveEnemies)
        {
            SpawnEnemy();
            _spawnCooldown = _definition.SpawnIntervalSeconds;
        }

        if (RemainingSpawnSeconds <= 0d) StopSpawning();
    }

    public void StopSpawning()
    {
        if (!IsSpawning) return;
        IsSpawning = false;
        RemainingSpawnSeconds = 0d;
        SetPhysicsProcess(false);
        TimeChanged?.Invoke(0d);
        SpawnWindowEnded?.Invoke();
        TryEmitAllEnemiesCleared();
    }

    /// <summary>
    /// 仅供 Debug 验收入口调用：击毁本导演生成且仍存活的敌军。
    /// 正常刷新、清场和奖励状态机不会调用本方法。
    /// </summary>
    public void ClearAliveEnemiesForAcceptance()
    {
        // “结束刷新”后的验收清场代表立即结束本轮，必须同时撤销仍处于预警阶段的出生任务。
        // 刷新仍在进行时只清当前敌军，保留导演继续补兵的既有验收语义。
        if (!IsSpawning && _pendingSpawns > 0)
        {
            _cancelPendingSpawnsForAcceptance = true;
            _pendingSpawns = 0;
        }

        foreach (EnemyTank enemy in _aliveEnemies.Where(IsInstanceValid).ToArray())
            enemy.ApplyDamage(new DamageContext(Math.Max(1, enemy.Armor)));
        TryEmitAllEnemiesCleared();
    }

    private void SpawnEnemy()
    {
        bool isElite = _definition.IncludesElite && !_eliteSpawned;
        BehaviorId behavior = isElite
            ? _definition.Behaviors[^1]
            : _definition.Behaviors[_spawnOrdinal % _definition.Behaviors.Count];
        SpawnEntrance? entrance = FindEntrance();
        if (entrance is null)
        {
            // 出生点未通过导航可达性校验时宁可跳过本次增援，
            // 也不能把敌军生成在不可离开的地形或边界内。
            return;
        }

        // 预警期间也要占用精英资格，避免同一波连续排入多个精英。
        if (isElite) _eliteSpawned = true;
        _pendingSpawns++;
        ShowSpawnWarning(entrance.Value);
        SpawnEnemyAfterWarning(entrance.Value, behavior, isElite);
    }

    private async void SpawnEnemyAfterWarning(SpawnEntrance entrance, BehaviorId behavior, bool isElite)
    {
        await ToSignal(GetTree().CreateTimer(entrance.WarningSeconds), SceneTreeTimer.SignalName.Timeout);
        _pendingSpawns = Math.Max(0, _pendingSpawns - 1);
        if (_cancelPendingSpawnsForAcceptance)
        {
            if (isElite) _eliteSpawned = false;
            return;
        }
        // 入口预警开始时已占用波次预算；即使刷新计时在预警期间结束，
        // 该单位仍必须入场并作为清场残敌，不能被静默取消。
        if (!IsInsideTree())
        {
            if (isElite) _eliteSpawned = false;
            return;
        }
        if (!IsSpawnPositionClear(entrance.Position))
        {
            if (isElite) _eliteSpawned = false;
            return;
        }

        EnemyTank enemy = EnemyScene.Instantiate<EnemyTank>();
        enemy.Name = isElite ? "ElitePlaceholder" : $"WaveEnemy{_spawnOrdinal + 1}";
        enemy.Behavior = behavior;
        enemy.SetPathProvider(_pathProvider);
        enemy.GlobalPosition = entrance.Position;
        if (isElite)
        {
            _eliteSpawned = true;
            EliteAlive = true;
            enemy.IsEliteVisual = true;
            enemy.Scale = Vector2.One * 1.25f;
            enemy.AddToGroup("elite_placeholder");
            EliteStateChanged?.Invoke(true);
        }

        _aliveEnemies.Add(enemy);
        enemy.Destroyed += () => OnEnemyDestroyed(enemy, isElite);
        GetParent().AddChild(enemy);
        _spawnOrdinal++;
        AliveEnemyCount = _aliveEnemies.Count;
        EnemyCountChanged?.Invoke(AliveEnemyCount);
        EnemySpawned?.Invoke(behavior, isElite);
    }

    private void ShowSpawnWarning(SpawnEntrance entrance)
    {
        SpawnWarning warning = new();
        GetParent().AddChild(warning);
        warning.GlobalPosition = entrance.Position;
        warning.Begin(entrance.WarningSeconds, EnemyVisualPalette.GetRoleTint(BehaviorId.Scout));
    }

    private SpawnEntrance? FindEntrance()
    {
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        IEnumerable<SpawnEntrance> ordered = _entrances
            .Select((entrance, index) => (entrance, index))
            .OrderBy(tuple => player is null || tuple.entrance.Position.DistanceTo(player.GlobalPosition) >= _definition.MinimumPlayerDistance ? 0 : 1)
            .ThenBy(tuple => (tuple.index - _entranceOffset - _spawnOrdinal + _entrances.Count * 2) % _entrances.Count)
            .Select(tuple => tuple.entrance);

        foreach (SpawnEntrance candidate in ordered)
        {
            if (!IsSpawnPositionClear(candidate.Position)) continue;
            if (player is null || _pathProvider.GetWorldPath(candidate.Position, player.GlobalPosition).Count >= 2)
                return candidate;
        }

        return null;
    }

    /// <summary>按敌军实际 16×16 碰撞体查询场景物理层，防止出生点与墙体或边界重叠。</summary>
    private bool IsSpawnPositionClear(Vector2 position)
    {
        PhysicsShapeQueryParameters2D query = new()
        {
            Shape = SpawnClearanceShape,
            Transform = new Transform2D(0f, position),
            CollisionMask = 1,
            CollideWithBodies = true,
            CollideWithAreas = false
        };
        return GetViewport().World2D.DirectSpaceState.IntersectShape(query, 1).Count == 0;
    }

    private void OnEnemyDestroyed(EnemyTank enemy, bool wasElite)
    {
        if (!_aliveEnemies.Remove(enemy)) return;
        EnemyDefeated?.Invoke(enemy.GlobalPosition, wasElite);
        AliveEnemyCount = _aliveEnemies.Count;
        if (wasElite)
        {
            EliteAlive = false;
            EliteStateChanged?.Invoke(false);
        }
        EnemyCountChanged?.Invoke(AliveEnemyCount);
        TryEmitAllEnemiesCleared();
    }

    private void TryEmitAllEnemiesCleared()
    {
        if (IsSpawning || AliveEnemyCount != 0 || _pendingSpawns != 0 || _clearEmitted) return;
        _clearEmitted = true;
        AllEnemiesCleared?.Invoke();
    }
}
