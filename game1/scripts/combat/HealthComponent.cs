using Godot;

namespace Game1;

/// <summary>将可测试的 HealthState 接入 Godot 节点，并保证耗尽信号只发出一次。</summary>
public partial class HealthComponent : Node, IDamageable
{
    [Export] public int MaximumArmor { get; set; } = 100;
    [Export] public int StartingShield { get; set; }
    [Signal] public delegate void ValueChangedEventHandler(int armor, int shield);
    [Signal] public delegate void DepletedEventHandler();
    private HealthState _state = null!;

    public int Armor => _state.Armor;
    public override void _Ready() => _state = new HealthState(MaximumArmor, StartingShield);
    public DamageResult ApplyDamage(DamageContext context)
    {
        DamageResult result = _state.ApplyDamage(context);
        EmitSignal(SignalName.ValueChanged, _state.Armor, _state.Shield);
        if (result.DepletedNow) EmitSignal(SignalName.Depleted);
        return result;
    }
    public void RestoreArmor(int amount)
    {
        _state = new HealthState(System.Math.Min(MaximumArmor, _state.Armor + System.Math.Max(0, amount)), _state.Shield);
        EmitSignal(SignalName.ValueChanged, _state.Armor, _state.Shield);
    }
}
