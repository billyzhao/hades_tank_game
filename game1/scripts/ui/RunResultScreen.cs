using Godot;

namespace Game1;

/// <summary>Boss 胜利结果层；仅发出操作请求，由 AppRoot 决定如何重开或返回。</summary>
public partial class RunResultScreen : Control
{
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void ReturnRequestedEventHandler();

    private Label _summary = null!;
    private Label _title = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Stop;
        ColorRect shade = new()
        {
            Name = "Shade",
            Color = new Color(.02f, .03f, .06f, .88f),
            AnchorRight = 1f,
            AnchorBottom = 1f
        };
        AddChild(shade);
        VBoxContainer panel = new()
        {
            Name = "Panel",
            Position = new Vector2(118, 54),
            Size = new Vector2(245, 162)
        };
        shade.AddChild(panel);
        _title = new Label { Text = "封锁城区突破", HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 20);
        panel.AddChild(_title);
        _summary = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        panel.AddChild(_summary);
        Button retry = new()
        {
            Name = "RetryButton",
            Text = "重试本局",
            TooltipText = "从核心选择重新开始"
        };
        retry.Pressed += () => EmitSignal(SignalName.RetryRequested);
        panel.AddChild(retry);
        Button back = new() { Name = "ReturnButton", Text = "返回整备" };
        back.Pressed += () => EmitSignal(SignalName.ReturnRequested);
        panel.AddChild(back);
        Visible = false;
    }

    public void ShowResult(RunResultSnapshot snapshot, bool victory = true)
    {
        _title.Text = victory ? "封锁城区突破" : "坦克已经报废";
        string protocols = snapshot.ProtocolIds.Count == 0 ? "无" : string.Join("、", snapshot.ProtocolIds);
        string core = string.IsNullOrWhiteSpace(snapshot.CoreId) ? "未选择" : snapshot.CoreId;
        _summary.Text = $"种子：{snapshot.Seed}\n核心：{core}\n协议：{protocols}\n区域：封锁城区  等级：{snapshot.Level}\n耗时：{snapshot.Elapsed.Minutes:00}:{snapshot.Elapsed.Seconds:00}";
        Visible = true;
        GetNode<Button>("Shade/Panel/RetryButton").GrabFocus();
    }

    public void HideResult() => Visible = false;
}
