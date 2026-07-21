using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>固定三选一的波间奖励候选；生成器负责资格与权重，此类型只守住 UI 合同。</summary>
public sealed record RewardOffer
{
    public RewardOffer(RewardKind kind, IReadOnlyList<RewardChoice> choices)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (choices is null || choices.Count != 3 || choices.Any(choice => choice is null))
            throw new ArgumentException("奖励候选必须恰好包含三项非空选择。", nameof(choices));
        if (choices.Select(choice => choice.Id).Distinct(StringComparer.Ordinal).Count() != 3)
            throw new ArgumentException("奖励候选不得包含重复 Id。", nameof(choices));

        Kind = kind;
        Choices = choices.ToArray();
    }

    public RewardKind Kind { get; }
    public IReadOnlyList<RewardChoice> Choices { get; }
}
