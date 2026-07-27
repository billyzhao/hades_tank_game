using Godot;

namespace Game1;

/// <summary>Boss 专属固定机枪哨；先显示直线预警，再进行三发短点射。</summary>
public partial class BossGunEmplacement : Node2D
{
    private static readonly PackedScene ProjectileScene = GD.Load<PackedScene>("res://scenes/combat/projectile.tscn");
    private static readonly Texture2D BodyTexture = GD.Load<Texture2D>("res://assets/sprites/bosses/roadblock_gun_emplacement.png");
    [Export] public float TelegraphSeconds { get; set; } = .8f;
    [Signal] public delegate void TelegraphStartedEventHandler();
    [Signal] public delegate void ShotFiredEventHandler();
    private Line2D _warning = null!;
    private Sprite2D _body = null!;
    private bool _active;
    private bool _firing;

    public override void _Ready()
    {
        _body = new Sprite2D
        {
            Texture = BodyTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ZIndex = 2
        };
        AddChild(_body);
        _warning = new Line2D { Width = 2f, DefaultColor = new Color(1f, .2f, .08f, .75f), Visible = false, ZIndex = 4 };
        AddChild(_warning);
        _active = true;
    }

    public async void TriggerBurst()
    {
        if (!_active || _firing || !IsInsideTree()) return;
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player is null) return;
        _firing = true;
        Vector2 direction = GlobalPosition.DirectionTo(player.GlobalPosition);
        _warning.Points = new Vector2[] { Vector2.Zero, ToLocal(player.GlobalPosition) };
        _warning.Visible = true;
        EmitSignal(SignalName.TelegraphStarted);
        await ToSignal(GetTree().CreateTimer(TelegraphSeconds), SceneTreeTimer.SignalName.Timeout);
        _warning.Visible = false;
        for (int shot = 0; shot < 3 && _active && IsInsideTree(); shot++)
        {
            Projectile projectile = ProjectileScene.Instantiate<Projectile>();
            GetTree().CurrentScene.AddChild(projectile);
            projectile.GlobalPosition = GlobalPosition + direction * 12f;
            projectile.CollisionMask = 19;
            projectile.Initialize(new ProjectileSpec(4, 210f, 1.8f, 0), Team.Enemy, direction);
            EmitSignal(SignalName.ShotFired);
            await ToSignal(GetTree().CreateTimer(.12f), SceneTreeTimer.SignalName.Timeout);
        }
        _firing = false;
    }

    public void Stop() { _active = false; _warning.Visible = false; }
}
