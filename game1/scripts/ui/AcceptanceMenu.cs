using System;
using System.Collections.Generic;
using Godot;

namespace Game1;

/// <summary>
/// Debug 构建中的策划可见验收与节奏调参入口。
/// UI 只发出正式操作请求或平衡快照，不直接修改单局私有状态和源资源。
/// </summary>
public partial class AcceptanceMenu : Control
{
    [Signal] public delegate void DamageRequestedEventHandler(int amount);
    [Signal] public delegate void ArmorPercentRequestedEventHandler(int percent);
    [Signal] public delegate void DefeatRequestedEventHandler();
    [Signal] public delegate void StopWaveSpawningRequestedEventHandler();
    [Signal] public delegate void ClearWaveEnemiesRequestedEventHandler();
    [Signal] public delegate void CompleteWaveRequestedEventHandler();
    [Signal] public delegate void AdvanceWaveRequestedEventHandler();
    [Signal] public delegate void EndRunRequestedEventHandler();
    [Signal] public delegate void ExperienceRequestedEventHandler(int amount);
    [Signal] public delegate void BossRequestedEventHandler();
    [Signal] public delegate void BossPhaseTwoRequestedEventHandler();
    [Signal] public delegate void BossDefeatRequestedEventHandler();
    [Signal] public delegate void RestartRequestedEventHandler();
    [Signal] public delegate void ProtocolRequestedEventHandler(string protocolId);
    [Signal] public delegate void AuxiliaryRequestedEventHandler(string auxiliaryId);
    [Signal]
    public delegate void TuningRequestedEventHandler(
        float spawnRateMultiplier,
        int maximumAliveAdjustment,
        float enemyMoveSpeedMultiplier,
        float enemyAttackRateMultiplier,
        float enemyArmorMultiplier,
        float playerMoveSpeedMultiplier,
        float playerFireRateMultiplier);
    [Signal] public delegate void SaveTuningRequestedEventHandler();

    private readonly Dictionary<string, Label> _valueLabels = new(StringComparer.Ordinal);
    private PanelContainer _panel = null!;
    private Label _status = null!;
    private Label _tuningTelemetry = null!;
    private Label _dirtyLabel = null!;
    private Button _saveButton = null!;
    private Button _firstQuickButton = null!;
    private ConfirmationDialog _saveConfirmation = null!;
    private BlockadeCityBalanceSettings _current = BlockadeCityBalanceSettings.DesignBaseline;
    private BlockadeCityBalanceSettings _saved = BlockadeCityBalanceSettings.DesignBaseline;
    private bool _saveAvailable;
    private string _saveUnavailableReason = "正式配置保存入口尚未就绪。";

    public BlockadeCityBalanceSettings CurrentTuning => _current;
    public bool HasUnsavedTuning => !_current.ApproximatelyEquals(_saved);

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        // Debug 控制台必须压在正式 HUD、核心选择和其他验收信息之上，避免顶部被经验框遮挡。
        ZIndex = 100;
        Visible = OS.IsDebugBuild();
        if (!Visible) return;

        Button entry = new()
        {
            Name = "EntryButton",
            Text = "竞技场验收",
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.All
        };
        entry.SetAnchorsPreset(LayoutPreset.TopRight);
        entry.OffsetLeft = -88f;
        entry.OffsetRight = -6f;
        entry.OffsetTop = 6f;
        entry.OffsetBottom = 26f;
        entry.AddThemeFontSizeOverride("font_size", 7);
        entry.Pressed += TogglePanel;
        AddChild(entry);

        _panel = new PanelContainer
        {
            Name = "Panel",
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false
        };
        _panel.SetAnchorsPreset(LayoutPreset.TopRight);
        _panel.OffsetLeft = -242f;
        _panel.OffsetRight = -4f;
        _panel.OffsetTop = 32f;
        _panel.OffsetBottom = 266f;
        AddChild(_panel);

