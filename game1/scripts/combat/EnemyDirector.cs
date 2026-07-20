using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1;

/// <summary>
/// 房间级敌军导演：按已验证的威胁波次逐个生成，只有当前波所有单位被消灭才会推进。
/// 生成点会避开玩家，避免敌人贴脸出现而失去预警价值。
/// </summary>
public partial class EnemyDirector : Node
{
    private const float SpawnIntervalSeconds = 0.6f;
    private const float SafeSpawnDistance = 72f;
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/actors/enemy_tank.tscn");
    [Signal] public delegate void EnemyCountChangedEventHandler(int count);
    [Signal] public delegate void EnemySpawnedEventHandler(int behavior, int waveIndex);
    [Signal] public delegate void WaveChangedEventHandler(int currentWave, int totalWaves);
    [Signal] public delegate void AllWavesFinishedEventHandler();

    private IReadOnlyList<IReadOnlyList<BehaviorId>> _waves = null!;
    private int _waveIndex = -1;
    private int _spawnIndex;
    private int _alive;
    private bool _finished;
    private IEnemyPathProvider _pathProvider;
    private IReadOnlyList<IReadOnlyList<BehaviorId>> _configuredWaves;
    private IReadOnlyList<Vector2> _spawnPoints;

    public void Configure(RoomDefinition definition, IEnemyPathProvider pathProvider)
    {
        definition.Validate();
        _configuredWaves = definition.Waves.Select(wave => (IReadOnlyList<BehaviorId>)wave.Behaviors.ToArray()).ToArray();
        _spawnPoints = definition.EnemySpawnPoints.ToArray();
        _pathProvider = pathProvider;
    }

    public int CurrentWave => _waveIndex + 1;
    public int TotalWaves => _waves?.Count ?? 3;

    public void StartWaves()
    {
        if (_waves is not null) return;
        if (_configuredWaves is null || _spawnPoints is null || _pathProvider is null)
            throw new InvalidOperationException("EnemyDirector 必须使用当前 RoomDefinition 和导航提供器完成配置后才能启动波次。");
        _waves = _configuredWaves;
        StartNextWave();
    }

    private void StartNextWave()
    {
        _waveIndex++;
        _spawnIndex = 0;
        if (_waveIndex >= _waves.Count)
        {
            _finished = true;
            EmitSignal(SignalName.AllWavesFinished);
            return;
        }

        EmitSignal(SignalName.WaveChanged, _waveIndex + 1, _waves.Count);
        SpawnNextInCurrentWave();
    }

    private async void SpawnNextInCurrentWave()
    {
        if (_waves is null || _finished || _spawnIndex >= _waves[_waveIndex].Count)
        {
            return;
        }

        await ToSignal(GetTree().CreateTimer(SpawnIntervalSeconds), SceneTreeTimer.SignalName.Timeout);
        if (_finished || !IsInsideTree())
        {
            return;
        }

        BehaviorId behavior = _waves[_waveIndex][_spawnIndex];
        EnemyTank enemy = EnemyScene.Instantiate<EnemyTank>();
        enemy.Behavior = behavior;
        if (_pathProvider is not null) enemy.SetPathProvider(_pathProvider);
        enemy.GlobalPosition = FindSafeSpawnPoint(_spawnIndex);
        enemy.Destroyed += OnEnemyDestroyed;
        GetParent().AddChild(enemy);

        _spawnIndex++;
        _alive++;
        EmitSignal(SignalName.EnemySpawned, (int)behavior, _waveIndex + 1);
        EmitSignal(SignalName.EnemyCountChanged, _alive);
        SpawnNextInCurrentWave();
    }

    private void OnEnemyDestroyed()
    {
        if (_finished)
        {
            return;
        }

        _alive = Mathf.Max(0, _alive - 1);
        EmitSignal(SignalName.EnemyCountChanged, _alive);
        bool currentWaveSpawned = _spawnIndex >= _waves[_waveIndex].Count;
        if (currentWaveSpawned && _alive == 0)
        {
            StartNextWave();
        }
    }

    private Vector2 FindSafeSpawnPoint(int offset)
    {
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        for (int index = 0; index < _spawnPoints.Count; index++)
        {
            Vector2 candidate = _spawnPoints[(offset + index) % _spawnPoints.Count];
            bool safeFromPlayer = player is null || candidate.DistanceTo(player.GlobalPosition) >= SafeSpawnDistance;
            if (safeFromPlayer)
            {
                return candidate;
            }
        }

        // 所有点都暂时不安全时，取最远候选点；预警时间仍会给玩家反应空间。
        return _spawnPoints[(offset + _spawnPoints.Count - 1) % _spawnPoints.Count];
    }
}
