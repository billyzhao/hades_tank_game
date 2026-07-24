using Godot;

namespace Game1;

/// <summary>把命中事实转换为短寿命的火花或爆炸，不参与任何伤害结算。</summary>
public partial class VisualFeedbackController : Node2D
{
    public override void _Ready()
    {
        Node room = GetParent();
        PlayerTank player = room.GetNode<PlayerTank>("PlayerTank");
        WeaponController weapon = player.GetNode<WeaponController>("WeaponController");
        weapon.ProjectileImpacted += SpawnImpact;
        HealthComponent health = player.GetNode<HealthComponent>("HealthComponent");
        int lastArmor = health.Armor;
        health.ValueChanged += (armor, _) =>
        {
            if (armor < lastArmor)
                SpriteEffectPlayer.Spawn(this, player.GlobalPosition, ArtTextureCatalog.PlayerHit, 16f, .72f, 21);
            Flash(player.GetNode<CanvasItem>("BodyVisual"), armor < lastArmor ? new Color(1f, 0.25f, 0.18f) : new Color(0.35f, 1f, 0.55f));
            Flash(player.GetNode<CanvasItem>("Turret/TurretVisual"), Colors.White);
            lastArmor = armor;
        };
    }

    private void SpawnImpact(Vector2 position, bool destroyedTarget, bool reflected)
    {
        SpriteEffectPlayer.Spawn(
            this,
            position,
            destroyedTarget ? ArtTextureCatalog.EnemyBurst : ArtTextureCatalog.SteelImpact,
            destroyedTarget ? 14f : 20f,
            destroyedTarget ? .8f : .55f,
            20,
            reflected ? new Color(.72f, .95f, 1f) : Colors.White);
    }

    private void Flash(CanvasItem item, Color color)
    {
        if (!IsInstanceValid(item)) return;
        item.Modulate = color;
        CreateTween().TweenProperty(item, "modulate", Colors.White, 0.12);
    }
}
