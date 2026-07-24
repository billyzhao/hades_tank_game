using Godot;

namespace Game1;

/// <summary>入口预警的短生命周期表现；仅提示即将生成的位置，不参与碰撞或导航。</summary>
public partial class SpawnWarning : Node2D
{
    private float _remaining;
    private float _duration;
    private AnimatedSprite2D _warning = null!;

    public override void _Ready()
    {
        _warning = SpriteEffectPlayer.Create("SpawnWarningVisual", ArtTextureCatalog.SpawnWarning, 10f, true);
        _warning.Scale = Vector2.One * .65f;
        _warning.ZIndex = -1;
        AddChild(_warning);
        _warning.Play();
    }

    public void Begin(float seconds, Color color)
    {
        _duration = Mathf.Max(0.05f, seconds);
        _remaining = _duration;
        Modulate = color;
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0f) { QueueFree(); return; }
        float phase = 1f - _remaining / _duration;
        if (_warning is not null)
        {
            float pulse = .82f + Mathf.Sin(phase * Mathf.Tau * 2f) * .12f;
            _warning.Scale = Vector2.One * pulse * .65f;
        }
    }
}
