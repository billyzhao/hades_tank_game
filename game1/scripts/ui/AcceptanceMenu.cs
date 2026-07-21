using Godot;

namespace Game1;

/// <summary>Debug 构建中的策划可见验收入口；只发出正式操作请求，不直接修改单局私有状态。</summary>
public partial class AcceptanceMenu : Control
{
    [Signal] public delegate void DamageRequestedEventHandler(int amount);
    [Signal] public delegate void ArmorPercentRequestedEventHandler(int percent);
    [Signal] public delegate void DefeatRequestedEventHandler();
    [Signal] public delegate void StopWaveSpawningRequestedEventHandler();
    [Signal] public delegate void ClearWaveEnemiesRequestedEventHandler();
    [Signal] public delegate void CompleteWaveRequestedEventHandler();
    [Signal] public delegate void AdvanceWaveRequestedEventHandler();
    [Signal] public delegate void EndRunRequestedEventHandler();
    [Signal] public delegate void ExperienceRequestedEventHandler(int amount);
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
            Text = "竞技场验收",
            Position = new Vector2(390, 6),
            Size = new Vector2(78, 20),
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.All
        };
        entry.AddThemeFontSizeOverride("font_size", 7);
        entry.Pressed += TogglePanel;
        AddChild(entry);

        _panel = new PanelContainer
        {
            Name = "Panel",
            Position = new Vector2(300, 38),
            Size = new Vector2(180, 300),
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false
        };
        AddChild(_panel);

        VBoxContainer content = new();
        _panel.AddChild(content);
        Label title = new() { Text = "移动核心竞技场验收", HorizontalAlignment = HorizontalAlignment.Center };
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
        content.AddChild(CreateButton("授予经验 +100", () => EmitSignal(SignalName.ExperienceRequested, 100)));
        content.AddChild(CreateButton("装甲 -25", () => EmitSignal(SignalName.DamageRequested, 25)));
        content.AddChild(CreateButton("装甲设为 29%（维护保障）", () => EmitSignal(SignalName.ArmorPercentRequested, 29)));
        content.AddChild(CreateButton("触发坦克报废", () => EmitSignal(SignalName.DefeatRequested)));
        content.AddChild(CreateButton("结束刷新（保留残敌）", () => EmitSignal(SignalName.StopWaveSpawningRequested)));
        content.AddChild(CreateButton("敌军全灭（当前波）", () => EmitSignal(SignalName.ClearWaveEnemiesRequested)));
        content.AddChild(CreateButton("结束本轮并结算", () => EmitSignal(SignalName.CompleteWaveRequested)));
        content.AddChild(CreateButton("确认并到下一波", () => EmitSignal(SignalName.AdvanceWaveRequested)));
        content.AddChild(CreateButton("结束本局（验收）", () => EmitSignal(SignalName.EndRunRequested)));
        content.AddChild(CreateButton("进入 Boss 验收", () => EmitSignal(SignalName.BossRequested)));
        content.AddChild(CreateButton("重新开始本局", () => EmitSignal(SignalName.RestartRequested)));
        content.AddChild(new Label
        {
            Text = "前三项用于验证波次门禁；“到下一波”会自动完成本轮并确认奖励。",
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
