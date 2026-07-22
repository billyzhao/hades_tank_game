namespace Game1;

/// <summary>为可受击实体声明所属阵营，供弹丸在命中时执行友伤保护。</summary>
public interface ITeamDamageable : IDamageable
{
    Team DamageTeam { get; }
}
