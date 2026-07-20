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
    private double _invulnerabilityRemaining;

    public int Armor => _state.Armor;
    public bool IsInvulnerable => _invulnerabilityRemaining > 0d;
    public double InvulnerabilityRemaining => _invulnerabilityRemaining;
    public override void _Ready() => _state = new HealthState(MaximumArmor, StartingShield);

    public override void _Process(double delta)
    {
        _invulnerabilityRemaining = System.Math.Max(0d, _invulnerabilityRemaining - delta);
    }

    public DamageResult ApplyDamage(DamageContext context)
    {
        // 重启保护只拦截伤害；负数伤害仍由 HealthState 按“不治疗”规则统一处理。
        if (IsInvulnerable && context.Amount > 0) return new DamageResult(0, false);

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

    /// <summary>用跨场景 RunState 初始化当前玩家实例，避免节点和单局状态各自维护一份装甲。</summary>
    public void InitializeArmor(int armor, int maximumArmor)
    {
        if (maximumArmor <= 0) throw new System.ArgumentOutOfRangeException(nameof(maximumArmor));
        MaximumArmor = maximumArmor;
        _state = new HealthState(System.Math.Clamp(armor, 0, maximumArmor), StartingShield);
        EmitSignal(SignalName.ValueChanged, _state.Armor, _state.Shield);
    }

    public void SetArmor(int armor)
    {
        _state = new HealthState(System.Math.Clamp(armor, 0, MaximumArmor), _state.Shield);
        EmitSignal(SignalName.ValueChanged, _state.Armor, _state.Shield);
    }

    public void GrantInvulnerability(double seconds)
    {
        _invulnerabilityRemaining = System.Math.Max(_invulnerabilityRemaining, System.Math.Max(0d, seconds));
    }
}
