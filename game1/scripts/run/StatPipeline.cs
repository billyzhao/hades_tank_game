namespace Game1;

/// <summary>属性修正管线的唯一计算入口。</summary>
public sealed class StatPipeline
{
    public float Evaluate(
        StatId stat,
        float baseValue,
        System.Collections.Generic.IEnumerable<StatModifier> modifiers)
    {
        if (!float.IsFinite(baseValue))
        {
            throw new System.ArgumentOutOfRangeException(nameof(baseValue), "基础属性必须是有限数值。");
        }

        if (modifiers is null)
        {
            throw new System.ArgumentNullException(nameof(modifiers));
        }

        float flatAdd = 0f;
        float additivePercent = 0f;
        float multiplicativePercent = 0f;
        foreach (StatModifier modifier in modifiers)
        {
            if (modifier.Stat != stat)
            {
                continue;
            }

            if (!float.IsFinite(modifier.FlatAdd) ||
                !float.IsFinite(modifier.AdditivePercent) ||
                !float.IsFinite(modifier.MultiplicativePercent))
            {
                throw new System.ArgumentOutOfRangeException(nameof(modifiers), "属性修正必须是有限数值。");
            }

            flatAdd += modifier.FlatAdd;
            additivePercent += modifier.AdditivePercent;
            multiplicativePercent += modifier.MultiplicativePercent;
        }

        // 固定值、加法百分比、乘法百分比严格分段累计；最终才钳制，避免修正器声明顺序影响结果。
        float value = baseValue + flatAdd;
        value *= 1f + additivePercent;
        value *= 1f + multiplicativePercent;
        return System.MathF.Max(0f, value);
    }
}
