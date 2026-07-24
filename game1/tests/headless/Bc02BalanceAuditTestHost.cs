using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>BC-02 数据审计：覆盖三核心多种子、四敌军、五波组合和单一精英规则。</summary>
public partial class Bc02BalanceAuditTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            ContentCatalog catalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
            ArenaDefinition arena = GD.Load<ArenaDefinition>("res://resources/arenas/blockade_city_arena.tres");
            catalog.Validate();
            arena.Validate();
            Assert(catalog.Protocols.Count(protocol => protocol.Rarity == 1) == 16,
                "封锁城区必须保持 16 个普通协议。");
            Assert(catalog.Protocols.Count(protocol => protocol.Rarity > 1) == 4,
                "封锁城区必须保持 4 个稀有协议。");

            RewardGenerator generator = new();
            BuildRouteCatalog routes = BuildRouteCatalog.CreateDefault();
            foreach (CoreDefinition core in CoreCatalog.CreateDefault().Definitions)
            {
                HashSet<string> observedRouteTags = new(StringComparer.Ordinal);
                bool observedOffCoreCandidate = false;
                HashSet<string> coreTags = core.BuildTags.ToHashSet(StringComparer.Ordinal);
                for (int seed = 1; seed <= 24; seed++)
                {
                    RewardGenerationInput input = new(
                        seed * 7919,
                        seed % 5,
                        Array.Empty<string>(),
                        catalog.Version,
                        RewardKind: RewardKind.NormalProtocol,
                        SelectedCore: core.Id);
                    ProtocolOffer first = generator.Generate(input, catalog);
                    ProtocolOffer repeated = generator.Generate(input, catalog);
                    Assert(first.ProtocolIds.Count == 3 && first.ProtocolIds.Distinct(StringComparer.Ordinal).Count() == 3,
                        $"{core.DisplayName} 的每个固定种子必须稳定提供三个唯一候选。");
                    Assert(first.ProtocolIds.SequenceEqual(repeated.ProtocolIds),
                        $"{core.DisplayName} 的同输入奖励必须确定性重复。");
                    foreach (ProtocolDefinition protocol in first.ProtocolIds.Select(catalog.GetProtocol))
                    {
                        foreach (string tag in protocol.Tags.Where(coreTags.Contains)) observedRouteTags.Add(tag);
                        if (!protocol.Tags.Any(coreTags.Contains)) observedOffCoreCandidate = true;
                    }
                }
                string[] expectedTags = routes.GetRoutes(core.Id).Select(route => route.Tag).ToArray();
                Assert(expectedTags.All(observedRouteTags.Contains),
                    $"{core.DisplayName} 的 24 种子样本必须覆盖三条路线。");
                Assert(observedOffCoreCandidate,
                    $"{core.DisplayName} 必须保留跨路线/跨核心混搭候选，不能变成锁池。");
            }

            Assert(catalog.Enemies.Select(enemy => enemy.Behavior).Distinct().Count() == 4,
                "四敌军职责必须保持唯一。");
            Assert(arena.Waves[0].Behaviors.Count == 2 && arena.Waves[2].Behaviors.Contains(BehaviorId.Mortar) &&
                   arena.Waves[4].Behaviors.Distinct().Count() == 4,
                "波次压力必须从教学双职责逐步发展到四职责组合。");
            Assert(arena.Waves.Take(4).All(wave => !wave.IncludesElite) &&
                   arena.Waves[4].IncludesElite && arena.Waves[4].EliteModifier.ArmorMultiplier == 1f,
                "只有第五波可出现唯一精英，且不得通过生命膨胀制造难度。");

            GD.Print("[PASS] bc02_balance_audit");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] bc02_balance_audit: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
