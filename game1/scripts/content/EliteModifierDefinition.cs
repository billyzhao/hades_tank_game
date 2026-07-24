using System;
using Godot;

namespace Game1;

/// <summary>第五波唯一精英规则：用速度节奏制造压力，不通过生命膨胀。</summary>
[GlobalClass]
public partial class EliteModifierDefinition : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public float BoostSeconds { get; set; } = 1.25f;
    [Export] public float RecoverySeconds { get; set; } = 0.75f;
    [Export] public float BoostSpeedMultiplier { get; set; } = 1.55f;
    [Export] public float RecoverySpeedMultiplier { get; set; } = 0.55f;
    [Export] public float ArmorMultiplier { get; set; } = 1f;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(DisplayName) ||
            !float.IsFinite(BoostSeconds) || BoostSeconds <= 0f ||
            !float.IsFinite(RecoverySeconds) || RecoverySeconds <= 0f ||
            !float.IsFinite(BoostSpeedMultiplier) || BoostSpeedMultiplier <= 1f ||
            !float.IsFinite(RecoverySpeedMultiplier) || RecoverySpeedMultiplier <= 0f ||
            RecoverySpeedMultiplier >= 1f ||
            !float.IsFinite(ArmorMultiplier) || ArmorMultiplier != 1f)
            throw new ArgumentException($"精英规则 '{Id}' 无效；封锁城区精英不得增加装甲。");
    }
}
