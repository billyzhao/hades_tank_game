using Godot;

namespace Game1;

/// <summary>失焦自动暂停；重新获得焦点后仍需玩家明确按 Esc 才恢复。</summary>
public partial class PauseController : CanvasLayer
{
    private Control _overlay = null!;

    public override void _Ready()
    {
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
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("pause")) SetPaused(!GetTree().Paused);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && IsInsideTree()) SetPaused(true);
    }

    private void SetPaused(bool paused)
    {
        GetTree().Paused = paused;
        if (_overlay is not null) _overlay.Visible = paused;
    }
}
