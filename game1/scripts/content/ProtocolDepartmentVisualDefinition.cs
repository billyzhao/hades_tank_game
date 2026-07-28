using Godot;

namespace Game1;

/// <summary>一个部门协议在坦克上的只读视觉配置。</summary>
[GlobalClass]
public partial class ProtocolDepartmentVisualDefinition : Resource
{
    [Export] public ProtocolDepartment Department { get; set; }
    [Export] public Texture2D Texture { get; set; } = null!;
    [Export] public TankVisualSlot Slot { get; set; }
    [Export] public Vector2 LocalPosition { get; set; }
    [Export] public float RotationDegrees { get; set; } = 90f;
    [Export] public float BaseScale { get; set; } = 0.18f;
    [Export] public Color AccentColor { get; set; } = Colors.White;

    public void Validate()
    {
        if (!System.Enum.IsDefined(Department)) throw new System.ArgumentOutOfRangeException(nameof(Department));
        if (!System.Enum.IsDefined(Slot)) throw new System.ArgumentOutOfRangeException(nameof(Slot));
        if (Texture is null) throw new System.ArgumentException($"{Department} 缺少协议模块贴图。");
        if (!float.IsFinite(BaseScale) || BaseScale is <= 0f or > 1f)
            throw new System.ArgumentOutOfRangeException(nameof(BaseScale));
    }
}
