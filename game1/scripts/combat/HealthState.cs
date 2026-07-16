namespace Game1;

/// <summary>不依赖 Godot 节点的生命结算状态，便于在单元测试中覆盖护盾和死亡边界。</summary>
public sealed class HealthState
{
    public HealthState(int armor, int shield = 0)
    {
        Armor = System.Math.Max(0, armor);
        Shield = System.Math.Max(0, shield);
    }

    public int Armor { get; private set; }
    public int Shield { get; private set; }
    public bool IsDepleted => Armor == 0;

    public DamageResult ApplyDamage(DamageContext context)
    {
        int incoming = System.Math.Max(0, context.Amount);
        int absorbed = System.Math.Min(Shield, incoming);
        Shield -= absorbed;
        int armorDamage = System.Math.Min(Armor, incoming - absorbed);
        bool depletedNow = Armor > 0 && armorDamage == Armor;
        Armor -= armorDamage;
        return new DamageResult(armorDamage, depletedNow);
    }
}
