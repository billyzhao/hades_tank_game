using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>经验与升级卡片的唯一流程控制器；一次只公开一组候选，队列清空才允许恢复战斗。</summary>
public sealed class LevelUpController
{
    private readonly RunState _state;
    private readonly BuildController _build;
    private readonly ControlledStatOfferGenerator _offers;
    private readonly ExperienceCurve _curve;
    private IReadOnlyList<StatUpgradeOffer> _currentOffer = Array.Empty<StatUpgradeOffer>();

    public LevelUpController(RunState state, BuildController build, ControlledStatOfferGenerator offers, ExperienceCurve curve)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _build = build ?? throw new ArgumentNullException(nameof(build));
        _offers = offers ?? throw new ArgumentNullException(nameof(offers));
        _curve = curve ?? throw new ArgumentNullException(nameof(curve));
    }

    public bool IsChoosing => _currentOffer.Count > 0;
    public IReadOnlyList<StatUpgradeOffer> CurrentOffer => _currentOffer;
    public event Action<int, IReadOnlyList<StatUpgradeOffer>> OfferRequested;
    public event Action QueueDrained;

    public void AddExperience(int amount)
    {
        _state.AddExperience(amount, _curve);
        RequestNextIfPossible();
    }

    public void Choose(StatUpgradeId id)
    {
        if (!IsChoosing) throw new InvalidOperationException("当前没有可选择的升级。 ");
        StatUpgradeOffer selected = _currentOffer.FirstOrDefault(offer => offer.Id == id)
            ?? throw new ArgumentException("选择不属于当前升级候选。", nameof(id));
        _build.ApplyStatUpgrade(selected);
        _currentOffer = Array.Empty<StatUpgradeOffer>();
        RequestNextIfPossible();
    }

    private void RequestNextIfPossible()
    {
        if (IsChoosing) return;
        if (!_state.TryConsumePendingLevel(out int level))
        {
            QueueDrained?.Invoke();
            return;
        }
        _currentOffer = _offers.Generate(_state.Seed, level, _build.StatUpgradeStacks);
        OfferRequested?.Invoke(level, _currentOffer);
    }
}
