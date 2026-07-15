using Godot;

namespace Game1;

public partial class WeaponDefinition : Resource
{
    [Export] public int Damage { get; set; } = 10;
    [Export] public float ProjectileSpeed { get; set; } = 360f;
    [Export] public float CooldownSeconds { get; set; } = 0.22f;
    [Export] public float LifetimeSeconds { get; set; } = 2.5f;
    [Export] public int Bounces { get; set; } = 1;
    public ProjectileSpec CreateSpec() => new(Damage, ProjectileSpeed, LifetimeSeconds, Bounces);
}
