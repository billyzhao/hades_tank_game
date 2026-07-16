namespace Game1;

/// <summary>一次伤害结算的不可变输入；后续可扩展来源阵营、命中点与伤害标签。</summary>
public readonly record struct DamageContext(int Amount);
