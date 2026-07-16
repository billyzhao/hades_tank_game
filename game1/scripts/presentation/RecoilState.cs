using Godot;

namespace Game1;

/// <summary>炮塔后坐的纯表现状态；数值表示沿炮口反方向的本地偏移像素。</summary>
public sealed class RecoilState
{
    private float _pixels;
    private float _duration;
    private float _remaining;

    public void Kick(float pixels, float seconds)
    {
        _pixels = Mathf.Max(0f, pixels);
        _duration = Mathf.Max(0f, seconds);
        _remaining = _duration;
    }

    public float Advance(float delta)
    {
        if (_remaining <= 0f || _pixels <= 0f)
        {
            return 0f;
        }

        _remaining = Mathf.Max(0f, _remaining - Mathf.Max(0f, delta));
        return _remaining <= 0f ? 0f : _pixels * (_remaining / _duration);
    }
}
