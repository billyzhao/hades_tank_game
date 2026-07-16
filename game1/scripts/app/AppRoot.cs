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
        _eventLabel = GetNode<Label>("UI/EventLabel");
        _playerHealth.Depleted += () => _eventLabel.Text = "坦克报废：尝试战场重启";
        RebootController reboot = room.GetNode<RebootController>("RebootController");
        reboot.Rebooted += () => _eventLabel.Text = "重启成功：已在中继站旁恢复 50 装甲";
        reboot.RunFailed += () => _eventLabel.Text = "本局失败：没有剩余战场重启次数";
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
        _armorLabel.Text = $"坦克装甲  {_playerHealth.Armor}/{_playerHealth.MaximumArmor}";
        _relayLabel.Text = $"中继站耐久  {CurrentRun.RelayIntegrity}/100";
        _rebootLabel.Text = $"战场重启  {CurrentRun.RebootsRemaining}";
    }
}
