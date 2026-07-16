using Godot;

namespace Game1;

/// <summary>房间内中继站的受击入口；真实耐久同步回当前单局状态。</summary>
public partial class RelayStation : StaticBody2D, IDamageable
{
    [Signal] public delegate void DestroyedEventHandler();
    [Signal] public delegate void DamagedEventHandler(int remainingIntegrity);
    [Export] public int MaximumIntegrity { get; set; } = 100;
    private RunState _run = null!;
    public void Initialize(RunState run) => _run = run;
    public DamageResult ApplyDamage(DamageContext context)
    {
        int before = _run.RelayIntegrity;
        _run.ApplyRelayDamage(context.Amount);
        bool destroyedNow = before > 0 && _run.RelayIntegrity == 0;
        if (_run.RelayIntegrity < before) EmitSignal(SignalName.Damaged, _run.RelayIntegrity);
        if (destroyedNow) EmitSignal(SignalName.Destroyed);
        return new DamageResult(before - _run.RelayIntegrity, destroyedNow);
    }
}
