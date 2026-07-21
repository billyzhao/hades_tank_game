using System;

namespace Game1;

/// <summary>完整单局的顶层裁决器；五波、清场和奖励由 ArenaController 独占。</summary>
public enum RunPhase
{
    Arena,
    Completed,
    Failed
}

public sealed class RunController
{
    private readonly RunState _state;

    public RunController(RunState state, BuildController build)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        ArgumentNullException.ThrowIfNull(build);
        Phase = RunPhase.Arena;
    }

    public RunPhase Phase { get; private set; }
    public RunState State => _state;

    public event Action<RunPhase> PhaseChanged;
    public event Action<int> ArenaRequested;

    /// <summary>玩家装甲耗尽时只裁决重启次数；当前竞技场内的波次状态不被重置。</summary>
    public bool OnTankDefeated()
    {
        if (Phase != RunPhase.Arena) return false;
        if (_state.TryConsumeReboot()) return true;

        OnArenaFailed();
        return false;
    }

    public void OnArenaCompleted()
    {
        if (Phase != RunPhase.Arena)
            throw new InvalidOperationException("只有进行中的竞技场可以完成。");

        if (_state.ArenaIndex >= 4)
        {
            SetPhase(RunPhase.Completed);
            return;
        }

        _state.RestoreArmorForNextArena();
        _state.AdvanceArena();
        ArenaRequested?.Invoke(_state.ArenaIndex);
    }

    public void OnArenaFailed()
    {
        if (Phase != RunPhase.Arena) return;
        SetPhase(RunPhase.Failed);
    }

    private void SetPhase(RunPhase phase)
    {
        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }
}
