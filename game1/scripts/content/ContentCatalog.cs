using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>协议内容目录的加载与校验入口。</summary>
[GlobalClass]
public partial class ContentCatalog : Resource
{
    [Export] public string Version { get; set; } = string.Empty;

    [Export] public Godot.Collections.Array<ProtocolDefinition> Protocols { get; set; } = new();

    [Export] public Godot.Collections.Array<AuxiliaryDefinition> Auxiliaries { get; set; } = new();

    [Export] public Godot.Collections.Array<EnemyDefinition> Enemies { get; set; } = new();

    [Export] public Godot.Collections.Array<EliteModifierDefinition> EliteModifiers { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Version) || !string.Equals(Version, Version.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("ContentCatalog.Version 必须是非空且无首尾空白的稳定版本标识。", nameof(Version));
        }

        if (Protocols is null || Protocols.Count == 0)
        {
            throw new ArgumentException("ContentCatalog 至少需要一项协议定义。", nameof(Protocols));
        }

        Dictionary<string, ProtocolDefinition> protocolsById = new(StringComparer.Ordinal);
        HashSet<string> effectIds = new(StringComparer.Ordinal);

        foreach (ProtocolDefinition protocol in Protocols)
        {
            ValidateProtocol(protocol, protocolsById, effectIds);
        }

        foreach (ProtocolDefinition protocol in protocolsById.Values)
        {
            ValidateRelations(protocol, protocolsById);
            ValidateTagRelations(protocol, protocolsById.Values);
        }

