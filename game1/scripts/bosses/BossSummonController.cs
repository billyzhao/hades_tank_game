using Godot;

namespace Game1;

/// <summary>Boss 专属巡逻召唤器；只复用现有巡逻坦克与共享 A*，并限制同时存活数量。</summary>
public partial class BossSummonController : Node
{
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/actors/enemy_tank.tscn");
    [Export] public int MaximumAlive { get; set; } = 2;
    [Export] public Godot.Collections.Array<Vector2> SpawnPoints { get; set; } = new();
    private Node2D _room = null!;
    private IEnemyPathProvider _pathProvider = null!;
    private int _alive;
    private int _spawnIndex;
    private bool _active;

    public void Initialize(Node2D room, IEnemyPathProvider pathProvider)
    {
        _room = room;
        _pathProvider = pathProvider;
        _active = true;
    }

    public void TrySummon()
    {
        if (!_active || _alive >= MaximumAlive || SpawnPoints.Count == 0) return;
        EnemyTank enemy = EnemyScene.Instantiate<EnemyTank>();
        enemy.Behavior = BehaviorId.Patrol;
        enemy.SetPathProvider(_pathProvider);
        enemy.AddToGroup("enemies");
        enemy.GlobalPosition = SpawnPoints[_spawnIndex++ % SpawnPoints.Count];
        enemy.Destroyed += () => _alive = Mathf.Max(0, _alive - 1);
        _room.AddChild(enemy);
        _alive++;
    }

    public void Stop() => _active = false;
}
