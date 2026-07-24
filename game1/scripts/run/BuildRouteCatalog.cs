using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>封锁城区三核心九路线的稳定目录。</summary>
public sealed class BuildRouteCatalog
{
    private readonly IReadOnlyDictionary<CoreId, IReadOnlyList<BuildRouteDefinition>> _byCore;

    private BuildRouteCatalog(IReadOnlyList<BuildRouteDefinition> routes)
    {
        if (routes is null || routes.Count != 9)
            throw new ArgumentException("BC-02 路线目录必须恰好包含九条路线。", nameof(routes));
        if (routes.Any(route => route is null || string.IsNullOrWhiteSpace(route.Id) ||
                                string.IsNullOrWhiteSpace(route.DisplayName) || string.IsNullOrWhiteSpace(route.Tag)))
            throw new ArgumentException("路线目录不得包含空定义或空字段。", nameof(routes));
        if (routes.Select(route => route.Id).Distinct(StringComparer.Ordinal).Count() != routes.Count)
            throw new ArgumentException("路线 Id 必须全局唯一。", nameof(routes));

        _byCore = routes
            .GroupBy(route => route.CoreId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<BuildRouteDefinition>)group.ToArray());
        foreach (CoreId coreId in Enum.GetValues<CoreId>())
        {
            if (!_byCore.TryGetValue(coreId, out IReadOnlyList<BuildRouteDefinition> coreRoutes) ||
                coreRoutes.Count != 3 ||
                coreRoutes.Select(route => route.Tag).Distinct(StringComparer.Ordinal).Count() != 3)
                throw new ArgumentException($"核心 {coreId} 必须恰好包含三个唯一路线标签。", nameof(routes));
        }
    }

    public IReadOnlyList<BuildRouteDefinition> GetRoutes(CoreId coreId) =>
        _byCore.TryGetValue(coreId, out IReadOnlyList<BuildRouteDefinition> routes)
            ? routes
            : throw new ArgumentOutOfRangeException(nameof(coreId));

    public static BuildRouteCatalog CreateDefault() => new(new[]
    {
        new BuildRouteDefinition(CoreId.BreakthroughCannon, "breakthrough_ricochet", "跳弹火网", "ricochet"),
        new BuildRouteDefinition(CoreId.BreakthroughCannon, "breakthrough_penetration", "破甲直击", "penetration"),
        new BuildRouteDefinition(CoreId.BreakthroughCannon, "breakthrough_impact", "分裂重击", "impact"),
        new BuildRouteDefinition(CoreId.OverdriveAutocannon, "overdrive_rapid_fire", "高频弹幕", "rapid_fire"),
        new BuildRouteDefinition(CoreId.OverdriveAutocannon, "overdrive_on_hit", "命中触发", "on_hit"),
        new BuildRouteDefinition(CoreId.OverdriveAutocannon, "overdrive_auxiliary", "自动武装", "auxiliary"),
        new BuildRouteDefinition(CoreId.ElectricRider, "electric_dash", "冲刺电轨", "dash"),
        new BuildRouteDefinition(CoreId.ElectricRider, "electric_mobility", "环场机动", "mobility"),
        new BuildRouteDefinition(CoreId.ElectricRider, "electric_area", "近身压制", "area")
    });
}
