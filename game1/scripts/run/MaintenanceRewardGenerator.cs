using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>维护奖励的受控随机入口；低装甲保障在此集中执行，UI 不得自行补插修复项。</summary>
public sealed class MaintenanceRewardGenerator
{
    private static readonly RewardChoice Repair = new("maintenance_repair_25", "应急装甲修复", "立刻恢复 25% 最大装甲。", new[] { "repair", "survival" });
    private static readonly RewardChoice Barrier = new("maintenance_barrier", "临时偏转护板", "下一波获得短时伤害缓冲。", new[] { "guard", "temporary" });
    private static readonly RewardChoice Coolant = new("maintenance_coolant", "冷却液补给", "下一波主炮装填更快。", new[] { "fire_rate", "temporary" });
    private static readonly RewardChoice Thruster = new("maintenance_thruster", "推进器校准", "下一波冲刺冷却更短。", new[] { "dash", "temporary" });

    public RewardOffer Generate(int seed, int armor, int maximumArmor)
    {
        if (maximumArmor <= 0) throw new ArgumentOutOfRangeException(nameof(maximumArmor));
        if (armor < 0 || armor > maximumArmor) throw new ArgumentOutOfRangeException(nameof(armor));

        List<RewardChoice> pool = new() { Barrier, Coolant, Thruster };
        Rotate(pool, seed);
        List<RewardChoice> choices = armor * 100 < maximumArmor * 30
            ? new List<RewardChoice> { Repair, pool[0], pool[1] }
            : new List<RewardChoice> { pool[0], pool[1], pool[2] };
        return new RewardOffer(RewardKind.Maintenance, choices);
    }

    private static void Rotate(IList<RewardChoice> choices, int seed)
    {
        int offset = (int)((uint)seed % (uint)choices.Count);
        if (offset == 0) return;
        RewardChoice[] ordered = choices.Skip(offset).Concat(choices.Take(offset)).ToArray();
        for (int index = 0; index < ordered.Length; index++) choices[index] = ordered[index];
    }
}
