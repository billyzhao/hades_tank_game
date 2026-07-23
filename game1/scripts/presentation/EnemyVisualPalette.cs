using Godot;

namespace Game1;

/// <summary>集中定义敌军职责色，确保场景精灵、轮廓和 HUD 使用同一套可读性语义。</summary>
public static class EnemyVisualPalette
{
    public static Color GetRoleTint(BehaviorId behavior) => behavior switch
    {
        BehaviorId.Scout => new Color(0.35f, 0.92f, 0.88f),
        BehaviorId.Assault => new Color(1f, 0.42f, 0.25f),
        BehaviorId.Mortar => new Color(0.72f, 0.30f, 0.85f),
        _ => new Color(0.95f, 0.75f, 0.20f)
    };
}
