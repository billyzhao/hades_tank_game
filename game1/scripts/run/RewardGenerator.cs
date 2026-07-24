using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game1;

/// <summary>仅生成奖励候选；不写入本局构筑状态。</summary>
public sealed class RewardGenerator
{
    private static readonly BuildRouteCatalog RouteCatalog = BuildRouteCatalog.CreateDefault();
    private static readonly BuildRouteAnalyzer RouteAnalyzer = new(RouteCatalog);

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
        IReadOnlyDictionary<string, ProtocolRank> protocolRanks = input.ProtocolRanks ?? selectedCounts
            .ToDictionary(pair => pair.Key, _ => ProtocolRank.MkI, StringComparer.Ordinal);
        foreach ((string protocolId, ProtocolRank rank) in protocolRanks)
        {
            _ = catalog.GetProtocol(protocolId);
            if (!Enum.IsDefined(rank)) throw new ArgumentException("协议阶级输入无效。", nameof(input));
        }
        HashSet<string> selectedIds = new(selectedCounts.Keys, StringComparer.Ordinal);
        HashSet<string> selectedTags = selectedIds
            .SelectMany(selectedId => catalog.GetProtocol(selectedId).Tags)
            .ToHashSet(StringComparer.Ordinal);
        string selectedRouteTag = null;
        if (input.SelectedCore is CoreId selectedCore)
        {
            BuildRouteAnalysis route = RouteAnalyzer.Analyze(
                selectedCore,
                selectedInStableOrder,
                input.SelectedAuxiliaryIds ?? Array.Empty<string>(),
                catalog);
            selectedRouteTag = route.Route?.Tag;
        }

        List<ProtocolDefinition> eligible = catalog.Protocols
            .OrderBy(protocol => protocol.Id, StringComparer.Ordinal)
            .Where(protocol => IsRewardKindEligible(protocol, input.RewardKind))
            .Where(protocol => IsEligible(protocol, selectedIds, selectedTags, selectedCounts, protocolRanks, input.ProtocolRanks is null, catalog))
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
            TakeWeighted(universal, random, input.SelectedCore, selectedRouteTag)
        };
        eligible.RemoveAll(protocol => string.Equals(protocol.Id, offer[0].Id, StringComparison.Ordinal));

        while (offer.Count < 3 && eligible.Count > 0)
        {
            offer.Add(TakeWeighted(eligible, random, input.SelectedCore, selectedRouteTag));
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
        IReadOnlyDictionary<string, ProtocolRank> protocolRanks,
        bool useLegacyStackLimit,
        ContentCatalog catalog)
    {
        if (protocolRanks.TryGetValue(candidate.Id, out ProtocolRank rank) && rank == ProtocolRank.MkIII)
        {
            return false;
        }

        if (useLegacyStackLimit && selectedCounts.TryGetValue(candidate.Id, out int count) && count >= candidate.StackLimit)
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

    private static bool IsRewardKindEligible(ProtocolDefinition protocol, RewardKind? kind) => kind switch
    {
        null => true,
        RewardKind.NormalProtocol => protocol.Rarity == 1,
        RewardKind.RareProtocol => protocol.Rarity > 1,
        _ => false
    };

    private static ProtocolDefinition TakeWeighted(
        IList<ProtocolDefinition> candidates,
        DeterministicRandom random,
        CoreId? selectedCore,
        string selectedRouteTag)
    {
        double totalWeight = candidates.Sum(candidate => CalculateWeight(candidate, selectedCore, selectedRouteTag));
        double roll = random.NextUnitInterval() * totalWeight;
        double cumulativeWeight = 0d;
        for (int index = 0; index < candidates.Count; index++)
        {
            cumulativeWeight += CalculateWeight(candidates[index], selectedCore, selectedRouteTag);
            if (roll < cumulativeWeight || index == candidates.Count - 1)
            {
                ProtocolDefinition selected = candidates[index];
                candidates.RemoveAt(index);
                return selected;
            }
        }

        throw new InvalidOperationException("加权候选池不能为空。");
    }

    /// <summary>核心和已成型路线只做软加权，所有未冲突候选始终保留资格。</summary>
    internal static double CalculateWeight(ProtocolDefinition candidate, CoreId? selectedCore, string selectedRouteTag)
    {
        double baseWeight = candidate.BaseWeight;
        bool coreMatch = selectedCore is CoreId core &&
            candidate.Tags.Any(RouteCatalog.GetRoutes(core)
                .Select(route => route.Tag)
                .Contains);
        bool routeMatch = !string.IsNullOrWhiteSpace(selectedRouteTag) && candidate.Tags.Contains(selectedRouteTag);
        if (coreMatch) baseWeight *= 1.4d;
        if (routeMatch) baseWeight *= 1.25d;
        return baseWeight;
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
