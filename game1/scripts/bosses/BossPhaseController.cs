using System;

namespace Game1;

/// <summary>
/// 纯运行时 Boss 阶段裁决器。它不依赖 Godot 节点，避免阶段规则与场景、HUD 或攻击行为耦合。
/// </summary>
public sealed class BossPhaseController
{
    public BossPhase CurrentPhase { get; private set; } = BossPhase.PhaseOne;
    public event Action<BossPhase> PhaseChanged;
    public event Action Defeated;

    public BossPhase ReportHealth(int currentHealth, int maximumHealth)
    {
        if (maximumHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHealth));
        if (CurrentPhase == BossPhase.Defeated) return CurrentPhase;

        if (currentHealth <= 0)
        {
            CurrentPhase = BossPhase.Defeated;
            Defeated?.Invoke();
            return CurrentPhase;
        }

        if (CurrentPhase == BossPhase.PhaseOne && currentHealth * 2 <= maximumHealth)
        {
            CurrentPhase = BossPhase.PhaseTwo;
            PhaseChanged?.Invoke(CurrentPhase);
        }

        return CurrentPhase;
    }
}
