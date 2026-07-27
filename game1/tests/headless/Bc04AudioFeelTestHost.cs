using System;
using System.Linq;
using Godot;

namespace Game1.Tests.Headless;

/// <summary>BC-04 门禁：验证正式音频、总线、低装甲提示和分级相机反馈均接入真实竞技场。</summary>
public partial class Bc04AudioFeelTestHost : Node
{
    public override async void _Ready()
    {
        try
        {
            string[] paths = AudioCueCatalog.RequiredPaths().ToArray();
            Assert(paths.Length == 34, "Batch 09 必须登记 34 个运行音频文件。");
            foreach (string path in paths)
                Assert(ResourceLoader.Exists(path), $"缺少可加载音频：{path}");

            PackedScene roomScene = GD.Load<PackedScene>("res://scenes/rooms/mvp_combat_room.tscn");
            Node2D room = roomScene.Instantiate<Node2D>();
            AddChild(room);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            AudioFeedbackController audio = room.GetNode<AudioFeedbackController>("AudioFeedbackController");
            Assert(audio.LoadedCueCount == Enum.GetValues<AudioCue>().Length,
                "音频控制器必须加载全部稳定语义。");
            Assert(audio.MusicLayersPlaying, "竞技场基础层与强度层必须同步播放。");
            foreach (string bus in new[] { "Master", "Music", "Ambience", "SFX", "UI" })
                Assert(AudioServer.GetBusIndex(bus) >= 0, $"缺少音频总线：{bus}");

            audio.PlayUiMove();
            audio.PlayUiConfirm();
            audio.PlayUiLevelUp();
            Assert(audio.GetChildCount() > 5, "UI 音效必须通过受控一次性播放器进入总线。");

            PlayerTank player = room.GetNode<PlayerTank>("PlayerTank");
            HealthComponent health = player.GetNode<HealthComponent>("HealthComponent");
            VisualFeedbackController visual = room.GetNode<VisualFeedbackController>("VisualFeedbackController");
            health.SetArmor(30);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Assert(visual.LowArmorWarningVisible, "30% 装甲必须显示不依赖日志的危险边框。");
            health.SetArmor(80);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Assert(!visual.LowArmorWarningVisible, "脱离低装甲阈值后危险边框必须消失。");

            CameraShakeController cameraFeedback = room.GetNode<CameraShakeController>("CameraShakeController");
            Camera2D camera = room.GetNode<Camera2D>("Camera2D");
            cameraFeedback.Trigger(FeedbackTier.Medium);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Assert(camera.Offset.Length() > 0f, "中等反馈必须只通过 Camera2D.Offset 产生可见震动。");
            Assert(player.GlobalPosition == new Vector2(240f, 184f),
                "震动不得修改玩家或物理世界坐标。");
            await ToSignal(GetTree().CreateTimer(.25d), SceneTreeTimer.SignalName.Timeout);
            Assert(camera.Offset.Length() < .05f, "震动必须在短时衰减后回到静止。");

            room.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("[PASS] bc04_audio_feel");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[FAIL] bc04_audio_feel: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
