using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>Alpha 02E 三核心的静态目录；数值只表达初始操作节奏，协议路线仍可自由混搭。</summary>
public sealed class CoreCatalog
{
    private readonly IReadOnlyDictionary<CoreId, CoreDefinition> _byId;

    private CoreCatalog(IReadOnlyList<CoreDefinition> definitions)
    {
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        if (Definitions.Count != 3 || Definitions.Select(definition => definition.Id).Distinct().Count() != 3)
            throw new ArgumentException("核心目录必须恰好包含三个唯一核心。", nameof(definitions));
        _byId = Definitions.ToDictionary(definition => definition.Id);
    }

    public IReadOnlyList<CoreDefinition> Definitions { get; }

    public CoreDefinition Get(CoreId id) => _byId.TryGetValue(id, out CoreDefinition definition)
        ? definition
        : throw new ArgumentOutOfRangeException(nameof(id));

    public static CoreCatalog CreateDefault() => new(new CoreDefinition[]
    {
        new(
            CoreId.BreakthroughCannon,
            "突破重炮核心",
            "高单发、低射速：优先建立反弹、爆破或穿甲路线。",
            new[] { "artillery", "impact" },
            new[]
            {
                new StatModifier(StatId.Damage, 4f, 0f, 0f, "core_breakthrough"),
                new StatModifier(StatId.FireCooldown, 0f, 0.18f, 0f, "core_breakthrough")
            }),
        new(
            CoreId.OverdriveAutocannon,
            "过载速射核心",
            "高射速、低单发：优先建立命中触发与弹幕路线。",
            new[] { "rapid_fire", "on_hit" },
            new[]
            {
                new StatModifier(StatId.Damage, -1f, 0f, 0f, "core_overdrive"),
                new StatModifier(StatId.FireCooldown, 0f, -0.25f, 0f, "core_overdrive")
            }),
        new(
            CoreId.ElectricRider,
            "电驱游骑核心",
            "标准火炮、强化位移：优先建立冲刺、电场或侧翼路线。",
            new[] { "dash", "mobility" },
            new[]
            {
                new StatModifier(StatId.MoveSpeed, 0f, 0.10f, 0f, "core_electric"),
                new StatModifier(StatId.DashCooldown, 0f, -0.18f, 0f, "core_electric")
            })
    });
}