        VBoxContainer shell = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _panel.AddChild(shell);
        Label title = new() { Text = "移动核心策划控制台", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 10);
        shell.AddChild(title);
        _status = new()
        {
            Text = "装甲、波次和调参结果会显示在这里",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(214f, 25f)
        };
        _status.AddThemeFontSizeOverride("font_size", 7);
        shell.AddChild(_status);

        TabContainer tabs = new()
        {
            Name = "AcceptanceTabs",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        shell.AddChild(tabs);
        tabs.AddChild(CreateQuickAcceptancePage());
        tabs.AddChild(CreateTuningPage());

        _saveConfirmation = new ConfirmationDialog
        {
            Name = "SaveTuningConfirmation",
            Title = "固化正式平衡配置",
            OkButtonText = "确认保存",
            CancelButtonText = "取消",
            Exclusive = true
        };
        _saveConfirmation.Confirmed += () => EmitSignal(SignalName.SaveTuningRequested);
        AddChild(_saveConfirmation);
        RefreshTuningUi();
    }

    public void SetStatus(string text)
    {
        if (_status is not null) _status.Text = text ?? string.Empty;
    }

    public void SetTuningTelemetry(string text)
    {
        if (_tuningTelemetry is not null) _tuningTelemetry.Text = text ?? string.Empty;
    }

    public void InitializeTuning(BlockadeCityBalanceSettings saved, bool saveAvailable, string unavailableReason = "")
    {
        saved.Validate();
        _saved = saved;
        _current = saved;
        _saveAvailable = saveAvailable;
        _saveUnavailableReason = string.IsNullOrWhiteSpace(unavailableReason)
            ? "当前运行环境不允许写入正式配置。"
            : unavailableReason;
        RefreshTuningUi();
    }

    public void MarkTuningSaved(BlockadeCityBalanceSettings saved)
    {
        saved.Validate();
        _saved = saved;
        _current = saved;
        RefreshTuningUi();
    }

