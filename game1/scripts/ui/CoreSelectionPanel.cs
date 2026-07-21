using System;
using Godot;

namespace Game1;

/// <summary>开局三核心选择卡；仅展示静态定义，并把最终选择提交给 AppRoot。</summary>
public partial class CoreSelectionPanel : PanelContainer
{
    private HBoxContainer _cards = null!;
    private bool _hasChosen;

    public event Action<CoreId> CoreChosen = delegate { };

    public override void _Ready()
    {
        Name = "CoreSelectionPanel";
        Position = new Vector2(72f, 76f);
        Size = new Vector2(336f, 110f);
        ProcessMode = ProcessModeEnum.WhenPaused;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        VBoxContainer content = new();
        AddChild(content);
        Label title = new()
        {
            Text = "选择移动核心",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 11);
        content.AddChild(title);
        Label hint = new()
        {
            Text = "核心决定初始节奏，不锁定后续协议路线。",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        hint.AddThemeFontSizeOverride("font_size", 7);
        content.AddChild(hint);
        _cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        content.AddChild(_cards);
    }

    public void ShowChoices(CoreCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _hasChosen = false;
        foreach (Node child in _cards.GetChildren()) child.QueueFree();
        foreach (CoreDefinition definition in catalog.Definitions)
        {
            Button card = new()
            {
                Text = definition.DisplayName,
                TooltipText = definition.Description,
                CustomMinimumSize = new Vector2(102f, 56f),
                FocusMode = FocusModeEnum.All
            };
            card.AddThemeFontSizeOverride("font_size", 7);
            card.Pressed += () => Choose(definition.Id);
            _cards.AddChild(card);
        }
        Visible = true;
        (_cards.GetChildOrNull<Button>(0))?.GrabFocus();
    }

    private void Choose(CoreId core)
    {
        if (!Visible || _hasChosen) return;
        _hasChosen = true;
        Visible = false;
        CoreChosen(core);
    }
}
