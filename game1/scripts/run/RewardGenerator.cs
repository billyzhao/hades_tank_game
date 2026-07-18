using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game1;

/// <summary>仅生成奖励候选；不写入本局构筑状态。</summary>
public sealed class RewardGenerator
{
    public ProtocolOffer Generate(RewardGenerationInput input, ContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(catalog);
        catalog.Validate();

        if (!string.Equals(input.ContentCatalogVersion, catalog.Version, StringComparison.Ordinal))
        {
            throw new ArgumentException("奖励输入的内容目录版本与已加载目录不一致。", nameof(input));
        }

        if (input.SelectedProtocolIds is null)
        {
            throw new ArgumentException("奖励输入必须包含已选协议列表。", nameof(input));
        }

        List<string> selectedInStableOrder = input.SelectedProtocolIds
            .Select(id => id ?? throw new ArgumentException("已选协议 Id 不得为空。", nameof(input)))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        foreach (string selectedId in selectedInStableOrder)
        {
            _ = catalog.GetProtocol(selectedId);
        }

        Dictionary<string, int> selectedCounts = selectedInStableOrder
            .GroupBy(id => id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        HashSet<string> selectedIds = new(selectedCounts.Keys, StringComparer.Ordinal);
        HashSet<string> selectedTags = selectedIds
            .SelectMany(selectedId => catalog.GetProtocol(selectedId).Tags)
            .ToHashSet(StringComparer.Ordinal);

        List<ProtocolDefinition> eligible = catalog.Protocols
            .OrderBy(protocol => protocol.Id, StringComparer.Ordinal)
            .Where(protocol => IsEligible(protocol, selectedIds, selectedTags, selectedCounts, catalog))
            .ToList();
        List<ProtocolDefinition> universal = eligible
            .Where(protocol =>
                protocol.PrerequisiteIds.Count == 0 &&
                protocol.RequiredTags.Count == 0 &&
                protocol.ConflictIds.Count == 0 &&
                protocol.ConflictTags.Count == 0)
            .ToList();

        if (universal.Count == 0)
        {
            throw new InvalidOperationException("当前奖励池没有可用的通用候选，无法满足三选一约束。");
        }

        DeterministicRandom random = new(CreateSeed(input, selectedInStableOrder));
        List<ProtocolDefinition> offer = new(3)
        {
            TakeWeighted(universal, random)
        };
        eligible.RemoveAll(protocol => string.Equals(protocol.Id, offer[0].Id, StringComparison.Ordinal));

        while (offer.Count < 3 && eligible.Count > 0)
        {
            offer.Add(TakeWeighted(eligible, random));
        }

        if (offer.Count != 3)
        {
            throw new InvalidOperationException("满足约束的协议不足三项，无法生成有效奖励。");
        }

        return new ProtocolOffer(offer.Select(protocol => protocol.Id).ToArray());
    }

    private static bool IsEligible(
        ProtocolDefinition candidate,
        IReadOnlySet<string> selectedIds,
        IReadOnlySet<string> selectedTags,
        IReadOnlyDictionary<string, int> selectedCounts,
        ContentCatalog catalog)
    {
        if (selectedCounts.TryGetValue(candidate.Id, out int count) && count >= candidate.StackLimit)
        {
            return false;
        }

        if (candidate.PrerequisiteIds.Any(prerequisiteId => !selectedIds.Contains(prerequisiteId)))
        {
            return false;
        }

        if (candidate.RequiredTags.Any(requiredTag => !selectedTags.Contains(requiredTag)))
        {
            return false;
        }

        if (candidate.ConflictIds.Any(selectedIds.Contains) || candidate.ConflictTags.Any(selectedTags.Contains))
        {
            return false;
        }

        foreach (string selectedId in selectedIds)
        {
            ProtocolDefinition selected = catalog.GetProtocol(selectedId);
            if (selected.ConflictIds.Contains(candidate.Id))
            {
                return false;
            }

            if (selected.ConflictTags.Any(candidate.Tags.Contains))
            {
                return false;
            }
        }

        return true;
    }

    private static ProtocolDefinition TakeWeighted(IList<ProtocolDefinition> candidates, DeterministicRandom random)
    {
        double totalWeight = candidates.Sum(candidate => (double)candidate.BaseWeight);
        double roll = random.NextUnitInterval() * totalWeight;
        double cumulativeWeight = 0d;
        for (int index = 0; index < candidates.Count; index++)
        {
            cumulativeWeight += candidates[index].BaseWeight;
            if (roll < cumulativeWeight || index == candidates.Count - 1)
            {
                ProtocolDefinition selected = candidates[index];
                candidates.RemoveAt(index);
                return selected;
            }
        }

        throw new InvalidOperationException("加权候选池不能为空。");
    }

    private static ulong CreateSeed(RewardGenerationInput input, IEnumerable<string> selectedInStableOrder)
    {
        StringBuilder canonicalInput = new();
        canonicalInput.Append(input.RunSeed).Append('|')
            .Append(input.RoomIndex).Append('|')
            .Append(input.ContentCatalogVersion).Append('|');
        foreach (string selectedId in selectedInStableOrder)
        {
            canonicalInput.Append(selectedId).Append('|');
        }

        return StableHash(canonicalInput.ToString());
    }

    private static ulong StableHash(string value)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        foreach (byte valueByte in Encoding.UTF8.GetBytes(value))
        {
            hash ^= valueByte;
            hash *= prime;
        }

        return hash;
    }

    private sealed class DeterministicRandom
    {
        private ulong _state;

        internal DeterministicRandom(ulong seed)
        {
            _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
        }

        internal double NextUnitInterval()
        {
            _state ^= _state << 13;
            _state ^= _state >> 7;
            _state ^= _state << 17;
            return (_state >> 11) * (1d / (1UL << 53));
        }
    }
}
