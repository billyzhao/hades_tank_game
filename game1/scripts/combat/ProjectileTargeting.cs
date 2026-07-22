namespace Game1;

/// <summary>集中定义弹丸的阵营查询层与伤害许可，避免友方误伤依赖调用方约定。</summary>
public static class ProjectileTargeting
{
    private const uint WorldMask = 3u;
    private const uint PlayerMask = 4u;
    private const uint EnemyMask = 8u;

    public static uint CollisionMaskFor(Team sourceTeam) => sourceTeam switch
    {
        Team.Player => WorldMask | EnemyMask,
        Team.Enemy => WorldMask | PlayerMask,
        _ => WorldMask
    };

    public static bool CanDamage(Team sourceTeam, Team targetTeam) => sourceTeam != targetTeam;
}
