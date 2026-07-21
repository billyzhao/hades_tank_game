using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>当前单局协议选择与战斗事件订阅的唯一所有者。</summary>
public sealed class BuildController
{
    private readonly RunState _state;
    private readonly ContentCatalog _catalog;
    private readonly StatPipeline _statPipeline = new();
    private readonly System.Collections.Generic.List<StatModifier> _modifiers = new();
    private readonly Dictionary<StatUpgradeId, int> _statUpgradeStacks = new();
    private bool _ended;

    public BuildController(RunState state, ContentCatalog catalog)
    {
        _state = state ?? throw new System.ArgumentNullException(nameof(state));
        _catalog = catalog ?? throw new System.ArgumentNullException(nameof(catalog));
        _catalog.Validate();
    }

    /// <summary>快照变更通知；运行时组件只订阅此受控入口，EndRun 会统一解除。</summary>
    public event Action SnapshotChanged;
    public event Action ShotFired;
    public event Action ProjectileHit;
    public event Action DashStarted;
    public event Action RelayDamaged;
    public event Action RoomCleared;

    public System.Collections.Generic.IReadOnlyList<StatModifier> ModifierSnapshot => _modifiers.ToArray();
    public IReadOnlyDictionary<StatUpgradeId, int> StatUpgradeStacks => _statUpgradeStacks;

    public ContentCatalog Catalog => _catalog;

    public string CatalogVersion => _catalog.Version;

    public BuildSnapshot GetSnapshot() => new(_state.SelectedProtocolIds.ToArray(), ModifierSnapshot);

    public void SelectProtocol(string protocolId)
    {
        EnsureActive();
        ProtocolDefinition protocol = _catalog.GetProtocol(protocolId);
        ValidateSelection(protocol);

        _state.RecordSelectedProtocol(protocol.Id);
        foreach (ProtocolEffectDefinition effect in protocol.Effects)
        {
            _modifiers.Add(new StatModifier(
                effect.Stat,
                effect.FlatAdd,
                effect.AdditivePercent,
                effect.MultiplicativePercent,
                protocol.Id));
        }

        SnapshotChanged?.Invoke();
    }

    public void ApplyStatUpgrade(StatUpgradeOffer offer)
    {
        EnsureActive();
        if (offer is null) throw new ArgumentNullException(nameof(offer));
        int count = _statUpgradeStacks.GetValueOrDefault(offer.Id);
        if (count >= offer.StackLimit) throw new InvalidOperationException("该基础属性已达到升级上限。 ");
        _statUpgradeStacks[offer.Id] = count + 1;
        _modifiers.Add(offer.Modifier);
        SnapshotChanged?.Invoke();
    }

    public float EvaluateStat(StatId stat, float baseValue)
    {
        return _ended ? baseValue : _statPipeline.Evaluate(stat, baseValue, _modifiers);
    }

    public void OnShotFired()
    {
        if (!_ended) ShotFired?.Invoke();
    }

    public void OnProjectileHit()
    {
        if (!_ended) ProjectileHit?.Invoke();
    }

    public void OnDashStarted()
    {
        if (!_ended) DashStarted?.Invoke();
    }

    public void OnRelayDamaged()
    {
        if (!_ended) RelayDamaged?.Invoke();
    }

    public void OnRoomCleared()
    {
        if (!_ended) RoomCleared?.Invoke();
    }

    public void EndRun()
    {
        if (_ended) return;

        _ended = true;
        _modifiers.Clear();
        _statUpgradeStacks.Clear();
        _state.ClearBuildState();
        SnapshotChanged = null;
        ShotFired = null;
        ProjectileHit = null;
        DashStarted = null;
        RelayDamaged = null;
        RoomCleared = null;
    }

    private void ValidateSelection(ProtocolDefinition candidate)
    {
        int selectedCount = _state.SelectedProtocolIds.Count(id => string.Equals(id, candidate.Id, System.StringComparison.Ordinal));
        if (selectedCount >= candidate.StackLimit)
        {
            throw new System.InvalidOperationException($"协议 '{candidate.Id}' 已达到叠层上限。");
        }

        System.Collections.Generic.HashSet<string> selectedIds = new(_state.SelectedProtocolIds, System.StringComparer.Ordinal);
        if (candidate.PrerequisiteIds.Any(id => !selectedIds.Contains(id)))
        {
            throw new System.InvalidOperationException($"协议 '{candidate.Id}' 的前置条件尚未满足。");
        }

        if (candidate.ConflictIds.Any(selectedIds.Contains) ||
            _state.SelectedProtocolIds.Any(id => _catalog.GetProtocol(id).ConflictIds.Contains(candidate.Id)))
        {
            throw new System.InvalidOperationException($"协议 '{candidate.Id}' 与当前构筑冲突。");
        }
    }

    private void EnsureActive()
    {
        if (_ended)
        {
            throw new System.InvalidOperationException("本局已经结束，不能再选择协议。");
        }
    }
}

/// <summary>提供给战斗组件的只读本局构筑快照，禁止修改 Resource 或下一局状态。</summary>
public sealed record BuildSnapshot(
    System.Collections.Generic.IReadOnlyList<string> SelectedProtocolIds,
    System.Collections.Generic.IReadOnlyList<StatModifier> Modifiers);
