using Godot;

namespace Game1;

/// <summary>把命中事实转换为短寿命的火花或爆炸，不参与任何伤害结算。</summary>
public partial class VisualFeedbackController : Node2D
{
    private Panel _dangerFrame = null!;
    private Label _dangerLabel = null!;
    private float _dangerClock;

    public bool LowArmorWarningVisible => _dangerFrame?.Visible == true;

    public override void _Ready()
    {
        CreateDangerOverlay();
        Node room = GetParent();
        PlayerTank player = room.GetNode<PlayerTank>("PlayerTank");
        WeaponController weapon = player.GetNode<WeaponController>("WeaponController");
        weapon.ProjectileImpacted += SpawnImpact;
        HealthComponent health = player.GetNode<HealthComponent>("HealthComponent");
        int lastArmor = health.Armor;
        health.ValueChanged += (armor, _) =>
        {
            if (armor < lastArmor)
            {
                SpriteEffectPlayer.Spawn(this, player.GlobalPosition, ArtTextureCatalog.PlayerHit, 18f, 1.02f, 21);
                player.GetNode<TankVisualAnimator>("TankVisualAnimator").PlayHitReaction();
                player.GetNode<TankBuildVisualController>("TankBuildVisualController").PlayHitFlash();
                player.GetNode<AuxiliaryHost>("AuxiliaryHost").PlayHitFlash();
            }
            Flash(player.GetNode<CanvasItem>("BodyVisual"), armor < lastArmor ? new Color(1f, 0.25f, 0.18f) : new Color(0.35f, 1f, 0.55f));
            Flash(player.GetNode<CanvasItem>("Turret/TurretVisual"), Colors.White);
            Flash(player.GetNode<CanvasItem>("CoreVisual"), Colors.White);
            SetLowArmorWarning(AudioMixPolicy.IsLowArmor(armor, health.MaximumArmor));
            lastArmor = armor;
        };
        SetLowArmorWarning(AudioMixPolicy.IsLowArmor(health.Armor, health.MaximumArmor));
    }

    public override void _Process(double delta)
    {
        if (!LowArmorWarningVisible) return;
        _dangerClock += (float)delta;
        float pulse = .55f + .25f * (0.5f + 0.5f * Mathf.Sin(_dangerClock * 5.5f));
        _dangerFrame.Modulate = new Color(1f, 1f, 1f, pulse);
        _dangerLabel.Modulate = new Color(1f, .78f, .72f, .82f + .18f * Mathf.Sin(_dangerClock * 5.5f));
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

    private void CreateDangerOverlay()
    {
        CanvasLayer layer = new()
        {
            Name = "DangerLayer",
            Layer = 28,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(layer);
        StyleBoxFlat frameStyle = new()
        {
            BgColor = Colors.Transparent,
            BorderColor = new Color(1f, .12f, .04f, .78f)
        };
        frameStyle.SetBorderWidthAll(4);
        _dangerFrame = new Panel
        {
            Name = "LowArmorFrame",
            Position = Vector2.Zero,
            Size = new Vector2(480f, 270f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };
        _dangerFrame.AddThemeStyleboxOverride("panel", frameStyle);
        layer.AddChild(_dangerFrame);
        _dangerLabel = new Label
        {
            Name = "LowArmorLabel",
            Text = "装甲临界",
            Position = new Vector2(198f, 54f),
            Size = new Vector2(84f, 16f),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _dangerLabel.AddThemeColorOverride("font_color", new Color(1f, .35f, .22f));
        _dangerLabel.AddThemeColorOverride("font_outline_color", new Color(.08f, .02f, .01f));
        _dangerLabel.AddThemeConstantOverride("outline_size", 2);
        _dangerLabel.AddThemeFontSizeOverride("font_size", 9);
        _dangerFrame.AddChild(_dangerLabel);
    }

    private void SetLowArmorWarning(bool visible)
    {
        _dangerFrame.Visible = visible;
        if (!visible)
        {
            _dangerClock = 0f;
            _dangerFrame.Modulate = Colors.White;
        }
    }
}
