using System;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>在已初始化的 Godot 运行时验证核心基础属性只能通过 BuildController 注入。</summary>
public partial class CoreBuildTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            RunState state = RunState.CreateNew(seed: 31);
            BuildController build = new(state, CreateCatalog());
            build.ApplyCore(CoreCatalog.CreateDefault().Get(CoreId.BreakthroughCannon));

            Assert(state.SelectedCore == CoreId.BreakthroughCannon, "核心选择必须写入单局状态。");
            Assert(Mathf.IsEqualApprox(build.EvaluateStat(StatId.Damage, 10f), 14f), "核心伤害修正必须经过统一属性管线。");
            AssertThrows(
                () => build.ApplyCore(CoreCatalog.CreateDefault().Get(CoreId.ElectricRider)),
                "核心不能在同一局中重复更换。");

            build.SelectProtocol("core_test_protocol");
            Assert(state.GetProtocolRank("core_test_protocol") == ProtocolRank.MkI, "首次获得协议必须成为 Mk.I。");
            build.SelectProtocol("core_test_protocol");
            Assert(state.GetProtocolRank("core_test_protocol") == ProtocolRank.MkII, "重复获得同一协议必须升级到 Mk.II，而不是被旧叠层规则拒绝。");

            BuildRouteCatalog routeCatalog = BuildRouteCatalog.CreateDefault();
            ContentCatalog productionCatalog = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
            foreach (CoreDefinition core in CoreCatalog.CreateDefault().Definitions)
            {
                BuildRouteDefinition[] routes = routeCatalog.GetRoutes(core.Id).ToArray();
                Assert(routes.Length == 3, $"{core.DisplayName} 必须恰好拥有三条构筑路线。");
                Assert(routes.Select(route => route.Tag).Distinct(StringComparer.Ordinal).Count() == 3,
                    $"{core.DisplayName} 的三条路线必须使用唯一标签。");
                Assert(core.BuildTags.OrderBy(tag => tag, StringComparer.Ordinal)
                        .SequenceEqual(routes.Select(route => route.Tag).OrderBy(tag => tag, StringComparer.Ordinal)),
                    $"{core.DisplayName} 的核心标签必须与路线目录完全一致。");
                foreach (BuildRouteDefinition route in routes)
                {
                    int normalSupport = productionCatalog.Protocols.Count(protocol =>
                        protocol.Rarity == 1 && protocol.Tags.Contains(route.Tag));
                    bool capstoneSupport = productionCatalog.Protocols.Any(protocol =>
                        protocol.Rarity > 1 && protocol.Tags.Contains(route.Tag)) ||
                        productionCatalog.Auxiliaries.Any(auxiliary => auxiliary.BuildTags.Contains(route.Tag));
                    Assert(normalSupport >= 2, $"{route.DisplayName} 至少需要两个普通协议支撑。");
                    Assert(capstoneSupport, $"{route.DisplayName} 至少需要一个稀有协议或辅助支撑。");
                }
            }

            GD.Print("[PASS] core_build_pipeline");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] core_build_pipeline: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static ContentCatalog CreateCatalog()
    {
        ProtocolEffectDefinition effect = new()
        {
            EffectId = "core_test_effect",
            Stat = StatId.Damage,
            FlatAdd = 1f
        };
        ProtocolDefinition protocol = new()
        {
            Id = "core_test_protocol",
            DisplayName = "核心测试协议",
            Description = "只用于验证核心属性管线。",
            Department = ProtocolDepartment.Arsenal,
            Rarity = 1,
            BaseWeight = 1f,
            StackLimit = 1,
            Effects = new Godot.Collections.Array<ProtocolEffectDefinition> { effect }
        };
        ContentCatalog catalog = new()
        {
            Version = "core-test-v1",
            Protocols = new Godot.Collections.Array<ProtocolDefinition> { protocol }
        };
        AddRequiredAuxiliaries(catalog);
        AddRequiredEnemies(catalog);
        return catalog;
    }

    /// <summary>测试目录也必须满足首区四辅助内容合同，避免绕过生产校验。</summary>
    private static void AddRequiredAuxiliaries(ContentCatalog catalog)
    {
        for (int index = 0; index < 4; index++)
        {
            catalog.Auxiliaries.Add(new AuxiliaryDefinition
            {
                Id = $"core_test_auxiliary_{index}",
                DisplayName = $"测试辅助 {index}",
                Description = "仅用于构筑管线测试目录校验。",
                TargetMode = AuxiliaryTargetMode.Nearest,
                BaseCooldown = 1f,
                MaximumRank = 3,
                BaseDamage = 1,
                Range = 100f
            });
        }
    }

    private static void AddRequiredEnemies(ContentCatalog catalog)
    {
        ContentCatalog production = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
        foreach (EnemyDefinition enemy in production.Enemies) catalog.Enemies.Add(enemy);
        foreach (EliteModifierDefinition modifier in production.EliteModifiers) catalog.EliteModifiers.Add(modifier);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }
}
