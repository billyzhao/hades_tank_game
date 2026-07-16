namespace Game1;

/// <summary>本次实际扣除的装甲值，以及是否由本次命中首次耗尽生命。</summary>
public readonly record struct DamageResult(int AppliedDamage, bool DepletedNow);
