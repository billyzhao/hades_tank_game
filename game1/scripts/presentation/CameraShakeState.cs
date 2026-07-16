using Godot;

namespace Game1;

/// <summary>
/// 纯渲染震动状态：输出短暂偏移，不持有节点引用，也不会影响战斗物理坐标。
/// </summary>
public sealed class CameraShakeState
{
    private float _strength;
    private float _duration;
    private float _remaining;
    private float _phase;

    public void Start(float strength, float seconds)
    {
        _strength = Mathf.Max(0f, strength);
        _duration = Mathf.Max(0f, seconds);
        _remaining = _duration;
        _phase = 0f;
    }

    public Vector2 Advance(float delta)
    {
        if (_remaining <= 0f || _strength <= 0f)
        {
            return Vector2.Zero;
        }

        _remaining = Mathf.Max(0f, _remaining - Mathf.Max(0f, delta));
        if (_remaining <= 0f)
        {
            return Vector2.Zero;
        }

        _phase += delta * 58f;
        float intensity = _strength * (_remaining / _duration);
        return new Vector2(Mathf.Sin(_phase), Mathf.Cos(_phase * 1.37f)) * intensity;
    }
}
