using System;
using Godot;

namespace Game1;

/// <summary>
/// Alpha 02C 的可见波间确认面板。它只确认五波节奏，不提前应用 Alpha 02E 的协议或维护效果。
/// </summary>
public partial class WaveRewardPanel : PanelContainer
{
    private Label _title = null!;
    private Label _description = null!;
    private Button _confirmButton = null!;
    private int _waveNumber;
    private RewardKind _kind;

    public event Action<string> RewardConfirmed;

    public override void _Ready()
    {
        Name = "WaveRewardPanel";
        Position = new Vector2(124f, 86f);
        Size = new Vector2(232f, 92f);
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;

        VBoxContainer content = new();
        AddChild(content);
        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 11);
        content.AddChild(_title);
        _description = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _description.AddThemeFontSizeOverride("font_size", 8);
        content.AddChild(_description);
        _confirmButton = new Button { Text = "确认并进入下一波", FocusMode = FocusModeEnum.All };
        _confirmButton.Pressed += Confirm;
        content.AddChild(_confirmButton);
    }

    public void ShowReward(int waveNumber, RewardKind kind)
    {
        if (waveNumber is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(waveNumber));
        _waveNumber = waveNumber;
        _kind = kind;
        _title.Text = $"第 {waveNumber} 波清场";
        _description.Text = kind switch
        {
            RewardKind.NormalProtocol => "协议奖励将在 Alpha 02E 接入；当前先确认波间节奏。",
            RewardKind.Maintenance => "维护效果将在 Alpha 02E 接入；当前先确认波间节奏。",
            RewardKind.RareProtocol => "稀有协议将在 Alpha 02E 接入；当前先确认第五波完成。",
            _ => "确认后继续正式竞技场流程。"
        };
        _confirmButton.Text = waveNumber == 5 ? "确认并准备 Boss" : "确认并进入下一波";
        Visible = true;
        _confirmButton.GrabFocus();
    }

    private void Confirm()
    {
        if (!Visible) return;
        Visible = false;
        RewardConfirmed?.Invoke($"arena_wave_{_waveNumber}_{_kind}");
    }
}
