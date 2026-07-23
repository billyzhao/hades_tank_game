namespace Game1;

/// <summary>波次威胁预算：巡逻=1、突击=2、攻城=3，且同一时刻只允许一台攻城炮车。</summary>
public sealed class ThreatBudget
{
    private readonly int _total;
    private int _spent;
    private bool _siegeSpawned;

    public ThreatBudget(int total) => _total = total;

    public bool TrySpend(BehaviorId behavior)
    {
        int cost = behavior switch { BehaviorId.Scout => 1, BehaviorId.Patrol => 1, BehaviorId.Assault => 2, BehaviorId.Mortar => 3, _ => 0 };
        if (behavior == BehaviorId.Mortar && _siegeSpawned) return false;
        if (_spent + cost > _total) return false;
        _spent += cost;
        _siegeSpawned |= behavior == BehaviorId.Mortar;
        return true;
    }
}
