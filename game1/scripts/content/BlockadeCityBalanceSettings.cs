using System;

namespace Game1;

/// <summary>
/// 封锁城区正式节奏参数的不可变快照。
/// 运行系统只消费快照，Debug 菜单不能直接持有或修改正式 Resource。
/// </summary>
public readonly record struct BlockadeCityBalanceSettings(
    float SpawnRateMultiplier,
    int MaximumAliveAdjustment,
    float EnemyMoveSpeedMultiplier,
    float EnemyAttackRateMultiplier,
    float EnemyArmorMultiplier,
    float PlayerMoveSpeedMultiplier,
    float PlayerFireRateMultiplier)
{
    public static BlockadeCityBalanceSettings DesignBaseline => new(1f, 0, 1f, 1f, 1f, 1f, 1f);

    public static BlockadeCityBalanceSettings DensePreset => new(1.5f, 2, 1f, 1f, 1f, 1f, 1f);

    public static BlockadeCityBalanceSettings HighPressurePreset => new(2f, 4, 1.1f, 1.25f, 1f, 1f, 1f);

    public BlockadeCityBalanceSettings ClampToApprovedRange() => new(
        Math.Clamp(SpawnRateMultiplier, 0.5f, 2.5f),
        Math.Clamp(MaximumAliveAdjustment, -3, 8),
        Math.Clamp(EnemyMoveSpeedMultiplier, 0.75f, 1.5f),
        Math.Clamp(EnemyAttackRateMultiplier, 0.75f, 1.75f),
        Math.Clamp(EnemyArmorMultiplier, 0.5f, 2f),
        Math.Clamp(PlayerMoveSpeedMultiplier, 0.8f, 1.3f),
        Math.Clamp(PlayerFireRateMultiplier, 0.75f, 2f));

    public void Validate()
    {
        ValidateFiniteRange(SpawnRateMultiplier, 0.5f, 2.5f, nameof(SpawnRateMultiplier));
        if (MaximumAliveAdjustment is < -3 or > 8)
            throw new ArgumentOutOfRangeException(nameof(MaximumAliveAdjustment));
        ValidateFiniteRange(EnemyMoveSpeedMultiplier, 0.75f, 1.5f, nameof(EnemyMoveSpeedMultiplier));
        ValidateFiniteRange(EnemyAttackRateMultiplier, 0.75f, 1.75f, nameof(EnemyAttackRateMultiplier));
        ValidateFiniteRange(EnemyArmorMultiplier, 0.5f, 2f, nameof(EnemyArmorMultiplier));
        ValidateFiniteRange(PlayerMoveSpeedMultiplier, 0.8f, 1.3f, nameof(PlayerMoveSpeedMultiplier));
        ValidateFiniteRange(PlayerFireRateMultiplier, 0.75f, 2f, nameof(PlayerFireRateMultiplier));
    }

    public bool ApproximatelyEquals(BlockadeCityBalanceSettings other, float tolerance = 0.0001f) =>
        Math.Abs(SpawnRateMultiplier - other.SpawnRateMultiplier) <= tolerance &&
        MaximumAliveAdjustment == other.MaximumAliveAdjustment &&
        Math.Abs(EnemyMoveSpeedMultiplier - other.EnemyMoveSpeedMultiplier) <= tolerance &&
        Math.Abs(EnemyAttackRateMultiplier - other.EnemyAttackRateMultiplier) <= tolerance &&
        Math.Abs(EnemyArmorMultiplier - other.EnemyArmorMultiplier) <= tolerance &&
        Math.Abs(PlayerMoveSpeedMultiplier - other.PlayerMoveSpeedMultiplier) <= tolerance &&
        Math.Abs(PlayerFireRateMultiplier - other.PlayerFireRateMultiplier) <= tolerance;

    public string ToCompactText() =>
        $"刷怪×{SpawnRateMultiplier:0.00}｜上限{MaximumAliveAdjustment:+0;-0;±0}｜" +
        $"敌移×{EnemyMoveSpeedMultiplier:0.00}｜敌攻×{EnemyAttackRateMultiplier:0.00}｜" +
        $"敌甲×{EnemyArmorMultiplier:0.00}｜我移×{PlayerMoveSpeedMultiplier:0.00}｜" +
        $"我射×{PlayerFireRateMultiplier:0.00}";

    private static void ValidateFiniteRange(float value, float minimum, float maximum, string name)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(name, value, $"{name} 必须位于 {minimum:0.00}～{maximum:0.00}。");
    }
}
