using Godot;

namespace Game1;

/// <summary>将成功开火转换为炮塔后坐和短促炮口火光；只移动视觉节点。</summary>
public partial class TankVisualAnimator : Node
{
    private readonly RecoilState _recoil = new();
    private PlayerTank _player = null!;
    private Sprite2D _bodyVisual = null!;
    private Node2D _turretVisual = null!;
    private AnimatedSprite2D _muzzleFlash = null!;
    private Vector2 _velocity;
    private bool _isDashing;
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
        _muzzleFlash = _player.GetNode<AnimatedSprite2D>("Turret/Muzzle/MuzzleFlash");
        _muzzleFlash.SpriteFrames = SpriteEffectPlayer.Create(
            "MuzzleFrames", ArtTextureCatalog.MuzzleFlash, 20f).SpriteFrames;
        _player.GetNode<WeaponController>("WeaponController").Fired += OnFired;
        _muzzleFlash.AnimationFinished += () => _muzzleFlash.Visible = false;
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
        UpdateMovement(deltaSeconds);
    }

    private void OnFired(Vector2 origin, Vector2 direction, int team)
    {
        _recoil.Kick(2f, 0.10f);
        _muzzleFlash.Visible = true;
        _muzzleFlash.Play();
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
        AnimatedSprite2D dust = SpriteEffectPlayer.Spawn(
            GetTree().CurrentScene,
            _player.GlobalPosition + backward * 10f + sideways,
            _isDashing ? ArtTextureCatalog.DashTrail : ArtTextureCatalog.TankDust,
            _isDashing ? 18f : 13f,
            intensity * (_isDashing ? .55f : .42f),
            -1,
            new Color(1f, 1f, 1f, _isDashing ? .82f : .70f));
        dust.Rotation = _player.Rotation + Mathf.Pi / 2f;
    }
}
