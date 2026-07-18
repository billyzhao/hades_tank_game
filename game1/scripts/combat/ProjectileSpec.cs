namespace Game1;

/// <summary>单枚炮弹的不可变运行规格；分裂数只在首次命中时消费，子弹不会继续分裂。</summary>
public readonly record struct ProjectileSpec(int Damage, float Speed, float LifetimeSeconds, int Bounces, int SplitCount = 0);
