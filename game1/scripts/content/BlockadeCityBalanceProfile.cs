using System;
using Godot;

namespace Game1;

/// <summary>
/// 封锁城区正式全局节奏系数。Debug 调参台可保存它，Release 只读并应用它。
/// 各波/各敌军基础数值仍保留在原 Resource 中，此处只保存已经确认的全局系数。
/// </summary>
[GlobalClass]
public partial class BlockadeCityBalanceProfile : Resource
{
    public const string OfficialResourcePath = "res://resources/tuning/blockade_city_balance_profile.tres";

    [Export(PropertyHint.Range, "0.5,2.5,0.25")]
    public float SpawnRateMultiplier { get; set; } = 1f;

    [Export(PropertyHint.Range, "-3,8,1")]
    public int MaximumAliveAdjustment { get; set; }

    [Export(PropertyHint.Range, "0.75,1.5,0.05")]
    public float EnemyMoveSpeedMultiplier { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.75,1.75,0.25")]
    public float EnemyAttackRateMultiplier { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.5,2.0,0.25")]
    public float EnemyArmorMultiplier { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.8,1.3,0.05")]
    public float PlayerMoveSpeedMultiplier { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.75,2.0,0.25")]
    public float PlayerFireRateMultiplier { get; set; } = 1f;

    public BlockadeCityBalanceSettings ToSettings()
    {
        BlockadeCityBalanceSettings settings = new(
            SpawnRateMultiplier,
            MaximumAliveAdjustment,
            EnemyMoveSpeedMultiplier,
            EnemyAttackRateMultiplier,
            EnemyArmorMultiplier,
            PlayerMoveSpeedMultiplier,
            PlayerFireRateMultiplier);
        settings.Validate();
        return settings;
    }

    public void Apply(BlockadeCityBalanceSettings settings)
    {
        settings.Validate();
        SpawnRateMultiplier = settings.SpawnRateMultiplier;
        MaximumAliveAdjustment = settings.MaximumAliveAdjustment;
        EnemyMoveSpeedMultiplier = settings.EnemyMoveSpeedMultiplier;
        EnemyAttackRateMultiplier = settings.EnemyAttackRateMultiplier;
        EnemyArmorMultiplier = settings.EnemyArmorMultiplier;
        PlayerMoveSpeedMultiplier = settings.PlayerMoveSpeedMultiplier;
        PlayerFireRateMultiplier = settings.PlayerFireRateMultiplier;
    }

    public void Validate() => ToSettings();
}
