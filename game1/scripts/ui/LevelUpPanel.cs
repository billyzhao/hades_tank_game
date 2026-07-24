using System;
using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>升级期间唯一可输入的三选一面板；由 AppRoot 持有暂停语义。</summary>
public partial class LevelUpPanel : PanelContainer
{
    public event Action<StatUpgradeId> UpgradeChosen;

    public override void _Ready()
    {
        Name = "LevelUpPanel";
        Position = new Vector2(56f, 58f);
        Size = new Vector2(368f, 154f);
        Visible = false;
        ProcessMode = ProcessModeEnum.WhenPaused;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public void ShowOffer(int level, IReadOnlyList<StatUpgradeOffer> offers)
    {
        foreach (Node child in GetChildren()) child.QueueFree();
        VBoxContainer root = new();
        AddChild(root);
        Label title = new() { Text = $"等级 {level}  // 选择一项强化", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 11);
        root.AddChild(title);
        HBoxContainer cards = new() { Alignment = BoxContainer.AlignmentMode.Center };
        root.AddChild(cards);
        foreach (StatUpgradeOffer offer in offers)
        {
            Button card = new() { Text = offer.DisplayName, CustomMinimumSize = new Vector2(106f, 112f), FocusMode = FocusModeEnum.All };
            card.AddThemeFontSizeOverride("font_size", 8);
            IndustrialUiSkin.ApplyRewardCard(card, ArtTextureCatalog.StatIcon(offer.Id));
            card.Pressed += () => UpgradeChosen?.Invoke(offer.Id);
            cards.AddChild(card);
        }
        Visible = true;
        cards.GetChild<Button>(0).GrabFocus();
    }

    public void HideOffer() => Visible = false;
}
