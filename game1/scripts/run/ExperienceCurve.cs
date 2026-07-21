using System;

namespace Game1;

/// <summary>Alpha 02D 的早期经验阈值。平衡参数集中在此处，掉落节点不得硬编码等级成本。</summary>
public sealed class ExperienceCurve
{
    public int GetRequiredExperience(int level)
    {
        if (level <= 0) throw new ArgumentOutOfRangeException(nameof(level));
        return 20 + (level - 1) * 10;
    }
}
