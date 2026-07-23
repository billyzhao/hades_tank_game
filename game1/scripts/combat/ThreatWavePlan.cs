using System.Collections.Generic;

namespace Game1;

/// <summary>
/// MVP 守点战的固定波次编排。数值直接对应策划确认的 4 / 6 / 8 威胁预算，
/// 每波至多包含一台重炮单位，避免同一时间形成无法处理的远程集火。
/// </summary>
public static class ThreatWavePlan
{
    public static IReadOnlyList<IReadOnlyList<BehaviorId>> CreateMvp() =>
    [
        [BehaviorId.Patrol, BehaviorId.Patrol, BehaviorId.Assault],
        [BehaviorId.Mortar, BehaviorId.Patrol, BehaviorId.Assault],
        [BehaviorId.Mortar, BehaviorId.Assault, BehaviorId.Patrol, BehaviorId.Patrol, BehaviorId.Patrol]
    ];

    public static int GetThreatCost(IEnumerable<BehaviorId> behaviors)
    {
        int total = 0;
        foreach (BehaviorId behavior in behaviors)
        {
            total += behavior switch
            {
                BehaviorId.Scout => 1,
                BehaviorId.Patrol => 1,
                BehaviorId.Assault => 2,
                BehaviorId.Mortar => 3,
                _ => 0
            };
        }

        return total;
    }
}
