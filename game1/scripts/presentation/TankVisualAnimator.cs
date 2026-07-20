using Godot;

namespace Game1;

/// <summary>将成功开火转换为炮塔后坐和短促炮口火光；只移动视觉节点。</summary>
public partial class TankVisualAnimator : Node
{
    private readonly RecoilState _recoil = new();
    private PlayerTank _player = null!;
    private Sprite2D _bodyVisual = null!;
    private Node2D _turretVisual = null!;
    private CanvasItem _muzzleFlash = null!;
    private Vector2 _velocity;
    private bool _isDashing;
    private float _flashRemaining;
    private float _motionClock;
    private float _dustCooldown;
    private int _dustSide = 1;
    private Vector2 _bodyBaseScale;

    public override void _Ready()
    {
        _player = GetParent<PlayerTank>();
        _bodyVisual = _player.GetNode<Sprite2D>("BodyVisual");
        _bodyBaseScale = _bodyVisual.Scale;
        _turretVisual = _player.GetNode<Node2D>("Turret/TurretVisual");
        _muzzleFlash = _player.GetNode<CanvasItem>("Turret/Muzzle/MuzzleFlash");
        _player.GetNode<WeaponController>("WeaponController").Fired += OnFired;
        _muzzleFlash.Visible = false;
    }

    public void SetMotion(Vector2 velocity, bool isDashing)
    {
        _velocity = velocity;
        _isDashing = isDashing;
    }

    public override void _Process(double delta)
    {
        float deltaSeconds = (float)delta;
        _turretVisual.Position = new Vector2(-_recoil.Advance(deltaSeconds), 0f);
        _flashRemaining = Mathf.Max(0f, _flashRemaining - deltaSeconds);
        _muzzleFlash.Visible = _flashRemaining > 0f;
        UpdateMovement(deltaSeconds);
    }

    private void OnFired(Vector2 origin, Vector2 direction, int team)
    {
        _recoil.Kick(2f, 0.10f);
        _flashRemaining = 0.08f;
    }

    private void UpdateMovement(float delta)
    {
        bool moving = !_velocity.IsZeroApprox();
        if (!moving)
        {
            _bodyVisual.Position = _bodyVisual.Position.Lerp(Vector2.Zero, Mathf.Min(1f, delta * 14f));
            _bodyVisual.Scale = _bodyVisual.Scale.Lerp(_bodyBaseScale, Mathf.Min(1f, delta * 14f));
            return;
        }

        _motionClock += delta * (_isDashing ? 2.2f : 1f);
        float treadPulse = Mathf.Sin(_motionClock * 18f);
        _bodyVisual.Position = new Vector2(0f, treadPulse * (_isDashing ? 0.75f : 0.40f));
        _bodyVisual.Scale = new Vector2(
            _bodyBaseScale.X + treadPulse * 0.010f,
            _bodyBaseScale.Y - treadPulse * 0.006f);

        _dustCooldown -= delta;
        float interval = _isDashing ? 0.035f : 0.12f;
        if (_dustCooldown <= 0f)
        {
            SpawnDust(_isDashing ? 1.7f : 1f);
            _dustCooldown = interval;
        }
    }

    private void SpawnDust(float intensity)
    {
        Vector2 backward = -_player.Transform.X.Normalized();
        Vector2 sideways = backward.Orthogonal() * (4f * _dustSide);
        _dustSide *= -1;
        Polygon2D dust = new()
        {
            GlobalPosition = _player.GlobalPosition + backward * 10f + sideways,
            Polygon = new Vector2[] { new(-2f, -1f), new(2f, -1f), new(3f, 1f), new(-3f, 1f) },
            Color = new Color(0.68f, 0.45f, 0.20f, 0.72f),
            Rotation = _player.Rotation,
            ZIndex = -1
        };
        GetTree().CurrentScene.AddChild(dust);
        Tween tween = dust.CreateTween();
        tween.SetParallel();
        tween.TweenProperty(dust, "scale", Vector2.One * intensity * 2.2f, 0.24);
        tween.TweenProperty(dust, "modulate:a", 0f, 0.26);
        tween.Chain().TweenCallback(Callable.From(dust.QueueFree));
    }
}
