using Godot;

namespace Game1;

/// <summary>房间内中继站的受击入口；真实耐久同步回当前单局状态。</summary>
public partial class RelayStation : StaticBody2D, IDamageable
{
    [Signal] public delegate void DestroyedEventHandler();
    [Signal] public delegate void DamagedEventHandler(int remainingIntegrity);
    [Signal] public delegate void ShieldInterceptedEventHandler(int preventedDamage);
    [Export] public int MaximumIntegrity { get; set; } = 100;
    private RunState _run = null!;
    private BuildController _buildController;
    public void Initialize(RunState run) => _run = run;
    public void AttachBuild(BuildController buildController) => _buildController = buildController ?? throw new System.ArgumentNullException(nameof(buildController));
    public DamageResult ApplyDamage(DamageContext context)
    {
        int before = _run.RelayIntegrity;
        int shield = _buildController is null ? 0 : Mathf.RoundToInt(_buildController.EvaluateStat(StatId.RelayShield, 0f));
        int incomingDamage = System.Math.Max(0, context.Amount);
        int preventedDamage = System.Math.Min(incomingDamage, shield);
        _run.ApplyRelayDamage(incomingDamage - preventedDamage);
        bool destroyedNow = before > 0 && _run.RelayIntegrity == 0;
        if (preventedDamage > 0) EmitSignal(SignalName.ShieldIntercepted, preventedDamage);
        if (_run.RelayIntegrity < before)
        {
            _buildController?.OnRelayDamaged();
            EmitSignal(SignalName.Damaged, _run.RelayIntegrity);
        }
        if (destroyedNow) EmitSignal(SignalName.Destroyed);
        return new DamageResult(before - _run.RelayIntegrity, destroyedNow);
    }
}