    private Control CreateQuickAcceptancePage()
    {
        ScrollContainer scroll = new()
        {
            Name = "快捷验收",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        VBoxContainer content = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(content);
        _firstQuickButton = CreateButton("授予经验 +100", () => EmitSignal(SignalName.ExperienceRequested, 100));
        content.AddChild(_firstQuickButton);
        content.AddChild(CreateButton("授予协议：军械模块", () => EmitSignal(SignalName.ProtocolRequested, "arsenal_damage")));
        content.AddChild(CreateButton("授予协议：侦察模块", () => EmitSignal(SignalName.ProtocolRequested, "recon_trail")));
        content.AddChild(CreateButton("授予协议：后勤模块", () => EmitSignal(SignalName.ProtocolRequested, "logistics_armor")));
        content.AddChild(CreateButton("授予协议：工程模块", () => EmitSignal(SignalName.ProtocolRequested, "engineering_sidecar")));
        content.AddChild(CreateButton("授予辅助：侧挂速射炮", () => EmitSignal(SignalName.AuxiliaryRequested, "aux_side_cannon")));
        content.AddChild(CreateButton("授予辅助：环绕无人机", () => EmitSignal(SignalName.AuxiliaryRequested, "aux_orbit_drone")));
        content.AddChild(CreateButton("授予辅助：履带布雷器", () => EmitSignal(SignalName.AuxiliaryRequested, "aux_mine_layer")));
        content.AddChild(CreateButton("授予辅助：区域压制器", () => EmitSignal(SignalName.AuxiliaryRequested, "aux_suppression_field")));
        content.AddChild(CreateButton("装甲 -25", () => EmitSignal(SignalName.DamageRequested, 25)));
        content.AddChild(CreateButton("装甲设为 29%（维护保障）", () => EmitSignal(SignalName.ArmorPercentRequested, 29)));
        content.AddChild(CreateButton("触发坦克报废", () => EmitSignal(SignalName.DefeatRequested)));
        content.AddChild(CreateButton("结束刷新（保留残敌）", () => EmitSignal(SignalName.StopWaveSpawningRequested)));
        content.AddChild(CreateButton("敌军全灭（当前波）", () => EmitSignal(SignalName.ClearWaveEnemiesRequested)));
        content.AddChild(CreateButton("结束本轮并结算", () => EmitSignal(SignalName.CompleteWaveRequested)));
        content.AddChild(CreateButton("确认并到下一波", () => EmitSignal(SignalName.AdvanceWaveRequested)));
        content.AddChild(CreateButton("结束本局（验收）", () => EmitSignal(SignalName.EndRunRequested)));
        content.AddChild(CreateButton("进入 Boss 验收", () => EmitSignal(SignalName.BossRequested)));
        content.AddChild(CreateButton("Boss 推进到第二阶段", () => EmitSignal(SignalName.BossPhaseTwoRequested)));
        content.AddChild(CreateButton("击败 Boss（验收）", () => EmitSignal(SignalName.BossDefeatRequested)));
        content.AddChild(CreateButton("重新开始本局", () => EmitSignal(SignalName.RestartRequested)));
        return scroll;
    }

    private Control CreateTuningPage()
    {
        ScrollContainer scroll = new()
        {
            Name = "节奏调参",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        VBoxContainer content = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(content);

        _dirtyLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _dirtyLabel.AddThemeFontSizeOverride("font_size", 8);
        content.AddChild(_dirtyLabel);
        _tuningTelemetry = new Label
        {
            Text = "等待波次数据",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(208f, 26f)
        };
        _tuningTelemetry.AddThemeFontSizeOverride("font_size", 7);
        content.AddChild(_tuningTelemetry);

        HBoxContainer presets = new();
        presets.AddChild(CreateButton("标准", () => ApplyPreset(BlockadeCityBalanceSettings.DesignBaseline)));
        presets.AddChild(CreateButton("密集", () => ApplyPreset(BlockadeCityBalanceSettings.DensePreset)));
        presets.AddChild(CreateButton("高压", () => ApplyPreset(BlockadeCityBalanceSettings.HighPressurePreset)));
        content.AddChild(presets);

        content.AddChild(CreateValueRow("spawn", "刷怪速度", -0.25f, 0.25f));
        content.AddChild(CreateValueRow("alive", "敌军上限", -1f, 1f));
        content.AddChild(CreateValueRow("enemy_move", "敌军移动", -0.05f, 0.05f));
        content.AddChild(CreateValueRow("enemy_attack", "敌军攻击", -0.25f, 0.25f));
        content.AddChild(CreateValueRow("enemy_armor", "敌军装甲", -0.25f, 0.25f));
        content.AddChild(CreateValueRow("player_move", "玩家移动", -0.05f, 0.05f));
        content.AddChild(CreateValueRow("player_fire", "玩家射速", -0.25f, 0.25f));

        content.AddChild(CreateButton("恢复已保存配置", RestoreSaved));
        content.AddChild(CreateButton("恢复设计基准 ×1 / +0", () => ApplyPreset(BlockadeCityBalanceSettings.DesignBaseline)));
        content.AddChild(CreateButton("复制当前参数快照", CopyCurrentSnapshot));
        _saveButton = CreateButton("保存为正式配置", OpenSaveConfirmation);
        content.AddChild(_saveButton);
        return scroll;
    }

    private Control CreateValueRow(string id, string title, float decrease, float increase)
    {
        HBoxContainer row = new();
        Label titleLabel = new() { Text = title, CustomMinimumSize = new Vector2(68f, 0f) };
        titleLabel.AddThemeFontSizeOverride("font_size", 7);
        row.AddChild(titleLabel);
        row.AddChild(CreateButton("−", () => AdjustValue(id, decrease), 22f));
        Label value = new() { HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(58f, 0f) };
        value.AddThemeFontSizeOverride("font_size", 7);
        _valueLabels.Add(id, value);
        row.AddChild(value);
        row.AddChild(CreateButton("+", () => AdjustValue(id, increase), 22f));
        return row;
    }

    private void AdjustValue(string id, float amount)
    {
        _current = id switch
        {
            "spawn" => _current with { SpawnRateMultiplier = _current.SpawnRateMultiplier + amount },
            "alive" => _current with { MaximumAliveAdjustment = _current.MaximumAliveAdjustment + (int)amount },
            "enemy_move" => _current with { EnemyMoveSpeedMultiplier = _current.EnemyMoveSpeedMultiplier + amount },
            "enemy_attack" => _current with { EnemyAttackRateMultiplier = _current.EnemyAttackRateMultiplier + amount },
            "enemy_armor" => _current with { EnemyArmorMultiplier = _current.EnemyArmorMultiplier + amount },
            "player_move" => _current with { PlayerMoveSpeedMultiplier = _current.PlayerMoveSpeedMultiplier + amount },
            "player_fire" => _current with { PlayerFireRateMultiplier = _current.PlayerFireRateMultiplier + amount },
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "未知调参字段。")
        };
        ApplyPreset(_current.ClampToApprovedRange());
    }

    private void ApplyPreset(BlockadeCityBalanceSettings settings)
    {
        settings.Validate();
        _current = settings;
        RefreshTuningUi();
        EmitSignal(
            SignalName.TuningRequested,
            settings.SpawnRateMultiplier,
            settings.MaximumAliveAdjustment,
            settings.EnemyMoveSpeedMultiplier,
            settings.EnemyAttackRateMultiplier,
            settings.EnemyArmorMultiplier,
            settings.PlayerMoveSpeedMultiplier,
            settings.PlayerFireRateMultiplier);
    }

    private void RestoreSaved() => ApplyPreset(_saved);

    private void CopyCurrentSnapshot()
    {
        DisplayServer.ClipboardSet(_current.ToCompactText());
        SetStatus("当前参数快照已复制到剪贴板。");
    }

    private void OpenSaveConfirmation()
    {
        if (!_saveAvailable)
        {
            SetStatus(_saveUnavailableReason);
            return;
        }
        _saveConfirmation.DialogText =
            "以下参数将成为后续 Release 的正式配置：\n\n" + _current.ToCompactText() +
            "\n\n保存后仍需完成一次功能验收。";
        _saveConfirmation.PopupCentered(new Vector2I(360, 170));
    }

    private void RefreshTuningUi()
    {
        if (_dirtyLabel is null) return;
        _dirtyLabel.Text = HasUnsavedTuning ? "● 未保存到正式配置" : "✓ 当前为已保存配置";
        _dirtyLabel.Modulate = HasUnsavedTuning ? new Color(1f, 0.68f, 0.2f) : new Color(0.4f, 1f, 0.55f);
        _valueLabels["spawn"].Text = $"×{_current.SpawnRateMultiplier:0.00}";
        _valueLabels["alive"].Text = _current.MaximumAliveAdjustment.ToString("+0;-0;±0");
        _valueLabels["enemy_move"].Text = $"×{_current.EnemyMoveSpeedMultiplier:0.00}";
        _valueLabels["enemy_attack"].Text = $"×{_current.EnemyAttackRateMultiplier:0.00}";
        _valueLabels["enemy_armor"].Text = $"×{_current.EnemyArmorMultiplier:0.00}";
        _valueLabels["player_move"].Text = $"×{_current.PlayerMoveSpeedMultiplier:0.00}";
        _valueLabels["player_fire"].Text = $"×{_current.PlayerFireRateMultiplier:0.00}";
        _saveButton.Disabled = !_saveAvailable || !HasUnsavedTuning;
        _saveButton.TooltipText = _saveAvailable ? "写入 Release 读取的正式 .tres" : _saveUnavailableReason;
    }

    private void TogglePanel()
    {
        _panel.Visible = !_panel.Visible;
        if (_panel.Visible) _firstQuickButton.GrabFocus();
    }

    private static Button CreateButton(string text, Action pressed, float minimumWidth = 0f)
    {
        Button button = new()
        {
            Text = text,
            FocusMode = FocusModeEnum.All,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(minimumWidth, 0f)
        };
        button.AddThemeFontSizeOverride("font_size", 7);
        button.Pressed += pressed;
        return button;
    }
}
