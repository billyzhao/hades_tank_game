using Godot;

namespace Game1;

/// <summary>封锁城区交付版标题入口；只发出开始或退出请求，不直接控制单局状态。</summary>
public partial class StartScreen : Control
{
    [Signal] public delegate void StartRequestedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();

    public override void _Ready()
    {
        Name = "StartScreen";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Stop;

        ColorRect shade = new()
        {
            Name = "Shade",
            Color = new Color(0.035f, 0.025f, 0.018f, 0.94f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(shade);

        VBoxContainer panel = new()
        {
            Name = "Panel",
            Position = new Vector2(122f, 48f),
            Size = new Vector2(236f, 174f),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        AddChild(panel);

        Label title = new()
        {
            Name = "TitleLabel",
            Text = "废土中继",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", new Color("#f4b942"));
        panel.AddChild(title);

        Label subtitle = new()
        {
            Name = "SubtitleLabel",
            Text = "封锁城区 · 单竞技场交付版",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitle.AddThemeFontSizeOverride("font_size", 13);
        panel.AddChild(subtitle);

        Label description = new()
        {
            Name = "DescriptionLabel",
            Text = "选择移动核心，突破五波封锁并击毁路障指挥车",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        panel.AddChild(description);

        Button start = new()
        {
            Name = "StartButton",
            Text = "进入封锁城区",
            FocusMode = FocusModeEnum.All
        };
        start.Pressed += () => EmitSignal(SignalName.StartRequested);
        panel.AddChild(start);

        Button quit = new()
        {
            Name = "QuitButton",
            Text = "退出游戏",
            FocusMode = FocusModeEnum.All
        };
        quit.Pressed += () => EmitSignal(SignalName.QuitRequested);
        panel.AddChild(quit);

        start.FocusNeighborBottom = start.GetPathTo(quit);
        quit.FocusNeighborTop = quit.GetPathTo(start);
        start.GrabFocus();
    }

    public void Dismiss() => Visible = false;
}
