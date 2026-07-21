using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>固定候选池、稳定排序与叠层上限共同保证升级不会重复或产生死选项。</summary>
public sealed class ControlledStatOfferGenerator
{
    private static readonly IReadOnlyList<StatUpgradeOffer> Definitions =
    [
        new(StatUpgradeId.ArmorMax, "强化装甲 +20", new StatModifier(StatId.ArmorMax, 20f, 0f, 0f, "level_armor"), 3),
        new(StatUpgradeId.MoveSpeed, "履带增压 +10%", new StatModifier(StatId.MoveSpeed, 0f, .10f, 0f, "level_move"), 5),
        new(StatUpgradeId.Damage, "炮弹增幅 +2", new StatModifier(StatId.Damage, 2f, 0f, 0f, "level_damage"), 5),
        new(StatUpgradeId.FireCooldown, "装填优化 -8%", new StatModifier(StatId.FireCooldown, 0f, -.08f, 0f, "level_fire"), 5),
        new(StatUpgradeId.DashCooldown, "冲刺冷却 -10%", new StatModifier(StatId.DashCooldown, 0f, -.10f, 0f, "level_dash"), 3)
    ];

    public IReadOnlyList<StatUpgradeOffer> Generate(int seed, int level, IReadOnlyDictionary<StatUpgradeId, int> stacks)
    {
        if (level <= 1) throw new ArgumentOutOfRangeException(nameof(level));
        if (stacks is null) throw new ArgumentNullException(nameof(stacks));
        List<StatUpgradeOffer> available = Definitions.Where(offer => !stacks.TryGetValue(offer.Id, out int count) || count < offer.StackLimit)
            .OrderBy(offer => StableScore(seed, level, offer.Id)).ThenBy(offer => offer.Id).Take(3).ToList();
        if (available.Count != 3) throw new InvalidOperationException("可用基础属性不足三个，不能生成升级面板。");
        return available;
    }

    public static StatUpgradeOffer Get(StatUpgradeId id) => Definitions.Single(offer => offer.Id == id);

    private static uint StableScore(int seed, int level, StatUpgradeId id) => unchecked((uint)(seed * 1103515245 + level * 12345 + (int)id * 265443576));
}
