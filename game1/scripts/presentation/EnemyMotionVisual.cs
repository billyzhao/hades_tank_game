using Godot;

namespace Game1;

/// <summary>敌军行驶动画的纯数值结果，不参与位置和碰撞结算。</summary>
public readonly record struct EnemyMotionPose(float LateralOffset, Vector2 Scale);

public static class EnemyMotionVisual
{
    public static EnemyMotionPose Calculate(float time, float speed, float baseScale)
    {
        if (speed <= 0.01f)
        {
            return new EnemyMotionPose(0f, Vector2.One * baseScale);
        }

        float pulse = Mathf.Sin(time * 18f);
        return new EnemyMotionPose(
            pulse * 0.5f,
            new Vector2(baseScale + pulse * 0.012f, baseScale - pulse * 0.008f));
    }
}
