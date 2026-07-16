using Godot;

namespace Game1;

public partial class AppRoot : Node
{
    private static readonly PackedScene MvpCombatRoomScene = GD.Load<PackedScene>("res://scenes/rooms/mvp_combat_room.tscn");

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

    public override void _Ready()
    {
        CurrentRun = RunState.CreateNew(System.Environment.TickCount);
        Node roomHost = GetNode<Node>("RoomHost");
        Node2D room = MvpCombatRoomScene.Instantiate<Node2D>();
        roomHost.AddChild(room);
        _relay = room.GetNode<RelayStation>("RelayStation");
        _relay.Initialize(CurrentRun);
        _playerHealth = room.GetNode<PlayerTank>("PlayerTank").GetNode<HealthComponent>("HealthComponent");
        _armorLabel = GetNode<Label>("UI/Hud/ArmorLabel");
        _relayLabel = GetNode<Label>("UI/Hud/RelayLabel");
        _rebootLabel = GetNode<Label>("UI/Hud/RebootLabel");
        _enemyLabel = GetNode<Label>("UI/Hud/EnemyLabel");
        _roomLabel = GetNode<Label>("UI/Hud/RoomLabel");
        _waveLabel = GetNode<Label>("UI/Hud/WaveLabel");
        _eventLabel = GetNode<Label>("UI/EventLabel");
        _playerHealth.Depleted += () => _eventLabel.Text = "坦克报废：尝试战场重启";
        RebootController reboot = room.GetNode<RebootController>("RebootController");
        reboot.Rebooted += () => _eventLabel.Text = "重启成功：已在中继站旁恢复 50 装甲";
        reboot.RunFailed += () => _eventLabel.Text = "本局失败：没有剩余战场重启次数";
        EnemyDirector director = room.GetNode<EnemyDirector>("EnemyDirector");
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
    }

    public override void _Process(double delta)
    {
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

    private static string BehaviorName(BehaviorId behavior) => behavior switch
    {
        BehaviorId.Patrol => "巡逻坦克（追击玩家）",
        BehaviorId.Assault => "突击车（快速压迫）",
        BehaviorId.Siege => "攻城炮车（攻击中继站）",
        _ => "未知单位"
    };
}
