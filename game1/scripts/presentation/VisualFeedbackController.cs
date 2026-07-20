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
            Flash(player.GetNode<CanvasItem>("BodyVisual"), armor < lastArmor ? new Color(1f, 0.25f, 0.18f) : new Color(0.35f, 1f, 0.55f));
            Flash(player.GetNode<CanvasItem>("Turret/TurretVisual"), Colors.White);
            lastArmor = armor;
        };
    }

    private void SpawnImpact(Vector2 position, bool destroyedTarget, bool reflected)
    {
        Polygon2D flash = new()
        {
            GlobalPosition = position,
            Polygon = new Vector2[]
            {
                new(-4f, 0f), new(0f, -4f), new(4f, 0f), new(0f, 4f)
            },
            Color = destroyedTarget
                ? new Color(1f, 0.30f, 0.08f)
                : reflected ? new Color(0.55f, 0.90f, 1f) : new Color(1f, 0.82f, 0.24f),
            ZIndex = 20
        };
        AddChild(flash);

        float scale = destroyedTarget ? 2.5f : 1.4f;
        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(flash, "scale", Vector2.One * scale, destroyedTarget ? 0.18 : 0.10);
        tween.TweenProperty(flash, "modulate:a", 0f, destroyedTarget ? 0.22 : 0.12);
        tween.Chain().TweenCallback(Callable.From(flash.QueueFree));
    }

    private void Flash(CanvasItem item, Color color)
    {
        if (!IsInstanceValid(item)) return;
        item.Modulate = color;
        CreateTween().TweenProperty(item, "modulate", Colors.White, 0.12);
    }
}
