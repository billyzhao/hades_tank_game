using Godot;

namespace Game1;

/// <summary>入口预警的短生命周期表现；仅提示即将生成的位置，不参与碰撞或导航。</summary>
public partial class SpawnWarning : Node2D
{
    private static readonly Texture2D BeaconTexture = GD.Load<Texture2D>("res://assets/sprites/terrain/spawn_beacon.png");
    private float _remaining;
    private float _duration;
    private Sprite2D _beacon = null!;

    public override void _Ready()
    {
        _beacon = new Sprite2D
        {
            Texture = BeaconTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Modulate = new Color(1f, 1f, 1f, .82f),
            ZIndex = -1
        };
        AddChild(_beacon);
    }

    public void Begin(float seconds, Color color)
    {
        _duration = Mathf.Max(0.05f, seconds);
        _remaining = _duration;
        Modulate = color;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0f) { QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float phase = 1f - _remaining / _duration;
        float radius = Mathf.Lerp(6f, 16f, phase);
        if (_beacon is not null)
        {
            float pulse = .82f + Mathf.Sin(phase * Mathf.Tau * 2f) * .12f;
            _beacon.Scale = Vector2.One * pulse;
        }
        DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 20, new Color(1f, 0.45f, 0.12f, 0.85f), 1.5f);
        DrawLine(Vector2.Left * radius, Vector2.Right * radius, new Color(1f, 0.45f, 0.12f, 0.6f), 1f);
    }
}
