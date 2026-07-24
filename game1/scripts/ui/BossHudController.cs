using Godot;

namespace Game1;

/// <summary>独立 Boss 血条。显示层只订阅 Boss 信号，不拥有或修改战斗状态。</summary>
public partial class BossHudController : Control
{
    private static Texture2D FrameTexture =>
        GD.Load<Texture2D>("res://assets/sprites/ui/boss_status_frame.png");
    private RoadblockCommander _boss;
    private Label _nameLabel = null!;
    private Label _phaseLabel = null!;
    private ProgressBar _healthBar = null!;
    private TextureRect _phaseIcon = null!;

    public override void _Ready()
    {
        Position = new Vector2(150, 10);
        Size = new Vector2(180, 42);
        MouseFilter = MouseFilterEnum.Ignore;

        TextureRect frame = new()
        {
            Name = "Frame",
            Position = Vector2.Zero,
            Size = Size,
            Texture = FrameTexture,
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(frame);
        _nameLabel = new Label { Name = "NameLabel", Position = new Vector2(8, 3), Size = new Vector2(164, 14) };
        _phaseLabel = new Label { Name = "PhaseLabel", Position = new Vector2(8, 16), Size = new Vector2(66, 16) };
        _healthBar = new ProgressBar { Name = "HealthBar", Position = new Vector2(72, 18), Size = new Vector2(98, 12), ShowPercentage = false };
        _phaseIcon = new TextureRect
        {
            Name = "PhaseIcon",
            Position = new Vector2(146, 1),
            Size = new Vector2(22, 16),
            Texture = ArtTextureCatalog.EliteIcon,
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        StyleBoxFlat background = new() { BgColor = new Color(.03f, .04f, .05f, .92f), BorderColor = new Color(.34f, .24f, .1f) };
        background.SetBorderWidthAll(1);
        StyleBoxFlat fill = new() { BgColor = new Color(1f, .55f, .12f) };
        _healthBar.AddThemeStyleboxOverride("background", background);
        _healthBar.AddThemeStyleboxOverride("fill", fill);
        AddChild(_nameLabel);
        AddChild(_phaseLabel);
        AddChild(_healthBar);
        AddChild(_phaseIcon);
    }

    public void Bind(RoadblockCommander boss, BossDefinition definition)
    {
        Unbind();
        _boss = boss;
        _nameLabel.Text = definition.DisplayName;
        _healthBar.MaxValue = definition.MaximumHealth;
        _boss.HealthChanged += OnHealthChanged;
        _boss.PhaseChanged += OnPhaseChanged;
        _boss.Defeated += OnDefeated;
        OnHealthChanged(_boss.CurrentHealth, definition.MaximumHealth);
        OnPhaseChanged((int)BossPhase.PhaseOne);
        Visible = true;
    }

    public void Unbind()
    {
        if (_boss is null) return;
        _boss.HealthChanged -= OnHealthChanged;
        _boss.PhaseChanged -= OnPhaseChanged;
        _boss.Defeated -= OnDefeated;
        _boss = null;
        Visible = false;
    }

    private void OnHealthChanged(int current, int maximum)
    {
        _healthBar.MaxValue = maximum;
        _healthBar.Value = current;
    }

    private void OnPhaseChanged(int phase)
    {
        bool secondPhase = phase == (int)BossPhase.PhaseTwo;
        _phaseLabel.Text = secondPhase ? "第二阶段" : "第一阶段";
        _phaseLabel.AddThemeColorOverride("font_color", secondPhase ? new Color(1f, 0.28f, 0.18f) : new Color(1f, 0.74f, 0.22f));
        _healthBar.Modulate = secondPhase ? new Color(1f, 0.5f, 0.5f) : Colors.White;
        _phaseIcon.Texture = secondPhase ? ArtTextureCatalog.RebootIcon : ArtTextureCatalog.EliteIcon;
    }

    private void OnDefeated()
    {
        _phaseLabel.Text = "已击败";
        _healthBar.Value = 0;
    }
}
