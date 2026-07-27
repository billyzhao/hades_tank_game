using Godot;
using System;

namespace Game1;

/// <summary>失焦自动暂停；重新获得焦点后仍需玩家明确按 Esc 才恢复。</summary>
public partial class PauseController : CanvasLayer
{
    private static Texture2D PanelTexture =>
        GD.Load<Texture2D>("res://assets/sprites/ui/reward_card_frame.png");
    private Control _overlay = null!;
    private PauseCoordinator _coordinator = null!;
    public event Action<bool> ManualPauseChanged = delegate { };

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
        TextureRect frame = new()
        {
            Name = "PauseFrame",
            Texture = PanelTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Position = new Vector2(145, 76),
            Size = new Vector2(190, 116),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _overlay.AddChild(frame);
        IndustrialUiSkin.ApplyCornerIcon(frame, ArtTextureCatalog.WaveIcon, 22f);
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
        if (_coordinator.Contains(PauseReason.StartScreen) ||
            _coordinator.Contains(PauseReason.CoreSelection) ||
            _coordinator.Contains(PauseReason.LevelUp) ||
            _coordinator.Contains(PauseReason.RunResult))
            return;

        if (_coordinator.Contains(PauseReason.Manual))
        {
            _coordinator.Release(PauseReason.Manual);
            ManualPauseChanged(false);
        }
        else
        {
            _coordinator.Acquire(PauseReason.Manual);
            ManualPauseChanged(true);
        }
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
        if (_overlay is not null)
        {
            bool selectionUiOwnsPause = _coordinator.Contains(PauseReason.LevelUp) ||
                                        _coordinator.Contains(PauseReason.CoreSelection) ||
                                        _coordinator.Contains(PauseReason.StartScreen) ||
                                        _coordinator.Contains(PauseReason.RunResult);
            _overlay.Visible = paused && !selectionUiOwnsPause;
        }
    }

    public override void _ExitTree()
    {
        if (_coordinator is not null) _coordinator.PauseChanged -= OnPauseChanged;
    }
}
