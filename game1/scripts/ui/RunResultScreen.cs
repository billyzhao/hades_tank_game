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
        MouseFilter = MouseFilterEnum.Stop;
        ColorRect shade = new() { Color = new Color(.02f, .03f, .06f, .88f), AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(shade);
        VBoxContainer panel = new() { Position = new Vector2(118, 70), Size = new Vector2(245, 130) };
        shade.AddChild(panel);
        _title = new Label { Text = "中继站守住了", HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 20);
        panel.AddChild(_title);
        _summary = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        panel.AddChild(_summary);
        Button retry = new() { Text = "重试本局", TooltipText = "从第一间战斗房重新开始" };
        retry.Pressed += () => EmitSignal(SignalName.RetryRequested);
        panel.AddChild(retry);
        Button back = new() { Text = "返回基地" };
        back.Pressed += () => EmitSignal(SignalName.ReturnRequested);
        panel.AddChild(back);
        Visible = false;
    }

    public void ShowResult(RunResultSnapshot snapshot, bool victory = true)
    {
        _title.Text = victory ? "中继站守住了" : "本次护送失败";
        string protocols = snapshot.ProtocolIds.Count == 0 ? "无" : string.Join("、", snapshot.ProtocolIds);
        _summary.Text = $"种子：{snapshot.Seed}\n协议：{protocols}\n中继站：{snapshot.RelayIntegrity}/100\n耗时：{snapshot.Elapsed.Minutes:00}:{snapshot.Elapsed.Seconds:00}";
        Visible = true;
    }

    public void HideResult() => Visible = false;
}
