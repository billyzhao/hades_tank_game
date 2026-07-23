using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>波间或 Boss 奖励卡的只读显示与选择事实；具体应用仍由 RewardController 负责。</summary>
public sealed record RewardChoice
{
    public RewardChoice(string id, string displayName, string description, IReadOnlyList<string> tags, bool isAuxiliary = false)
    {
        if (string.IsNullOrWhiteSpace(id) || !string.Equals(id, id.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("奖励 Id 必须为非空稳定标识。", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("奖励必须提供名称与说明。");
        if (tags is null || tags.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("奖励标签不得为空。", nameof(tags));

        Id = id;
        DisplayName = displayName;
        Description = description;
        Tags = tags.ToArray();
        IsAuxiliary = isAuxiliary;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public IReadOnlyList<string> Tags { get; }
    public bool IsAuxiliary { get; }
}
