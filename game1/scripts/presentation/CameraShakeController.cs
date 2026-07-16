using Godot;

namespace Game1;

/// <summary>使用 Camera2D.Offset 播放渲染震动；不修改房间或角色的物理坐标。</summary>
public partial class CameraShakeController : Node
{
    private readonly CameraShakeState _state = new();
    private Camera2D _camera = null!;

    public override void _Ready()
    {
        _camera = GetParent().GetNode<Camera2D>("Camera2D");
        PlayerTank player = GetParent().GetNode<PlayerTank>("PlayerTank");
        WeaponController weapon = player.GetNode<WeaponController>("WeaponController");
        weapon.Fired += (_, _, _) => _state.Start(1.1f, 0.08f);
        weapon.ProjectileImpacted += (_, destroyed, reflected) =>
            _state.Start(destroyed ? 2.8f : reflected ? 1.5f : 1.2f, destroyed ? 0.14f : 0.09f);
    }

    public override void _Process(double delta) => _camera.Offset = _state.Advance((float)delta);

    public override void _ExitTree()
    {
        if (IsInstanceValid(_camera))
        {
            _camera.Offset = Vector2.Zero;
        }
    }
}
