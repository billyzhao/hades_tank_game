using Godot;

namespace Game1;

/// <summary>首区工业 UI 的纯表现适配；只替换卡片和图标，不保存或修改任何玩法数据。</summary>
public static class IndustrialUiSkin
{
    private static Texture2D RewardCardTexture =>
        GD.Load<Texture2D>("res://assets/sprites/ui/reward_card_frame.png");

    public static void ApplyRewardCard(Button button, Texture2D icon = null)
    {
        StyleBoxEmpty empty = new();
        button.AddThemeStyleboxOverride("normal", empty);
        button.AddThemeStyleboxOverride("hover", empty);
        button.AddThemeStyleboxOverride("pressed", empty);
        button.AddThemeStyleboxOverride("disabled", empty);

        TextureRect frame = new()
        {
            Name = "IndustrialFrame",
            Texture = RewardCardTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ShowBehindParent = true
        };
        button.AddChild(frame);
        frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        button.AddThemeConstantOverride("outline_size", 2);
        button.AddThemeColorOverride("font_outline_color", new Color(.03f, .035f, .04f));
        if (icon is not null) ApplyCornerIcon(button, icon, 22);
    }

    public static TextureRect ApplyCornerIcon(Control host, Texture2D icon, float size = 14f)
    {
        TextureRect visual = new()
        {
            Name = "SemanticIcon",
            Texture = icon,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Position = new Vector2(4f, 4f),
            Size = new Vector2(size, size),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        host.AddChild(visual);
        return visual;
    }
}
