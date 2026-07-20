using System;
using System.Collections.Generic;
using Godot;

namespace Game1;

public enum PauseReason
{
    Manual,
    FocusLost,
    LevelUp,
    InterWaveReward
}

/// <summary>
/// 聚合所有暂停原因。每个调用方只能释放自己持有的原因，避免失焦恢复误解锁手动或升级暂停。
/// </summary>
public sealed class PauseCoordinator
{
    private readonly SceneTree _sceneTree;
    private readonly HashSet<PauseReason> _reasons = new();

    public PauseCoordinator(SceneTree sceneTree) =>
        _sceneTree = sceneTree ?? throw new ArgumentNullException(nameof(sceneTree));

    public bool IsPaused => _reasons.Count > 0;

    public event Action<bool> PauseChanged = delegate { };

    public bool Contains(PauseReason reason) => _reasons.Contains(reason);

    public void Acquire(PauseReason reason)
    {
        bool wasPaused = IsPaused;
        if (!_reasons.Add(reason)) return;
        ApplyTransition(wasPaused);
    }

    public void Release(PauseReason reason)
    {
        bool wasPaused = IsPaused;
        if (!_reasons.Remove(reason)) return;
        ApplyTransition(wasPaused);
    }

    private void ApplyTransition(bool wasPaused)
    {
        bool paused = IsPaused;
        if (paused == wasPaused) return;
        _sceneTree.Paused = paused;
        PauseChanged(paused);
    }
}
