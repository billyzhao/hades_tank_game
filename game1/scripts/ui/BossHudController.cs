using Godot;

namespace Game1;

/// <summary>独立 Boss 血条。显示层只订阅 Boss 信号，不拥有或修改战斗状态。</summary>
public partial class BossHudController : Control
{
    private RoadblockCommander _boss;
    private Label _nameLabel = null!;
    private Label _phaseLabel = null!;
    private ProgressBar _healthBar = null!;

    public override void _Ready()
    {
        Position = new Vector2(150, 10);
        Size = new Vector2(180, 42);
        MouseFilter = MouseFilterEnum.Ignore;

        _nameLabel = new Label { Name = "NameLabel", Position = Vector2.Zero, Size = new Vector2(180, 16) };
        _phaseLabel = new Label { Name = "PhaseLabel", Position = new Vector2(0, 14), Size = new Vector2(90, 16) };
        _healthBar = new ProgressBar { Name = "HealthBar", Position = new Vector2(72, 16), Size = new Vector2(108, 16), ShowPercentage = false };
        AddChild(_nameLabel);
        AddChild(_phaseLabel);
        AddChild(_healthBar);
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
    }

    private void OnDefeated()
    {
        _phaseLabel.Text = "已击败";
        _healthBar.Value = 0;
    }
}
