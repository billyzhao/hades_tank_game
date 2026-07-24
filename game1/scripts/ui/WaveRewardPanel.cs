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
    private HBoxContainer _cards = null!;
    private Button _confirmButton = null!;
    private int _waveNumber;
    private RewardKind _kind;

    public event Action<string> RewardConfirmed;

    public override void _Ready()
    {
        Name = "WaveRewardPanel";
        Position = new Vector2(70f, 55f);
        Size = new Vector2(340f, 160f);
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
        _cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        content.AddChild(_cards);
        _confirmButton = new Button { Text = "确认并进入下一波", FocusMode = FocusModeEnum.All };
        _confirmButton.Pressed += () => Confirm($"arena_wave_{_waveNumber}_{_kind}");
        content.AddChild(_confirmButton);
    }

    public void ShowReward(int waveNumber, RewardKind kind)
    {
        if (waveNumber is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(waveNumber));
        _waveNumber = waveNumber;
        _kind = kind;
        ClearCards();
        _title.Text = $"第 {waveNumber} 波清场";
        _description.Text = kind switch
        {
            RewardKind.NormalProtocol => "协议奖励将在 Alpha 02E 接入；当前先确认波间节奏。",
            RewardKind.Maintenance => "维护效果将在 Alpha 02E 接入；当前先确认波间节奏。",
            RewardKind.RareProtocol => "稀有协议将在 Alpha 02E 接入；当前先确认第五波完成。",
            _ => "确认后继续正式竞技场流程。"
        };
        _confirmButton.Text = waveNumber == 5 ? "确认并准备 Boss" : "确认并进入下一波";
        _confirmButton.Visible = true;
        Visible = true;
        _confirmButton.GrabFocus();
    }

    /// <summary>显示真实三选一卡片；每张卡只回传选择 Id，构筑应用由上层 RewardController 统一负责。</summary>
    public void ShowOffer(RewardOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ClearCards();
        _title.Text = offer.Kind switch
        {
            RewardKind.NormalProtocol => "选择军械协议",
            RewardKind.RareProtocol => "选择稀有协议",
            RewardKind.Maintenance => "选择维护方案",
            _ => "选择战术奖励"
        };
        _description.Text = "三选一；确认后才进入下一波。";
        _confirmButton.Visible = false;
        foreach (RewardChoice choice in offer.Choices)
        {
            Button card = new()
            {
                Text = choice.DisplayName,
                TooltipText = choice.Description,
                CustomMinimumSize = new Vector2(96f, 98f),
                FocusMode = FocusModeEnum.All
            };
            card.AddThemeFontSizeOverride("font_size", 7);
            IndustrialUiSkin.ApplyRewardCard(card, ArtTextureCatalog.RewardIcon(choice.Id));
            card.Pressed += () => Confirm(choice.Id);
            _cards.AddChild(card);
        }
        Visible = true;
        (_cards.GetChildOrNull<Button>(0))?.GrabFocus();
    }

    private void Confirm(string rewardId)
    {
        if (!Visible) return;
        Visible = false;
        RewardConfirmed?.Invoke(rewardId);
    }

    private void ClearCards()
    {
        foreach (Node child in _cards.GetChildren()) child.QueueFree();
    }
}
