using Godot;

namespace Game1;

/// <summary>首区工业 UI 的纯表现适配；只替换卡片底图，不保存或修改任何奖励数据。</summary>
public static class IndustrialUiSkin
{
    private static readonly Texture2D RewardCardTexture =
        GD.Load<Texture2D>("res://assets/sprites/ui/reward_card_frame.png");

    public static void ApplyRewardCard(Button button)
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
    }
}
