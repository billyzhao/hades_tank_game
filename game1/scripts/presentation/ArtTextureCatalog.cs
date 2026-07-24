using System;
using Godot;

namespace Game1;

/// <summary>
/// BC-03 正式美术资源的唯一运行时索引。玩法代码只请求语义资源，
/// 不在各控制器里重复拼接文件路径。
/// </summary>
public static class ArtTextureCatalog
{
    private const string AnimationRoot = "res://assets/sprites/effects/animations/";
    private const string IconRoot = "res://assets/sprites/ui/icons/";

    // 使用属性而不是静态 Resource 强引用，避免 Godot 退出时由 CLR 静态字段延长纹理 RID 生命周期。
    public static Texture2D[] MuzzleFlash => LoadSequence("muzzle_flash");
    public static Texture2D[] SteelImpact => LoadSequence("steel_impact");
    public static Texture2D[] EnemyBurst => LoadSequence("enemy_burst");
    public static Texture2D[] TankDust => LoadSequence("tank_dust");
    public static Texture2D[] DashTrail => LoadSequence("dash_trail");
    public static Texture2D[] PlayerHit => LoadSequence("player_hit");
    public static Texture2D[] Reboot => LoadSequence("reboot");
    public static Texture2D[] LevelUp => LoadSequence("level_up");
    public static Texture2D[] SpawnWarning => LoadSequence("spawn_warning");
    public static Texture2D[] MortarWarning => LoadSequence("mortar_warning");
    public static Texture2D[] BarrierWarning => LoadSequence("barrier_warning");
    public static Texture2D[] ChargeWarning => LoadSequence("charge_warning");
    public static Texture2D[] BossPhase => LoadSequence("boss_phase");
    public static Texture2D[] BossDeath => LoadSequence("boss_death");
    public static Texture2D[] CombatData => LoadSequence("combat_data");
    public static Texture2D[] PlayerProjectile => LoadSequence("player_projectile");
    public static Texture2D[] EnemyProjectile => LoadSequence("enemy_projectile");

    public static Texture2D ArmorIcon => LoadIcon("armor");
    public static Texture2D RebootIcon => LoadIcon("reboot");
    public static Texture2D WaveIcon => LoadIcon("wave");
    public static Texture2D EliteIcon => LoadIcon("elite");
    public static Texture2D AuxiliaryIcon => LoadIcon("auxiliary");
    public static Texture2D ArsenalIcon => LoadIcon("arsenal");
    public static Texture2D EngineeringIcon => LoadIcon("engineering");
    public static Texture2D ReconnaissanceIcon => LoadIcon("reconnaissance");
    public static Texture2D LogisticsIcon => LoadIcon("logistics");

    public static Texture2D CoreIcon(CoreId id) => LoadIcon(id switch
    {
        CoreId.BreakthroughCannon => "core_breakthrough",
        CoreId.OverdriveAutocannon => "core_overdrive",
        CoreId.ElectricRider => "core_electric",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    });

    public static Texture2D CoreSprite(CoreId id) => GD.Load<Texture2D>(id switch
    {
        CoreId.BreakthroughCannon => "res://assets/sprites/player/core_breakthrough.png",
        CoreId.OverdriveAutocannon => "res://assets/sprites/player/core_overdrive.png",
        CoreId.ElectricRider => "res://assets/sprites/player/core_electric.png",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    });

    public static Texture2D StatIcon(StatUpgradeId id) => LoadIcon(id switch
    {
        StatUpgradeId.ArmorMax => "armor",
        StatUpgradeId.MoveSpeed => "move_speed",
        StatUpgradeId.Damage => "damage",
        StatUpgradeId.FireCooldown => "fire_rate",
        StatUpgradeId.DashCooldown => "projectile_speed",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    });

    public static Texture2D RewardIcon(string rewardId)
    {
        if (rewardId.StartsWith("arsenal_", StringComparison.Ordinal)) return ArsenalIcon;
        if (rewardId.StartsWith("engineering_", StringComparison.Ordinal)) return EngineeringIcon;
        if (rewardId.StartsWith("recon_", StringComparison.Ordinal)) return ReconnaissanceIcon;
        if (rewardId.StartsWith("logistics_", StringComparison.Ordinal)) return LogisticsIcon;
        if (rewardId.StartsWith("aux_", StringComparison.Ordinal)) return AuxiliaryIcon;
        return ArmorIcon;
    }

    private static Texture2D[] LoadSequence(string prefix)
    {
        Texture2D[] textures = new Texture2D[4];
        for (int index = 0; index < textures.Length; index++)
            textures[index] = GD.Load<Texture2D>($"{AnimationRoot}{prefix}_{index + 1}.png");
        return textures;
    }

    private static Texture2D LoadIcon(string name) =>
        GD.Load<Texture2D>($"{IconRoot}{name}.png");
}
