using Godot;

namespace Game1;

/// <summary>Debug 构建中的策划可见验收入口；只发出正式操作请求，不直接修改单局私有状态。</summary>
public partial class AcceptanceMenu : Control
{
    [Signal] public delegate void DamageRequestedEventHandler(int amount);
    [Signal] public delegate void DefeatRequestedEventHandler();
    [Signal] public delegate void BossRequestedEventHandler();
    [Signal] public delegate void RestartRequestedEventHandler();

    private PanelContainer _panel = null!;
    private Label _status = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = OS.IsDebugBuild();
        if (!Visible) return;

        Button entry = new()
        {
            Name = "EntryButton",
            Text = "生存验收",
            Position = new Vector2(366, 8),
            Size = new Vector2(102, 24),
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.All
        };
        entry.Pressed += TogglePanel;
        AddChild(entry);

        _panel = new PanelContainer
        {
            Name = "Panel",
            Position = new Vector2(300, 38),
            Size = new Vector2(168, 190),
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false
        };
        AddChild(_panel);

        VBoxContainer content = new();
        _panel.AddChild(content);
        Label title = new() { Text = "移动核心生存验收", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 10);
        content.AddChild(title);
        _status = new()
        {
            Text = "装甲与重启状态会显示在这里",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(150, 32)
        };
        _status.AddThemeFontSizeOverride("font_size", 8);
        content.AddChild(_status);
        content.AddChild(CreateButton("装甲 -25", () => EmitSignal(SignalName.DamageRequested, 25)));
        content.AddChild(CreateButton("触发坦克报废", () => EmitSignal(SignalName.DefeatRequested)));
        content.AddChild(CreateButton("进入 Boss 验收", () => EmitSignal(SignalName.BossRequested)));
        content.AddChild(CreateButton("重新开始本局", () => EmitSignal(SignalName.RestartRequested)));
        content.AddChild(new Label
        {
            Text = "第一次报废消耗重启；保护结束后再次报废验证失败。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
    }

    public void SetStatus(string text)
    {
        if (_status is not null) _status.Text = text ?? string.Empty;
    }

    private void TogglePanel()
    {
        _panel.Visible = !_panel.Visible;
        if (_panel.Visible) _panel.GetChild<VBoxContainer>(0).GetChild<Button>(2).GrabFocus();
    }

    private static Button CreateButton(string text, System.Action pressed)
    {
        Button button = new() { Text = text, FocusMode = FocusModeEnum.All };
        button.Pressed += pressed;
        return button;
    }
}
