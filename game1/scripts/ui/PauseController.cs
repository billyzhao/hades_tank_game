using Godot;

namespace Game1;

/// <summary>失焦自动暂停；重新获得焦点后仍需玩家明确按 Esc 才恢复。</summary>
public partial class PauseController : CanvasLayer
{
    private Control _overlay = null!;
    private PauseCoordinator _coordinator = null!;

    public void Configure(PauseCoordinator coordinator) =>
        _coordinator = coordinator ?? throw new System.ArgumentNullException(nameof(coordinator));

    public override void _Ready()
    {
        if (_coordinator is null)
            throw new System.InvalidOperationException("PauseController 必须先注入 PauseCoordinator。");

        Layer = 100;
        ProcessMode = ProcessModeEnum.Always;
        _overlay = new ColorRect
        {
            Color = new Color(0.02f, 0.025f, 0.035f, 0.82f),
            Position = Vector2.Zero,
            Size = new Vector2(480, 270),
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false
        };
        AddChild(_overlay);
        Label label = new()
        {
            Text = "战术暂停\n\n按 Esc 继续战斗",
            Position = new Vector2(170, 102),
            Size = new Vector2(160, 70),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 14);
        _overlay.AddChild(label);
        _coordinator.PauseChanged += OnPauseChanged;
        OnPauseChanged(_coordinator.IsPaused);
    }

    public override void _Process(double delta)
    {
        if (!Input.IsActionJustPressed("pause")) return;
        if (_coordinator.Contains(PauseReason.Manual)) _coordinator.Release(PauseReason.Manual);
        else _coordinator.Acquire(PauseReason.Manual);
    }

    public override void _Notification(int what)
    {
        if (_coordinator is null || !IsInsideTree()) return;
        if (what == NotificationApplicationFocusOut) _coordinator.Acquire(PauseReason.FocusLost);
        else if (what == NotificationApplicationFocusIn) _coordinator.Release(PauseReason.FocusLost);
    }

    private void OnPauseChanged(bool paused)
    {
        // 升级暂停冻结战斗，但三选一面板必须保持可见并可输入；
        // 手动/失焦暂停仍由通用遮罩表达。
        if (_overlay is not null) _overlay.Visible = paused && !_coordinator.Contains(PauseReason.LevelUp);
    }

    public override void _ExitTree()
    {
        if (_coordinator is not null) _coordinator.PauseChanged -= OnPauseChanged;
    }
}
