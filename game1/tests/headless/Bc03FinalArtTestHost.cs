using System;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>BC-03 资源门禁：验证正式素材可加载、关键场景已换图、卡片具有语义图标。</summary>
public partial class Bc03FinalArtTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            AssertSequence(ArtTextureCatalog.PlayerProjectile, "玩家弹道");
            AssertSequence(ArtTextureCatalog.EnemyProjectile, "敌军弹道");
            AssertSequence(ArtTextureCatalog.MuzzleFlash, "炮口焰");
            AssertSequence(ArtTextureCatalog.PlayerHit, "玩家受击");
            AssertSequence(ArtTextureCatalog.Reboot, "重启");
            AssertSequence(ArtTextureCatalog.LevelUp, "升级");
            AssertSequence(ArtTextureCatalog.MortarWarning, "迫击预警");
            AssertSequence(ArtTextureCatalog.BarrierWarning, "路障预警");
            AssertSequence(ArtTextureCatalog.ChargeWarning, "冲锋预警");
            AssertSequence(ArtTextureCatalog.BossPhase, "Boss 阶段");
            AssertSequence(ArtTextureCatalog.BossDeath, "Boss 击毁");

            PackedScene playerScene = GD.Load<PackedScene>("res://scenes/actors/player_tank.tscn");
            PlayerTank player = playerScene.Instantiate<PlayerTank>();
            AddChild(player);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Assert(player.GetNode<Sprite2D>("BodyVisual").Texture.ResourcePath.EndsWith("player_hull.png"),
                "玩家车体必须引用 Batch 08 正式底盘。");
            Assert(player.GetNode<Sprite2D>("Turret/TurretVisual").Texture.ResourcePath.EndsWith("player_turret.png"),
                "玩家炮塔必须引用 Batch 08 独立炮塔。");
            player.SetCoreVisual(CoreId.ElectricRider);
            Assert(player.GetNode<Sprite2D>("CoreVisual").Texture.ResourcePath.EndsWith("core_electric.png"),
                "选择核心必须实时替换坦克中央模块。");

            EnemyDefinition scout = GD.Load<EnemyDefinition>("res://resources/enemies/scout_drone.tres");
            Assert(scout.Texture.ResourcePath.EndsWith("scout_drone.png"),
                "侦察单位不得继续复用巡逻坦克贴图。");

            CoreSelectionPanel panel = new();
            AddChild(panel);
            panel.ShowChoices(CoreCatalog.CreateDefault());
            Assert(panel.FindChild("SemanticIcon", true, false) is TextureRect,
                "核心卡片必须包含正式语义图标。");

            player.QueueFree();
            panel.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("[PASS] bc03_final_art");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] bc03_final_art: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void AssertSequence(Texture2D[] textures, string name)
    {
        Assert(textures.Length == 4 && textures.All(texture => texture is not null),
            $"{name}必须包含四张可加载运行帧。");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
