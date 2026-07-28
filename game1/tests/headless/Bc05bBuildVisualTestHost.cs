using System;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>
/// BC-05B 构筑表现合同：敌军无底板，协议改变坦克本体，辅助机按正式等级谱系换图。
/// 表现测试只读取构筑快照，不允许绕过 BuildController 修改单局状态。
/// </summary>
public partial class Bc05bBuildVisualTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            ContentCatalog content = GD.Load<ContentCatalog>("res://resources/content_catalog.tres");
            TankBuildVisualCatalog visuals =
                GD.Load<TankBuildVisualCatalog>("res://resources/presentation/tank_build_visual_catalog.tres");
            content.Validate();
            visuals.Validate(content);

            Assert(visuals.ProtocolVisuals.Count == 4, "构筑视觉目录必须覆盖四个协议部门。");
            Assert(visuals.ProtocolVisuals.All(item => item.Texture is not null),
                "每个协议部门都必须具有可加载的坦克模块贴图。");
            foreach (AuxiliaryVisualSet visualSet in visuals.AuxiliaryVisuals)
            {
                string[] paths =
                {
                    visualSet.RankOneTexture.ResourcePath,
                    visualSet.RankTwoTexture.ResourcePath,
                    visualSet.RankThreeTexture.ResourcePath
                };
                Assert(paths.Distinct(StringComparer.Ordinal).Count() == 3,
                    $"{visualSet.AuxiliaryId} 的 Mk.I～Mk.III 必须使用三张独立贴图。");
            }

            PackedScene enemyScene = GD.Load<PackedScene>("res://scenes/actors/enemy_tank.tscn");
            EnemyTank enemy = enemyScene.Instantiate<EnemyTank>();
            AddChild(enemy);
            Assert(enemy.GetNodeOrNull<Polygon2D>("Visual") is null,
                "敌军场景不得保留程序色块或背景底板。");
            Assert(enemy.GetNode<Sprite2D>("RoleSprite") is not null,
                "敌军仍必须保留透明职责贴图节点。");

            RunState state = RunState.CreateNew(20260728);
            BuildController build = new(state, content);
            PackedScene playerScene = GD.Load<PackedScene>("res://scenes/actors/player_tank.tscn");
            PlayerTank player = playerScene.Instantiate<PlayerTank>();
            AddChild(player);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            TankBuildVisualController buildVisual =
                player.GetNode<TankBuildVisualController>("TankBuildVisualController");
            buildVisual.AttachBuild(build, visuals);
            build.SelectProtocol("arsenal_damage");
            build.SelectProtocol("recon_trail");
            build.SelectProtocol("logistics_armor");
            build.SelectProtocol("engineering_sidecar");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            AssertProtocolModule(player, "Turret/ProtocolModule_Arsenal", "protocol_arsenal.png");
            AssertProtocolModule(player, "Turret/ProtocolModule_Recon", "protocol_recon.png");
            AssertProtocolModule(player, "ProtocolModule_Logistics", "protocol_logistics.png");
            AssertProtocolModule(player, "ProtocolModule_Engineering", "protocol_engineering.png");

            AuxiliaryHost auxiliaryHost = player.GetNode<AuxiliaryHost>("AuxiliaryHost");
            auxiliaryHost.AttachBuild(build, content, visuals);
            auxiliaryHost.Activate(player);
            string[] expectedTextures =
            {
                "side_cannon.png",
                "side_cannon_mk2.png",
                "side_cannon_mk3.png"
            };
            for (int rank = 1; rank <= 3; rank++)
            {
                Assert(build.AddOrUpgradeAuxiliary("aux_side_cannon") == rank,
                    $"侧挂速射炮必须升级到 Mk.{rank}。");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Sprite2D auxiliary = auxiliaryHost.GetNode<Sprite2D>("Visual_aux_side_cannon");
                Assert(auxiliary.Texture.ResourcePath.EndsWith(expectedTextures[rank - 1], StringComparison.Ordinal),
                    $"侧挂速射炮 Mk.{rank} 必须切换到对应外观。");
            }

            Vector2 physicalPosition = player.GlobalPosition;
            player.GetNode<TankVisualAnimator>("TankVisualAnimator").PlayHitReaction();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Assert(player.GlobalPosition.IsEqualApprox(physicalPosition),
                "玩家受击反馈不得移动物理坐标或改变碰撞结果。");

            enemy.QueueFree();
            player.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("[PASS] bc05b_build_visuals");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] bc05b_build_visuals: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void AssertProtocolModule(Node player, string nodePath, string expectedFile)
    {
        Sprite2D module = player.GetNode<Sprite2D>(nodePath);
        Assert(module.Texture.ResourcePath.EndsWith(expectedFile, StringComparison.Ordinal),
            $"{nodePath} 必须使用 {expectedFile}。");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