        ValidatePrerequisiteGraph(protocolsById);
        ValidateAuxiliaries();
        ValidateEnemies();
        ValidateEliteModifiers();
    }

    public ProtocolDefinition GetProtocol(string protocolId)
    {
        if (string.IsNullOrWhiteSpace(protocolId))
        {
            throw new ArgumentException("协议 Id 不得为空。", nameof(protocolId));
        }

        foreach (ProtocolDefinition protocol in Protocols)
        {
            if (protocol is not null && string.Equals(protocol.Id, protocolId, StringComparison.Ordinal))
            {
                return protocol;
            }
        }

        throw new ArgumentException($"ContentCatalog 中不存在协议 '{protocolId}'。", nameof(protocolId));
    }

    public AuxiliaryDefinition GetAuxiliary(string auxiliaryId)
    {
        if (string.IsNullOrWhiteSpace(auxiliaryId)) throw new ArgumentException("辅助 Id 不得为空。", nameof(auxiliaryId));
        foreach (AuxiliaryDefinition auxiliary in Auxiliaries)
        {
            if (auxiliary is not null && string.Equals(auxiliary.Id, auxiliaryId, StringComparison.Ordinal)) return auxiliary;
        }
        throw new ArgumentException($"ContentCatalog 中不存在辅助 '{auxiliaryId}'。", nameof(auxiliaryId));
    }

    public EnemyDefinition GetEnemy(BehaviorId behavior)
    {
        foreach (EnemyDefinition enemy in Enemies)
        {
            if (enemy is not null && enemy.Behavior == behavior) return enemy;
        }
        throw new ArgumentException($"ContentCatalog 中不存在敌军职责 '{behavior}'。", nameof(behavior));
    }

    public EliteModifierDefinition GetEliteModifier(string modifierId)
    {
        if (string.IsNullOrWhiteSpace(modifierId))
            throw new ArgumentException("精英规则 Id 不得为空。", nameof(modifierId));
        foreach (EliteModifierDefinition modifier in EliteModifiers)
        {
            if (modifier is not null && string.Equals(modifier.Id, modifierId, StringComparison.Ordinal)) return modifier;
        }
        throw new ArgumentException($"ContentCatalog 中不存在精英规则 '{modifierId}'。", nameof(modifierId));
    }

    private void ValidateEliteModifiers()
    {
        if (EliteModifiers is null || EliteModifiers.Count != 1)
            throw new ArgumentException("封锁城区必须且只能登记一个精英规则。", nameof(EliteModifiers));
        EliteModifiers[0]?.Validate();
        if (EliteModifiers[0] is null)
            throw new ArgumentException("精英规则不得为空。", nameof(EliteModifiers));
    }

    private void ValidateEnemies()
    {
        if (Enemies is null || Enemies.Count != 4)
            throw new ArgumentException("封锁城区内容目录必须恰好包含四类普通敌军。", nameof(Enemies));
        HashSet<string> ids = new(StringComparer.Ordinal);
        HashSet<BehaviorId> behaviors = new();
        foreach (EnemyDefinition enemy in Enemies)
        {
            if (enemy is null) throw new ArgumentException("敌军目录不得包含空引用。", nameof(Enemies));
            enemy.Validate();
            if (!ids.Add(enemy.Id) || !behaviors.Add(enemy.Behavior))
                throw new ArgumentException("敌军 Id 和职责必须唯一。", nameof(Enemies));
        }
    }

    private void ValidateAuxiliaries()
    {
        if (Auxiliaries is null || Auxiliaries.Count != 4)
            throw new ArgumentException("首区内容目录必须恰好包含四种辅助系统。", nameof(Auxiliaries));
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (AuxiliaryDefinition auxiliary in Auxiliaries)
        {
            if (auxiliary is null || string.IsNullOrWhiteSpace(auxiliary.Id) || !ids.Add(auxiliary.Id) ||
                string.IsNullOrWhiteSpace(auxiliary.DisplayName) || string.IsNullOrWhiteSpace(auxiliary.Description) ||
                !Enum.IsDefined(auxiliary.TargetMode) || !float.IsFinite(auxiliary.BaseCooldown) || auxiliary.BaseCooldown <= 0f ||
                auxiliary.MaximumRank <= 0 || auxiliary.BaseDamage <= 0 || !float.IsFinite(auxiliary.Range) || auxiliary.Range <= 0f)
                throw new ArgumentException("辅助系统目录包含无效或重复的配置。");
            if (auxiliary.BuildTags is null ||
                auxiliary.BuildTags.Any(string.IsNullOrWhiteSpace) ||
                auxiliary.BuildTags.Any(tag => !string.Equals(tag, tag.Trim(), StringComparison.Ordinal)) ||
                auxiliary.BuildTags.Distinct(StringComparer.Ordinal).Count() != auxiliary.BuildTags.Count)
                throw new ArgumentException($"辅助系统 '{auxiliary.Id}' 的构筑标签无效或重复。");
        }
    }

    private static void ValidateProtocol(
        ProtocolDefinition protocol,
        IDictionary<string, ProtocolDefinition> protocolsById,
        ISet<string> effectIds)
    {
        if (protocol is null)
        {
            throw new ArgumentException("ContentCatalog 不得包含空协议引用。");
        }

        if (string.IsNullOrWhiteSpace(protocol.Id) || !string.Equals(protocol.Id, protocol.Id.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("协议 Id 必须是非空且无首尾空白的稳定标识。");
        }

        if (!protocolsById.TryAdd(protocol.Id, protocol))
        {
            throw new ArgumentException($"检测到重复协议 Id：'{protocol.Id}'。");
        }

        if (string.IsNullOrWhiteSpace(protocol.DisplayName) || string.IsNullOrWhiteSpace(protocol.Description))
        {
            throw new ArgumentException($"协议 '{protocol.Id}' 缺少可读名称或说明。");
        }

        if (!Enum.IsDefined(protocol.Department) || protocol.StackLimit <= 0)
        {
            throw new ArgumentException($"协议 '{protocol.Id}' 的部门或叠层上限无效。");
        }

        if (protocol.Rarity <= 0 || !float.IsFinite(protocol.BaseWeight) || protocol.BaseWeight <= 0f)
        {
            throw new ArgumentException($"协议 '{protocol.Id}' 的稀有度或基础权重无效。");
        }

        HashSet<string> tags = ValidateTags(protocol, protocol.Tags, "提供");
        HashSet<string> requiredTags = ValidateTags(protocol, protocol.RequiredTags, "需求");
        HashSet<string> conflictTags = ValidateTags(protocol, protocol.ConflictTags, "冲突");
        if (requiredTags.Overlaps(conflictTags))
        {
            throw new ArgumentException($"协议 '{protocol.Id}' 的同一标签不能同时为需求与冲突标签。");
        }

        if (tags.Overlaps(conflictTags))
        {
            throw new ArgumentException($"协议 '{protocol.Id}' 不能同时提供并冲突同一标签。");
        }

        if (protocol.Effects is null || protocol.Effects.Count == 0)
        {
            throw new ArgumentException($"协议 '{protocol.Id}' 至少需要一个效果声明。");
        }

        foreach (ProtocolEffectDefinition effect in protocol.Effects)
        {
            if (effect is null)
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 包含空效果引用。");
            }

            if (string.IsNullOrWhiteSpace(effect.EffectId) ||
                !string.Equals(effect.EffectId, effect.EffectId.Trim(), StringComparison.Ordinal) ||
                !effectIds.Add(effect.EffectId))
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 的效果 Id 为空、含首尾空白或重复。");
            }

            if (!Enum.IsDefined(typeof(StatId), effect.Stat) ||
                !float.IsFinite(effect.FlatAdd) ||
                !float.IsFinite(effect.AdditivePercent) ||
                !float.IsFinite(effect.MultiplicativePercent))
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 的效果 '{effect.EffectId}' 包含无效属性或非有限数值。");
            }
        }
    }

    private static void ValidateRelations(
        ProtocolDefinition protocol,
        IReadOnlyDictionary<string, ProtocolDefinition> protocolsById)
    {
        HashSet<string> conflicts = ValidateRelationIds(protocol, protocol.ConflictIds, "冲突", protocolsById);
        HashSet<string> prerequisites = ValidateRelationIds(protocol, protocol.PrerequisiteIds, "前置", protocolsById);

        if (conflicts.Overlaps(prerequisites))
        {
            throw new ArgumentException($"协议 '{protocol.Id}' 的同一协议不能同时作为冲突与前置条件。");
        }

        foreach (string prerequisiteId in prerequisites)
        {
            ProtocolDefinition prerequisite = protocolsById[prerequisiteId];
            if (prerequisite.ConflictIds.Contains(protocol.Id) || conflicts.Contains(prerequisiteId))
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 的前置条件 '{prerequisiteId}' 与其冲突，无法被满足。");
            }
        }
    }

    private static void ValidateTagRelations(
        ProtocolDefinition protocol,
        IEnumerable<ProtocolDefinition> allProtocols)
    {
        foreach (string requiredTag in protocol.RequiredTags)
        {
            bool providedByAnotherProtocol = allProtocols.Any(other =>
                !ReferenceEquals(other, protocol) && other.Tags.Contains(requiredTag));
            if (!providedByAnotherProtocol)
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 的需求标签 '{requiredTag}' 无法由其他协议提供。");
            }
        }
    }

    private static HashSet<string> ValidateTags(
        ProtocolDefinition protocol,
        Godot.Collections.Array<string> tags,
        string tagKind)
    {
        if (tags is null)
        {
            throw new ArgumentException($"协议 '{protocol.Id}' 的{tagKind}标签集合不能为空引用。");
        }

        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (string tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag) || !string.Equals(tag, tag.Trim(), StringComparison.Ordinal) || !result.Add(tag))
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 包含空白或重复的{tagKind}标签。");
            }
        }

        return result;
    }

    private static HashSet<string> ValidateRelationIds(
        ProtocolDefinition protocol,
        Godot.Collections.Array<string> relationIds,
        string relationName,
        IReadOnlyDictionary<string, ProtocolDefinition> protocolsById)
    {
        HashSet<string> result = new(StringComparer.Ordinal);
        if (relationIds is null)
        {
            return result;
        }

        foreach (string relationId in relationIds)
        {
            if (string.IsNullOrWhiteSpace(relationId) || !string.Equals(relationId, relationId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 包含无效{relationName} Id。");
            }

            if (string.Equals(protocol.Id, relationId, StringComparison.Ordinal) || !result.Add(relationId))
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 包含自身或重复的{relationName} Id '{relationId}'。");
            }

            if (!protocolsById.ContainsKey(relationId))
            {
                throw new ArgumentException($"协议 '{protocol.Id}' 的{relationName} Id '{relationId}' 不存在于目录中。");
            }
        }

        return result;
    }

    private static void ValidatePrerequisiteGraph(IReadOnlyDictionary<string, ProtocolDefinition> protocolsById)
    {
        Dictionary<string, VisitState> states = protocolsById.Keys.ToDictionary(id => id, _ => VisitState.Unvisited, StringComparer.Ordinal);
        foreach (string protocolId in protocolsById.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            Visit(protocolId, protocolsById, states);
        }
    }

    private static void Visit(
        string protocolId,
        IReadOnlyDictionary<string, ProtocolDefinition> protocolsById,
        IDictionary<string, VisitState> states)
    {
        if (states[protocolId] == VisitState.Visiting)
        {
            throw new ArgumentException($"协议前置条件存在循环，包含 '{protocolId}'。");
        }

        if (states[protocolId] == VisitState.Visited)
        {
            return;
        }

        states[protocolId] = VisitState.Visiting;
        foreach (string prerequisiteId in protocolsById[protocolId].PrerequisiteIds)
        {
            Visit(prerequisiteId, protocolsById, states);
        }

        states[protocolId] = VisitState.Visited;
    }

    private enum VisitState
    {
        Unvisited,
        Visiting,
        Visited
    }
}
