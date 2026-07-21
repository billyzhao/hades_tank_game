using System;
using Godot;

namespace Game1;

/// <summary>手工布置的竞技场边缘入口；位置与朝向来自场景，不允许导演在场内随机造点。</summary>
public readonly record struct SpawnEntrance(
    string Id,
    Vector2 Position,
    Vector2 Facing,
    float WarningSeconds)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("出生入口 Id 不得为空。", nameof(Id));
        if (!Position.IsFinite() || !Facing.IsFinite() || Facing.IsZeroApprox())
            throw new ArgumentException($"出生入口 '{Id}' 的位置或朝向无效。");
        if (!float.IsFinite(WarningSeconds) || WarningSeconds < 0f)
            throw new ArgumentException($"出生入口 '{Id}' 的预警时间无效。");
    }
}
