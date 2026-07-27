using Godot;

namespace Game1;

/// <summary>
/// 纯渲染震动状态：输出短暂偏移，不持有节点引用，也不会影响战斗物理坐标。
/// </summary>
public sealed class CameraShakeState
{
    private float _trauma;
    private float _maximumOffset;
    private float _decayPerSecond;
    private float _phase;

    public void Start(float strength, float seconds)
    {
        if (strength <= 0f || seconds <= 0f) return;
        float addedTrauma = Mathf.Clamp(strength / 6f, 0f, 1f);
        _trauma = Mathf.Clamp(_trauma + addedTrauma, 0f, 1f);
        _maximumOffset = Mathf.Max(_maximumOffset, strength);
        _decayPerSecond = Mathf.Max(_decayPerSecond, _trauma / seconds);
    }

    public Vector2 Advance(float delta)
    {
        if (_trauma <= 0f || _maximumOffset <= 0f)
        {
            return Vector2.Zero;
        }

        _phase += Mathf.Max(0f, delta) * 46f;
        float shake = _trauma * _trauma;
        Vector2 offset = new(
            Mathf.Sin(_phase * 1.71f) * _maximumOffset * shake,
            Mathf.Sin(_phase * 2.29f + .8f) * _maximumOffset * .72f * shake);
        _trauma = Mathf.Max(0f, _trauma - _decayPerSecond * Mathf.Max(0f, delta));
        if (_trauma <= 0f)
        {
            _maximumOffset = 0f;
            _decayPerSecond = 0f;
        }
        return offset;
    }
}
