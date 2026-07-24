using System;
using Godot;

namespace Game1;

/// <summary>仅调试构建启用的运行信息面板；F8 显示/隐藏，不占用用户已声明冲突的 F1。</summary>
public partial class DebugOverlay : CanvasLayer
{
    private Label _label = null!;
    private RunState _runState = null!;
    private SaveData _saveData = null!;
    private Func<RunPhase> _phaseProvider = null!;

    public override void _Ready()
    {
        Layer = 80;
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        SetProcessInput(OS.IsDebugBuild());

        Panel panel = new()
        {
            Position = new Vector2(318, 84),
            Size = new Vector2(154, 104),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(panel);
        _label = new Label
        {
            Position = new Vector2(8, 6),
            Size = new Vector2(140, 92),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _label.AddThemeFontSizeOverride("font_size", 9);
        panel.AddChild(_label);
    }

    public void Bind(RunState runState, SaveData saveData, Func<RunPhase> phaseProvider)
    {
        _runState = runState ?? throw new ArgumentNullException(nameof(runState));
        _saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        _phaseProvider = phaseProvider ?? throw new ArgumentNullException(nameof(phaseProvider));
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!OS.IsDebugBuild() || inputEvent is not InputEventKey key
            || key.Keycode != Key.F8 || !key.Pressed || key.Echo)
        {
            return;
        }

        Visible = !Visible;
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!Visible || _runState is null || _phaseProvider is null) return;
        string lastRun = string.IsNullOrWhiteSpace(_saveData.LastRun.Result)
            ? "无"
            : $"{_saveData.LastRun.Result} / {_saveData.LastRun.Seed}";
        _label.Text = $"DEBUG  F8 关闭\nFPS  {Engine.GetFramesPerSecond()}\n敌军  {GetTree().GetNodesInGroup("enemies").Count}\n敌弹  {GetTree().GetNodesInGroup("enemy_projectiles").Count}\nSeed  {_runState.Seed}\n竞技场  {_runState.ArenaIndex + 1} / 波次 {_runState.WaveIndex + 1} / {_phaseProvider()}\n上局  {lastRun}";
    }
}
